using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorApp11.Server.Data;
using BlazorApp11.Shared;

namespace BlazorApp11.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ElevsController : ControllerBase
    {
        private readonly DataContext _context;

        public ElevsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Elevs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Elev>>> GetElevi()
        {
          if (_context.Elevi == null)
          {
              return NotFound();
          }
            return await _context.Elevi.ToListAsync();
        }

        // GET: api/Elevs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Elev>> GetElev(int id)
        {
          if (_context.Elevi == null)
          {
              return NotFound();
          }
            var elev = await _context.Elevi.FindAsync(id);

            if (elev == null)
            {
                return NotFound();
            }

            return elev;
        }

        // PUT: api/Elevs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutElev(int id, Elev elev)
        {
            if (id != elev.Id)
            {
                return BadRequest();
            }

            _context.Entry(elev).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ElevExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Elevs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Elev>> PostElev(Elev elev)
        {
          if (_context.Elevi == null)
          {
              return Problem("Entity set 'DataContext.Elevi'  is null.");
          }
            _context.Elevi.Add(elev);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetElev", new { id = elev.Id }, elev);
        }

        // DELETE: api/Elevs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteElev(int id)
        {
            if (_context.Elevi == null)
            {
                return NotFound();
            }
            var elev = await _context.Elevi.FindAsync(id);
            if (elev == null)
            {
                return NotFound();
            }

            _context.Elevi.Remove(elev);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ElevExists(int id)
        {
            return (_context.Elevi?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
