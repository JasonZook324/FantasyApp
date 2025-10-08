using Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class FantasyDbContext : DbContext
    {
        public FantasyDbContext(DbContextOptions<FantasyDbContext> options) : base(options)
        {
        }

        public DbSet<Player> Players => Set<Player>();
        public DbSet<User> Users => Set<User>();
        public DbSet<EspnData> EspnDatas => Set<EspnData>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<LogEntry> Logs => Set<LogEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FantasyDbContext).Assembly);
        }
    }
}
