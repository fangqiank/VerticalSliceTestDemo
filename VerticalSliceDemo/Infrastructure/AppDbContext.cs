using Microsoft.EntityFrameworkCore;
using VerticalSliceDemo.Domains;

namespace VerticalSliceDemo.Infrastructure
{
    public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Shipment> Shipments => Set<Shipment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasConversion<string>();
            });

            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderId).IsUnique();
                entity.Property(e => e.Status).HasConversion<string>();
            });
        }
    }
}
