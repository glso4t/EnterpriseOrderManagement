namespace EnterpriseOrderManagement.Api.Dtos.Orders;

public sealed class OrderItemResponse
{
    public int ProductId { get; init; }

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; } //η αξία της συγκεκριμένης γραμμής: Quantity × UnitPrice
}