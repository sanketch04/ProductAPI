
using Microsoft.EntityFrameworkCore;
using Product.DAL.Entities;

namespace Product.DAL.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProductEntity> Products { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductEntity>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(p => p.Description)
                    .HasMaxLength(1000);

                entity.Property(p => p.Price)
                    .HasPrecision(18, 2);

                entity.Property(p => p.Stock)
                    .IsRequired();

                entity.Property(p => p.ImagePath)
                    .HasMaxLength(500);
            });
        }
    }
}
