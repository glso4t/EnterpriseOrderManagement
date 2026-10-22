using EnterpriseOrderManagement.Api.Dtos.Products;
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

    [Fact]
    public async Task CreateAsync_TrimsName_ReturnsCreatedProduct() //Ελέγχουμε ότι το ProductService δίνει στο repository ένα Product με καθαρισμένο όνομα και σωστές τιμές
    {
        // Arrange
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Product>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product
            {
                Id = 15,
                Name = "Keyboard",
                Price = 45m,
                Stock = 8
            });
        var loggerMock = new Mock<ILogger<ProductService>>();
        var options = Options.Create(
            new InventorySettings
            {
                LowStockThreshold = 5
            });
        var service = new ProductService(
            repositoryMock.Object,
            loggerMock.Object,
            options);
        var request = new CreateProductRequest
        {
            Name = "  Keyboard  ",
            Price = 45m,
            Stock = 8
        };
        // Act
        var result = await service.CreateAsync(
            request,
            CancellationToken.None);
        // Assert
        Assert.Equal(15, result.Id);
        Assert.Equal("Keyboard", result.Name);
        Assert.Equal(45m, result.Price);
        Assert.Equal(8, result.Stock);
        repositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Product>(product =>
                    product.Name == "Keyboard"
                    && product.Price == 45m
                    && product.Stock == 8),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}