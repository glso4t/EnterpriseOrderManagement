using EnterpriseOrderManagement.Api.Dtos.Orders;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services;
using EnterpriseOrderManagement.Api.Services.Results;
using Moq;

namespace EnterpriseOrderManagement.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_WithDuplicateProductIds_ReturnsFailure()
    {
        // Arrange
        var orderRepositoryMock =
            new Mock<IOrderRepository>();
        var customerRepositoryMock =
            new Mock<ICustomerRepository>();
        var productRepositoryMock =
            new Mock<IProductRepository>();
        var service = new OrderService(
            orderRepositoryMock.Object,
            customerRepositoryMock.Object,
            productRepositoryMock.Object);
        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = 7,
                    Quantity = 1
                },
                new()
                {
                    ProductId = 7,
                    Quantity = 2
                }
            }
        };
        // Act
        var result = await service.CreateAsync(request);
        // Assert
        Assert.Equal(
            OrderCreationStatus.DuplicateProductItems,
            result.Status);
        customerRepositoryMock.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        orderRepositoryMock.Verify(
            repository => repository.CreateAsync(
                It.IsAny<Order>(),
                It.IsAny<IReadOnlyCollection<Product>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenStockIsInsufficient_DoesNotCreateOrder()
    {
        // Arrange
        const int customerId = 3;
        const int productId = 7;
        var orderRepositoryMock =
            new Mock<IOrderRepository>();
        var customerRepositoryMock =
            new Mock<ICustomerRepository>();
        var productRepositoryMock =
            new Mock<IProductRepository>();
        customerRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                customerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer
            {
                Id = customerId,
                Name = "Test Customer",
                Email = "test@example.com"
            });
        var product = new Product
        {
            Id = productId,
            Name = "SSD",
            Price = 50m,
            Stock = 1
        };
        productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        var service = new OrderService(
            orderRepositoryMock.Object,
            customerRepositoryMock.Object,
            productRepositoryMock.Object);
        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 2
                }
            }
        };
        // Act
        var result = await service.CreateAsync(request);
        // Assert
        Assert.Equal(
            OrderCreationStatus.InsufficientStock,
            result.Status);
        Assert.Equal(productId, result.RelatedId);
        Assert.Equal(1, product.Stock);
        orderRepositoryMock.Verify(
            repository => repository.CreateAsync(
                It.IsAny<Order>(),
                It.IsAny<IReadOnlyCollection<Product>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAsync_WhenOrderIsAlreadyCancelled_DoesNotRestoreStockAgain()
    {
        // Arrange
        const int orderId = 10;
        var orderRepositoryMock =
            new Mock<IOrderRepository>();
        var customerRepositoryMock =
            new Mock<ICustomerRepository>();
        var productRepositoryMock =
            new Mock<IProductRepository>();
        orderRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                Id = orderId,
                CustomerId = 1,
                Status = OrderStatus.Cancelled,
                Items = new List<OrderItem>
                {
                    new()
                    {
                        ProductId = 7,
                        Quantity = 2
                    }
                }
            });
        var service = new OrderService(
            orderRepositoryMock.Object,
            customerRepositoryMock.Object,
            productRepositoryMock.Object);
        // Act
        var result = await service.CancelAsync(orderId);
        // Assert
        Assert.Equal(
            OrderStatusChangeStatus.InvalidTransition,
            result.Status);
        productRepositoryMock.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        orderRepositoryMock.Verify(
            repository => repository.UpdateStatusAsync(
                It.IsAny<Order>(),
                It.IsAny<IReadOnlyCollection<Product>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}