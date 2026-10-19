using Microsoft.EntityFrameworkCore;
using TransportGwp.Domain.Entities;

namespace TransportGwp.Infrastructure.Persistence
{
    public class TransportDbContext : DbContext
    {
        public TransportDbContext(DbContextOptions<TransportDbContext> options) : base(options)
        {
        }

        public DbSet<Transport> Transports => Set<Transport>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transport>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Id).HasMaxLength(32);
                e.Property(t => t.Name).HasMaxLength(120).IsRequired();
                e.HasIndex(t => t.Name).IsUnique();
                e.Property(t => t.Mode).HasMaxLength(32);
                e.Property(t => t.ModeLabel).HasMaxLength(64);
                e.Property(t => t.FromLabel).HasMaxLength(256);
                e.Property(t => t.ToLabel).HasMaxLength(256);
            });
        }
    }
}
