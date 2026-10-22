using EnterpriseOrderManagement.Api.Dtos.Customers;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services;
using EnterpriseOrderManagement.Api.Services.Results;
using Moq;

namespace EnterpriseOrderManagement.Tests;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenEmailAlreadyExists_ReturnsConflictResult()
    {
        // Arrange
        var repositoryMock = new Mock<ICustomerRepository>();
        repositoryMock
            .Setup(repository => repository.EmailExistsAsync(
                "goulasgg@gmail.com",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new CustomerService(
            repositoryMock.Object);
        var request = new CreateCustomerRequest
        {
            Name = "Goulas Geo",
            Email = "goulasgg@gmail.com"
        };
        // Act
        var result = await service.CreateAsync(request);
        // Assert
        Assert.Equal(
            CustomerWriteStatus.EmailAlreadyExists,
            result.Status);
        Assert.Null(result.Customer);   //δεν επιστρέφει customer object αφού υπήρχε πρόβλημα
        repositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Customer>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailIsAvailable_CreatesCustomer()
    {
        // Arrange
        var repositoryMock = new Mock<ICustomerRepository>();
        repositoryMock
            .Setup(repository => repository.EmailExistsAsync(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Customer>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer
            {
                Id = 42,
                Name = "Goulas Kostis",
                Email = "kkk123@gmail.com"
            });
        var service = new CustomerService(
            repositoryMock.Object);
        var request = new CreateCustomerRequest
        {
            Name = "  Goulas Kostis  ",
            Email = "  kkk123@gmail.com  "
        };
        // Act
        var result = await service.CreateAsync(request);
        // Assert
        Assert.Equal(
            CustomerWriteStatus.Success,
            result.Status);
        Assert.NotNull(result.Customer);
        Assert.Equal(42, result.Customer.Id);
        Assert.Equal("Goulas Kostis", result.Customer.Name);
        Assert.Equal("kkk123@gmail.com", result.Customer.Email);
        repositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Customer>(customer =>
                    customer.Name == "Goulas Kostis"
                    && customer.Email == "kkk123@gmail.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}