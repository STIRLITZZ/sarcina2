using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorApp11.Server.Data;
using BlazorApp11.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlazorApp11.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration config;

        public AuthenticationController(DataContext context, IConfiguration config)
        {
            _context = context;
            this.config = config;
        }

        // Account Registration
        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterUser(RegisterModel model)
        {
            //Check if user email contains admin and admin role is already occupied.
            string role = string.Empty;
            if (model.Email!.ToLower().StartsWith("admin"))
            {
                role = "Admin";
            }
            else
            {
                role = "User";
            }


            //Check if the email is already used in registration.
            var chk = await _context.Registration.FirstOrDefaultAsync(_ => _.Email!.ToLower().Equals(model.Email!.ToLower()));
            if (chk is not null) return BadRequest(-1);

            var entity = _context.Registration.Add(
                new Register()
                {
                    Email = model.Email,
                    Name = model.Name,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password)
                }).Entity;
            await _context.SaveChangesAsync();


            //Save the Role asigned to the database   
            _context.UserRoles.Add(new AuthenticationModel.UserRole() { RoleName = role, UserId = entity.Id });
            await _context.SaveChangesAsync();
            return Ok(entity.Id);
        }

        // Account login
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserSession>> LoginUser(Login model)
        {
            //Check if user email exist
            var chk = await _context.Registration.FirstOrDefaultAsync(_ => _.Email!.ToLower().Equals(model.Email!.ToLower()));
            if (chk is null) return BadRequest(null!);

            if (BCrypt.Net.BCrypt.Verify(model.Password, chk.Password))
            {
                //Find User Role from the User-role table
                var getRole = await _context.UserRoles.FirstOrDefaultAsync(_ => _.UserId == chk.Id);
                if (getRole is null) return BadRequest(null!);


                //Generate Token
                var token = GenerateToken(model.Email, getRole.RoleName);

                // Generate Refresh Token
                var refreshToken = GenerateRefreshToken();

                //Check if user has refresh token already.
                var chkUserToken = await _context.TokenInfo.FirstOrDefaultAsync(_ => _.UserId == chk.Id);
                if (chkUserToken is null)
                {
                    //Save the refreshtoken to the TokenInfo table
                    _context.TokenInfo.Add(new AuthenticationModel.TokenInfo()
                    { RefreshToken = refreshToken, UserId = chk.Id, TokenExpiry = DateTime.UtcNow.AddMinutes(10) });
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Update the the refresh token
                    chkUserToken.RefreshToken = refreshToken;
                    chkUserToken.TokenExpiry = DateTime.Now.AddMinutes(1);
                    await _context.SaveChangesAsync();
                }
                return Ok(new UserSession() { Token = token, RefreshToken = refreshToken });
            }
            return null!;
        }

        // General Methods for token and refresh token generating.
        private static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private string GenerateToken(string? email, string? roleName)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.Name,email!),
                new Claim(ClaimTypes.Role,roleName!)
            };
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: userClaims,
                expires: DateTime.UtcNow.AddMinutes(1),
                signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        //PRIVATE API ENDPOINTS

        // Get total user in the database, ONLY Admin can do that.
        [HttpGet("total-users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTotalUsersCount()
        {
            var users = await _context.Registration.ToListAsync();
            return Ok(users.Count);
        }


        // Get Current User info, ONLY user can do that.
        [HttpGet("my-info/{email}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetMyInfo(string email)
        {
            var user = await _context.Registration.FirstOrDefaultAsync(_ => _.Email.ToLower().Equals(email.ToLower()));
            string info = $"Name : {user.Name}{Environment.NewLine}Email: {user.Email}";
            return Ok(info);
        }

        //PUBLIC API ENPOINT FOR GENERATING NEW REFRESH TOKEN AND AUTHENTICATION TOKEN
        [HttpPost("GetNewToken")]
        [AllowAnonymous]
        public async Task<ActionResult<UserSession>> GetNewToken(UserSession userSession)
        {
            if (userSession is null) return null!;
            var rToken = await _context.TokenInfo.Where(_ => _.RefreshToken!.Equals(userSession.RefreshToken)).FirstOrDefaultAsync();

            // check if refresh token expiration date is due then.
            if (rToken is null) return null!;

            //Generate new refresh token if Due.
            string newRefreshToken = string.Empty;
            if (rToken.TokenExpiry < DateTime.Now)
                newRefreshToken = GenerateRefreshToken();

            //Generate new token by extracting the claims from the old jwt 
            var jwtToken = new JwtSecurityToken(userSession!.Token);
            var userClaims = jwtToken.Claims;

            //Get all claims from the token.
            var email = userClaims.First(c => c.Type == ClaimTypes.Name).Value;
            var role = userClaims.First(c => c.Type == ClaimTypes.Role).Value;
            string newToken = GenerateToken(email, role);

            //update refresh token in the DB
            var user = await _context.Registration.FirstOrDefaultAsync(_ => _.Email.ToLower().Equals(email.ToLower()));
            var rTokenUser = await _context.TokenInfo.FirstOrDefaultAsync(_ => _.UserId == user.Id);

            if (!string.IsNullOrEmpty(newRefreshToken))
            {
                rTokenUser.RefreshToken = newRefreshToken;
                rTokenUser.TokenExpiry = DateTime.Now.AddMinutes(10);
                await _context.SaveChangesAsync();
            }
            return Ok(new UserSession() { Token = newToken, RefreshToken = newRefreshToken });

        }



            private bool RegisterModelExists(int id)
        {
            return (_context.RegisterModel?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
