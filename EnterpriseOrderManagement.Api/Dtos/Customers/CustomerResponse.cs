namespace EnterpriseOrderManagement.Api.Dtos.Customers;

public sealed class CustomerResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}