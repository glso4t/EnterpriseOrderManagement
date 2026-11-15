using System.Net;
using System.Net.Http.Json;

using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Dtos.Customers;
using EnterpriseOrderManagement.Api.Dtos.Orders;
using EnterpriseOrderManagement.Api.Dtos.Products;
using EnterpriseOrderManagement.Api.Models;

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

    [Fact]
    public async Task CustomerLifecycle_CreatesAuditEntries()
    {
        // CREATE
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/customers",
            new
            {
                name = "Audit Customer",
                email = $"audit-{Guid.NewGuid():N}@example.com"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var customer = await createResponse.Content
            .ReadFromJsonAsync<CustomerResponse>();

        Assert.NotNull(customer);

        // UPDATE
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1/customers/{customer.Id}",
            new
            {
                name = "Updated Audit Customer",
                email = $"updated-{Guid.NewGuid():N}@example.com"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        // DELETE
        var deleteResponse = await _client.DeleteAsync(
            $"/api/v1/customers/{customer.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        // VERIFY AUDIT RECORDS
        var auditEntries = await GetAuditEntriesAsync(
            "Customer",
            customer.Id);

        Assert.Equal(
            new[] { "Created", "Updated", "Deleted" },
            auditEntries.Select(log => log.Action).ToArray());

        Assert.All(
            auditEntries,
            entry => Assert.Equal(
                "integration-test-user",
                entry.UserId));
    }

    [Fact]
    public async Task OrderLifecycle_CreatesAuditEntriesForOrderAndStock()
    {
        // ARRANGE: customer for the order
        var customerResponse = await _client.PostAsJsonAsync(
            "/api/v1/customers",
            new
            {
                name = "Audit Order Customer",
                email = $"audit-order-{Guid.NewGuid():N}@example.com"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            customerResponse.StatusCode);

        var customer = await customerResponse.Content
            .ReadFromJsonAsync<CustomerResponse>();

        Assert.NotNull(customer);

        // CREATE ORDER (product 1 is seeded with stock 10)
        var orderResponse = await _client.PostAsJsonAsync(
            "/api/v1/orders",
            new
            {
                customerId = customer.Id,
                items = new[]
                {
                    new { productId = 1, quantity = 1 }
                }
            });

        Assert.Equal(
            HttpStatusCode.Created,
            orderResponse.StatusCode);

        var order = await orderResponse.Content
            .ReadFromJsonAsync<OrderResponse>();

        Assert.NotNull(order);

        // CONFIRM
        var confirmResponse = await _client.PostAsync(
            $"/api/v1/orders/{order.Id}/confirm",
            content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            confirmResponse.StatusCode);

        // CANCEL (restores the stock)
        var cancelResponse = await _client.PostAsync(
            $"/api/v1/orders/{order.Id}/cancel",
            content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            cancelResponse.StatusCode);

        // VERIFY ORDER AUDIT RECORDS
        var orderAudit = await GetAuditEntriesAsync(
            "Order",
            order.Id);

        Assert.Equal(
            new[] { "Created", "Updated", "Updated" },
            orderAudit.Select(log => log.Action).ToArray());

        Assert.All(
            orderAudit,
            entry => Assert.Equal(
                "integration-test-user",
                entry.UserId));

        // VERIFY PRODUCT STOCK AUDIT RECORDS
        // (stock decreased on create, restored on cancel)
        var productAudit = await GetAuditEntriesAsync(
            "Product",
            1);

        Assert.Equal(
            new[] { "Updated", "Updated" },
            productAudit.Select(log => log.Action).ToArray());
    }

    // Reads the audit history of one entity in its own scope.
    private async Task<List<AuditLog>> GetAuditEntriesAsync(
        string entityName,
        int entityId)
    {
        var entityIdText = entityId.ToString();

        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<
                EnterpriseOrderManagementDbContext>();

        return await dbContext.AuditLogs
            .AsNoTracking()
            .Where(log =>
                log.EntityName == entityName &&
                log.EntityId == entityIdText)
            .OrderBy(log => log.Id)
            .ToListAsync();
    }
}