using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EnterpriseOrderManagement.Tests;

public class ProductApiIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductApiIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostProduct_WithZeroPrice_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            name = "Invalid Product",
            price = 0,
            stock = 10
        };
        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/products",
            request);
        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithInvalidPagination_ReturnsBadRequest()
    {
        // Arrange
        const string url = "/api/orders?page=0&pageSize=500";
        // Act
        var response = await _client.GetAsync(url);
        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        const string url =
            "/api/orders?fromDate=2026-12-31T00%3A00%3A00"
            + "&toDate=2026-01-01T00%3A00%3A00";
        // Act
        var response = await _client.GetAsync(url);
        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}