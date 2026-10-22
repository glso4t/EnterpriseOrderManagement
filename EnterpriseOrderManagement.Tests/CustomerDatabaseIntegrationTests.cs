using System.Net;
using System.Net.Http.Json;
using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseOrderManagement.Tests;

public class CustomerDatabaseIntegrationTests
    : IClassFixture<SqlServerTestWebApplicationFactory>
{
    private readonly SqlServerTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomerDatabaseIntegrationTests(
        SqlServerTestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostCustomer_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        var request = new
        {
            name = "Test Customer",
            email
        };
        // Act
        var firstResponse = await _client.PostAsJsonAsync(
            "/api/customers",
            request);
        var secondResponse = await _client.PostAsJsonAsync(
            "/api/customers",
            request);
        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task CustomerEmailUniqueIndex_RejectsDuplicateEmail()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<
                EnterpriseOrderManagementDbContext>();
        var email = $"database-{Guid.NewGuid():N}@example.com";
        dbContext.Customers.Add(new Customer
        {
            Name = "First Customer",
            Email = email
        });
        await dbContext.SaveChangesAsync();
        // Act
        dbContext.Customers.Add(new Customer
        {
            Name = "Second Customer",
            Email = email
        });
        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }
}