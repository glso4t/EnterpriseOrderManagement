namespace EnterpriseOrderManagement.Api.Models;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Customer Customer { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; }
        = new List<OrderItem>();
}