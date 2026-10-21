using System.ComponentModel.DataAnnotations;
using System.Linq;
using EnterpriseOrderManagement.Api.Dtos.Products;
using EnterpriseOrderManagement.Api.Dtos.Orders;

namespace EnterpriseOrderManagement.Tests;

public class DtoValidationTests
{
    [Fact]
    public void CreateProductRequest_WithZeroPrice_FailsValidation()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Keyboard",
            Price = 0,
            Stock = 10
        };
        // Act
        var errors = Validate(request);
        // Assert
        Assert.Contains(
            errors,
            error => error.MemberNames.Contains(
                nameof(request.Price)));
    }

    [Fact]
    public void CreateProductRequest_WithNegativeStock_FailsValidation()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Keyboard",
            Price = 45,
            Stock = -1
        };
        // Act
        var errors = Validate(request);
        // Assert
        Assert.Contains(
            errors,
            error => error.MemberNames.Contains(
                nameof(request.Stock)));
    }

    [Fact]
    public void CreateProductRequest_WithValidValues_PassesValidation()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Keyboard",
            Price = 45,
            Stock = 10
        };
        // Act
        var errors = Validate(request);
        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void CreateOrderItemRequest_WithZeroQuantity_FailsValidation()
    {
        // Arrange
        var request = new CreateOrderItemRequest
        {
            ProductId = 1,
            Quantity = 0
        };
        // Act
        var errors = Validate(request);
        // Assert
        Assert.Contains(
            errors,
            error => error.MemberNames.Contains(
                nameof(request.Quantity)));
    }

    [Fact]
    public void CreateOrderItemRequest_WithValidValues_PassesValidation()
    {
        // Arrange
        var request = new CreateOrderItemRequest
        {
            ProductId = 1,
            Quantity = 2
        };
        // Act
        var errors = Validate(request);
        // Assert
        Assert.Empty(errors);
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            errors,
            validateAllProperties: true);
        return errors;
    }
}