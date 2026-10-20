using EnterpriseOrderManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.Data;

public class EnterpriseOrderManagementDbContext(
    DbContextOptions<EnterpriseOrderManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();
    
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
            entity.Property(product => product.RowVersion)
                .IsRowVersion();
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

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(customer => customer.Name)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(customer => customer.Email)
                .HasMaxLength(320)
                .IsRequired();
            entity.HasIndex(customer => customer.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
    {
        entity.Property(order => order.OrderDate)
            .IsRequired();
        entity.HasOne(order => order.Customer)
            .WithMany(customer => customer.Orders)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict); //Αν ένας πελάτης έχει έστω και μία παραγγελία στο σύστημα, δεν μπορείς να διαγράψεις τον πελάτη για να διατηρηθεί το ιστορικό της επιχείρησης
    });

        modelBuilder.Entity<OrderItem>(entity =>
    {
        entity.Property(item => item.Quantity)
            .IsRequired();
        entity.Property(item => item.UnitPrice)
            .HasPrecision(18, 2);
        entity.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade); //αν διαγραφει η κεντρική παραγγελία (Order), τότε διαγράφονται αυτόματα από τη βάση και όλες οι γραμμές
        entity.HasOne(item => item.Product)
            .WithMany(product => product.OrderItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict); //Αν κάποιος πάει να διαγράψει ένα προϊόν (Product) το οποίο έχει χρησιμοποιηθεί έστω και σε μία παλιά παραγγελία, η βάση θα το απαγορεύσει και θα πετάξει σφάλμα
    });

    }
}