using System.ComponentModel.DataAnnotations;

namespace EnterpriseOrderManagement.Api.Dtos.Orders;

public sealed class CreateOrderRequest
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "An order must contain at least one item.")]
    public List<CreateOrderItemRequest> Items { get; init; } = new();
}