namespace EnterpriseOrderManagement.Api.Dtos.Orders;

public sealed class OrderResponse
{
    public int Id { get; init; }

    public int CustomerId { get; init; }

    public DateTime OrderDate { get; init; }

    public string Status { get; init; } = string.Empty;

    public IReadOnlyList<OrderItemResponse> Items { get; init; }
        = Array.Empty<OrderItemResponse>();

    public decimal TotalAmount { get; init; }   //Σ (Quantity × UnitPrice)
}