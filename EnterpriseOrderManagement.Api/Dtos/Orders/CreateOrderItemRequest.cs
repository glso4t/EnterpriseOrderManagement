using System.ComponentModel.DataAnnotations;

namespace EnterpriseOrderManagement.Api.Dtos.Orders;

public sealed class CreateOrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}