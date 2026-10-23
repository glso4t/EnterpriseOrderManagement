using System.Net;
using System.Net.Http.Json;
using EnterpriseOrderManagement.Api.Dtos.Customers;
using EnterpriseOrderManagement.Api.Dtos.Orders;
using EnterpriseOrderManagement.Api.Dtos.Products;

namespace EnterpriseOrderManagement.Tests;

public class OrderLifecycleIntegrationTests
    : IClassFixture<SqlServerTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrderLifecycleIntegrationTests(
        SqlServerTestWebApplicationFactory factory)
    {
        factory.InitializeDatabase();
        _client = factory.CreateClient();
    }

    [Fact]  //πλήρες End-to-End (E2E) test
    public async Task CreateConfirmCancelOrder_UpdatesAndRestoresStock()
    {
        // CREATE CUSTOMER
        var customerResponse = await _client.PostAsJsonAsync(
            "/api/v1/customers",
            new
            {
                name = "Integration Test Customer",
                email = $"customer-{Guid.NewGuid():N}@example.com"
            });
        Assert.Equal(
            HttpStatusCode.Created,
            customerResponse.StatusCode);
        var customer = await customerResponse.Content
            .ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);
        // CHECKS INITIAL STOCK
        var initialProduct = await _client
            .GetFromJsonAsync<ProductResponse>(
                "/api/v1/products/1");
        Assert.NotNull(initialProduct);
        Assert.Equal(10, initialProduct.Stock);
        // CREATES ORDER FOR TWO LAPTOPS
        var orderResponse = await _client.PostAsJsonAsync(
            "/api/v1/orders",
            new
            {
                customerId = customer.Id,
                items = new[]
                {
                    new
                    {
                        productId = 1,
                        quantity = 2
                    }
                }
            });
        Assert.Equal(
            HttpStatusCode.Created,
            orderResponse.StatusCode);
        var order = await orderResponse.Content
            .ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.True(order.Id > 0);
        Assert.Equal("Pending", order.Status);
        Assert.Single(order.Items);
        Assert.Equal(2, order.Items[0].Quantity);
        Assert.Equal(899.99m, order.Items[0].UnitPrice);
        Assert.Equal(1799.98m, order.TotalAmount);
        // STOCK MUST DECREASE FROM 10 TO 8
        var productAfterOrder = await _client
            .GetFromJsonAsync<ProductResponse>(
                "/api/v1/products/1");
        Assert.NotNull(productAfterOrder);
        Assert.Equal(8, productAfterOrder.Stock);
        // CONFIRM ORDER
        var confirmResponse = await _client.PostAsync(
            $"/api/v1/orders/{order.Id}/confirm",
            content: null);
        Assert.Equal(
            HttpStatusCode.OK,
            confirmResponse.StatusCode);
        var confirmedOrder = await confirmResponse.Content
            .ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(confirmedOrder);
        Assert.Equal("Confirmed", confirmedOrder.Status);
        // CANCEL ORDER
        var cancelResponse = await _client.PostAsync(
            $"/api/v1/orders/{order.Id}/cancel",
            content: null);
        Assert.Equal(
            HttpStatusCode.OK,
            cancelResponse.StatusCode);
        var cancelledOrder = await cancelResponse.Content
            .ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(cancelledOrder);
        Assert.Equal("Cancelled", cancelledOrder.Status);
        // STOCK MUST RETURN TO 10
        var productAfterCancellation = await _client
            .GetFromJsonAsync<ProductResponse>(
                "/api/v1/products/1");
        Assert.NotNull(productAfterCancellation);
        Assert.Equal(10, productAfterCancellation.Stock);
        // VERIFY THE ORDER IS PERSISTED
        var persistedOrder = await _client
            .GetFromJsonAsync<OrderResponse>(
                $"/api/v1/orders/{order.Id}");
        Assert.NotNull(persistedOrder);
        Assert.Equal("Cancelled", persistedOrder.Status);
        Assert.Equal(1799.98m, persistedOrder.TotalAmount);
    }
}