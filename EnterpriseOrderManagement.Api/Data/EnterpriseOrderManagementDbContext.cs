using EnterpriseOrderManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using System.Globalization;
using System.Reflection;
using System.Security.Claims;

using EnterpriseOrderManagement.Api.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.Data;

public class EnterpriseOrderManagementDbContext(
    DbContextOptions<EnterpriseOrderManagementDbContext> options,
    IHttpContextAccessor httpContextAccessor)
    : IdentityDbContext<ApplicationUser>(options)
{

    private readonly IHttpContextAccessor _httpContextAccessor =
        httpContextAccessor;

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();



    // Save business changes and their audit records atomically.
    public override int SaveChanges()
    {
        var pendingAuditEntries = GetPendingAuditEntries();
        if (pendingAuditEntries.Count == 0)
        {
            return base.SaveChanges();
        }

        using var transaction = Database.CurrentTransaction is null
            ? Database.BeginTransaction()
            : null;

        try
        {
            // Save the original business changes.
            var affectedRows = base.SaveChanges();

            // Create audit records after database-generated IDs are available.
            AddAuditLogs(pendingAuditEntries);

            // Save the audit records in the same transaction.
            affectedRows += base.SaveChanges();

            transaction?.Commit();

            return affectedRows;
        }
        catch
        {
            transaction?.Rollback();
            throw;
        }
    }


    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var pendingAuditEntries = GetPendingAuditEntries();

        if (pendingAuditEntries.Count == 0)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        await using var transaction = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            // Save the original business changes.
            var affectedRows = await base.SaveChangesAsync(
                cancellationToken);

            // Create audit records after database-generated IDs are available.
            AddAuditLogs(pendingAuditEntries);

            // Save audit records in the same transaction.
            affectedRows += await base.SaveChangesAsync(
                cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return affectedRows;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
    }


    // Collect only the business entities that need auditing.
    private List<PendingAuditEntry> GetPendingAuditEntries()
    {
        ChangeTracker.DetectChanges();
        return ChangeTracker.Entries()
            .Where(entry =>
                entry.Entity is Product or Customer or Order)
            .Where(entry =>
                entry.State is EntityState.Added
                    or EntityState.Modified
                    or EntityState.Deleted)
            .Select(entry =>
            {
                var keyProperty = entry.Metadata.FindPrimaryKey()
                    ?.Properties.SingleOrDefault()
                    ?.PropertyInfo
                    ?? throw new InvalidOperationException(
                        $"Audited entity {entry.Metadata.ClrType.Name} " +
                        "must have a single CLR primary key property.");
                var action = entry.State switch
                {
                    EntityState.Added => "Created",
                    EntityState.Modified => "Updated",
                    EntityState.Deleted => "Deleted",
                    _ => throw new InvalidOperationException(
                        "Unsupported audit operation.")
                };
                return new PendingAuditEntry(
                    entry.Entity,
                    entry.Metadata.ClrType.Name,
                    action,
                    keyProperty);
            })
            .ToList();
    }


    // Add the audit records after the business entities have been saved.
    private void AddAuditLogs(
        IReadOnlyCollection<PendingAuditEntry> pendingEntries)
    {
        var currentUser = _httpContextAccessor.HttpContext?.User;
        var userId =
            currentUser?.FindFirst("sub")?.Value
            ?? currentUser?.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
        var changedAtUtc = DateTime.UtcNow;
        foreach (var pendingEntry in pendingEntries)
        {
            var entityId = Convert.ToString(
                pendingEntry.KeyProperty.GetValue(
                    pendingEntry.Entity),
                CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(entityId))
            {
                throw new InvalidOperationException(
                    "Could not determine the audited entity ID.");
            }
            AuditLogs.Add(new AuditLog
            {
                EntityName = pendingEntry.EntityName,
                EntityId = entityId,
                Action = pendingEntry.Action,
                UserId = userId,
                ChangedAtUtc = changedAtUtc
            });
        }
    }


    // Temporary data collected before SaveChanges.
    private sealed record PendingAuditEntry(
        object Entity,
        string EntityName,
        string Action,
        PropertyInfo KeyProperty);


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
            entity.ToTable(table =>
            {   
                table.HasCheckConstraint(
                    "CK_Products_Price_Positive",
                    "[Price] > 0");
                table.HasCheckConstraint(
                    "CK_Products_Stock_NonNegative",
                    "[Stock] >= 0");
            });
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

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId)
                .IsRequired();
            entity.Property(x => x.TokenHash)
                .HasMaxLength(64)
                .IsRequired();
            entity.HasIndex(x => x.TokenHash)
                .IsUnique();
            entity.HasIndex(x => x.TokenFamilyId);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(order => order.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(OrderStatus.Pending)
                .IsRequired();
            entity.Property(order => order.RowVersion)
                .IsRowVersion();
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
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_OrderItems_Quantity_Positive",
                    "[Quantity] > 0");

                table.HasCheckConstraint(
                    "CK_OrderItems_UnitPrice_NonNegative",
                    "[UnitPrice] >= 0");
            });
            entity.HasOne(item => item.Order)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade); //αν διαγραφει η κεντρική παραγγελία (Order), τότε διαγράφονται αυτόματα από τη βάση και όλες οι γραμμές
            entity.HasOne(item => item.Product)
                .WithMany(product => product.OrderItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict); //Αν κάποιος πάει να διαγράψει ένα προϊόν (Product) το οποίο έχει χρησιμοποιηθεί έστω και σε μία παλιά παραγγελία, η βάση θα το απαγορεύσει και θα πετάξει σφάλμα
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(auditLog => auditLog.Id);
            entity.Property(auditLog => auditLog.EntityName)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(auditLog => auditLog.EntityId)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(auditLog => auditLog.Action)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(auditLog => auditLog.UserId)
                .HasMaxLength(450);
            entity.Property(auditLog => auditLog.ChangedAtUtc)
                .IsRequired();
            entity.HasIndex(auditLog => auditLog.ChangedAtUtc);
            entity.HasIndex(auditLog => new
            {
                auditLog.EntityName,
                auditLog.EntityId
            });
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Type)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(message => message.Content)
                .HasColumnType("nvarchar(max)")
                .IsRequired();
            entity.Property(message => message.OccurredOnUtc)
                .IsRequired();
            entity.Property(message => message.Error)
                .HasMaxLength(2000);
            entity.HasIndex(message => new
            {
                message.ProcessedOnUtc,
                message.OccurredOnUtc
            });
        });
    }
}