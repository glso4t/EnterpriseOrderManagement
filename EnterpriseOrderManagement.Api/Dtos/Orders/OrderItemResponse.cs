namespace EnterpriseOrderManagement.Api.Dtos.Orders;

public sealed class OrderItemResponse
{
    public int ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;
    
    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; } //η αξία της συγκεκριμένης γραμμής: Quantity × UnitPrice
}