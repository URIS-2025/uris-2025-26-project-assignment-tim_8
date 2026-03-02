using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Abstractions;
using LoggerService.Models;

namespace LoggerService.Context
{
    public class LoggerContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public LoggerContext(DbContextOptions options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<Log> Logs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("LoggerDB"));
            }
        }

    }
}
