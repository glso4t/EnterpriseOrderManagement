using EnterpriseOrderManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.Data;

public class EnterpriseOrderManagementDbContext(
    DbContextOptions<EnterpriseOrderManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);

            entity.Property(product => product.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(product => product.Price)
                .HasPrecision(18, 2);

            entity.Property(product => product.Stock)
                .IsRequired();

            entity.HasData(
                new Product
                {
                    Id = 1,
                    Name = "Laptop",
                    Price = 899.99m,
                    Stock = 10
                },
                new Product
                {
                    Id = 2,
                    Name = "Mouse",
                    Price = 25.50m,
                    Stock = 50
                }
            );
        });
    }
}