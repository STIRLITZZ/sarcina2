using BlazorApp11.Shared;
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

	}
}
