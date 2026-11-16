using System.Security.Claims;

using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseOrderManagement.Tests;

public class AuditTransactionIntegrationTests
    : IClassFixture<SqlServerTestWebApplicationFactory>
{
    private readonly SqlServerTestWebApplicationFactory _factory;

    public AuditTransactionIntegrationTests(
        SqlServerTestWebApplicationFactory factory)
    {
        _factory = factory;

        factory.InitializeDatabase();
    }

    [Fact]
    public async Task SaveChanges_WhenAuditInsertFails_RollsBackBusinessChange()
    {
        var email = $"rollback-{Guid.NewGuid():N}@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<
                    EnterpriseOrderManagementDbContext>();

            var accessor = scope.ServiceProvider
                .GetRequiredService<IHttpContextAccessor>();

            // A UserId longer than AuditLogs.UserId (nvarchar(450))
            // makes the audit INSERT fail after the business INSERT.
            accessor.HttpContext = CreateHttpContextWithOversizedUserId();

            try
            {
                dbContext.Customers.Add(new Customer
                {
                    Name = "Rollback Customer",
                    Email = email
                });

                await Assert.ThrowsAnyAsync<DbUpdateException>(
                    () => dbContext.SaveChangesAsync());
            }
            finally
            {
                accessor.HttpContext = null;
            }
        }

        // The business change must have been rolled back as well.
        Assert.False(await CustomerExistsAsync(email));
    }

    [Fact]
    public async Task OuterTransaction_WhenAuditInsertFails_RollsBackEverything()
    {
        var email = $"outer-rollback-{Guid.NewGuid():N}@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<
                    EnterpriseOrderManagementDbContext>();

            var unitOfWork = scope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            var accessor = scope.ServiceProvider
                .GetRequiredService<IHttpContextAccessor>();

            accessor.HttpContext = CreateHttpContextWithOversizedUserId();

            try
            {
                await Assert.ThrowsAnyAsync<DbUpdateException>(
                    () => unitOfWork.ExecuteInTransactionAsync(
                        async cancellationToken =>
                        {
                            dbContext.Customers.Add(new Customer
                            {
                                Name = "Outer Rollback Customer",
                                Email = email
                            });

                            await dbContext.SaveChangesAsync(
                                cancellationToken);

                            return true;
                        }));
            }
            finally
            {
                accessor.HttpContext = null;
            }
        }

        Assert.False(await CustomerExistsAsync(email));
    }

    // Authenticated user whose "sub" claim cannot fit in AuditLogs.UserId.
    private static DefaultHttpContext CreateHttpContextWithOversizedUserId()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("sub", new string('x', 500)) },
            authenticationType: "Test");

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }

    // Checks the database from a fresh scope (no tracked entities).
    private async Task<bool> CustomerExistsAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<
                EnterpriseOrderManagementDbContext>();

        return await dbContext.Customers
            .AsNoTracking()
            .AnyAsync(customer => customer.Email == email);
    }
}