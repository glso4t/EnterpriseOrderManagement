using System.Net;
using System.Net.Http.Json;

using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Dtos.Products;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseOrderManagement.Tests;

public class AuditLoggingIntegrationTests
    : IClassFixture<SqlServerTestWebApplicationFactory>
{
    private readonly SqlServerTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuditLoggingIntegrationTests(
        SqlServerTestWebApplicationFactory factory)
    {
        _factory = factory;

        factory.InitializeDatabase();

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProductLifecycle_CreatesAuditEntries()
    {
        // CREATE
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                name = $"Audit Product {Guid.NewGuid():N}",
                price = 19.99m,
                stock = 10
            });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var product = await createResponse.Content
            .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);

        // UPDATE
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1/products/{product.Id}",
            new
            {
                name = "Updated Audit Product",
                price = 24.99m,
                stock = 15
            });

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        // DELETE
        var deleteResponse = await _client.DeleteAsync(
            $"/api/v1/products/{product.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        // VERIFY AUDIT RECORDS
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<
                EnterpriseOrderManagementDbContext>();

        var auditEntries = await dbContext.AuditLogs
            .AsNoTracking()
            .Where(log =>
                log.EntityName == "Product" &&
                log.EntityId == product.Id.ToString())
            .OrderBy(log => log.Id)
            .ToListAsync();

        Assert.Equal(
            new[] { "Created", "Updated", "Deleted" },
            auditEntries.Select(log => log.Action).ToArray());

        Assert.All(
            auditEntries,
            entry => Assert.Equal(
                "integration-test-user",
                entry.UserId));
    }
}