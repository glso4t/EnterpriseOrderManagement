using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EnterpriseOrderManagement.Tests;

public class ProductApiIntegrationTests
    : IClassFixture<SqlServerTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductApiIntegrationTests(
        SqlServerTestWebApplicationFactory factory)
    {
        factory.InitializeDatabase();
        _client = factory.CreateClient();   //δημιουργεί έναν HTTP client έτοιμο να στείλει αιτήματα στο test API
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

    [Fact]
    public async Task GetProducts_WithoutAuthentication_ReturnsUnauthorized()
    {
        //Arrange
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/products");
        request.Headers.Add(
            "X-Test-Unauthenticated",
            "true");
        //Act
        var response = await _client.SendAsync(request);
        //Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task PostProduct_AsUser_ReturnsForbidden()
    {
        //Arrange
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/products")
        {
            Content = JsonContent.Create(new
            {
                name = "Test Product",
                price = 10m,
                stock = 5
            })
        };
        request.Headers.Add(
            "X-Test-Role",
            "User");
        //Act
        var response = await _client.SendAsync(request);
        //Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

}