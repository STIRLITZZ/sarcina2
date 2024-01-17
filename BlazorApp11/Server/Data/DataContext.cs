using BlazorApp11.Server.AuthenticationModel;
using BlazorApp11.Shared;
using BlazorApp11.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace BlazorApp11.Server.Data
{
	public class DataContext : DbContext
	{
		public DataContext(DbContextOptions<DataContext> options)
			: base(options)

		{

		}


		public DbSet<Elev> Elevi { get; set; }

        public DbSet<Register> Registration { get; set; }
        public DbSet<TokenInfo> TokenInfo { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<BlazorApp11.Shared.Models.RegisterModel> RegisterModel { get; set; } = default!;


    }
}
