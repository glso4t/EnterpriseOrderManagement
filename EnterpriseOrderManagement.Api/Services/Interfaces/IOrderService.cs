using EnterpriseOrderManagement.Api.Dtos.Orders;
using EnterpriseOrderManagement.Api.Services.Results;

namespace EnterpriseOrderManagement.Api.Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OrderCreationResult> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);
}