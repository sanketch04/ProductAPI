
using Microsoft.EntityFrameworkCore;
using Product.DAL.Entities;

namespace Product.DAL.Context;

public class AppDbContext : DbContext
{
    #region Ctor

    public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
    {
    }

    #endregion

    #region DbSets

    public DbSet<ProductEntity> Products { get; set; } = null!;

    public DbSet<ApplicationUserEntity> users { get; set; } = null!;

    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; } = null!;

    #endregion

    #region Model Configuration

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

        modelBuilder.Entity<ApplicationUserEntity>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(15);

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(u => u.IsActive)
                .HasDefaultValue(true);

            entity.Property(u => u.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(u => u.NormalizedEmail)
                .IsUnique();
        });

        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(r => r.TokenHash)
                .IsUnique();

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    #endregion
}
