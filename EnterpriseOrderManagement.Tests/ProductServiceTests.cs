using EnterpriseOrderManagement.Api.Configuration;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EnterpriseOrderManagement.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        // Arrange
        const int productId = 10;
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        var loggerMock = new Mock<ILogger<ProductService>>();
        var optionsMock = Options.Create(new InventorySettings());
        var productService = new ProductService(
            repositoryMock.Object,
            loggerMock.Object,
            optionsMock);
        // Act
        var result = await productService.GetByIdAsync(
            productId,
            CancellationToken.None);
        // Assert
        Assert.Null(result);
        repositoryMock.Verify(
            repository => repository.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProductResponse()
    {
        // Arrange
        const int productId = 10;
        var product = new Product
        {
            Id = productId,
            Name = "Edding 142M Stylus",
            Price = 45,
            Stock = 20
        };
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        var loggerMock = new Mock<ILogger<ProductService>>();
        var optionsMock = Options.Create(new InventorySettings());
        var productService = new ProductService(
            repositoryMock.Object,
            loggerMock.Object,
            optionsMock);
        // Act
        var result = await productService.GetByIdAsync(
            productId,
            CancellationToken.None);
        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Edding 142M Stylus", result.Name);
        Assert.Equal(45m, result.Price);
        Assert.Equal(20, result.Stock);
        repositoryMock.Verify(
            repository => repository.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}