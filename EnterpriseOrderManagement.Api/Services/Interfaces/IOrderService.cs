using EnterpriseOrderManagement.Api.Dtos.Orders;
using EnterpriseOrderManagement.Api.Services.Results;
using EnterpriseOrderManagement.Api.Dtos.Common;
using EnterpriseOrderManagement.Api.Dtos.Orders;

namespace EnterpriseOrderManagement.Api.Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OrderCreationResult> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderStatusChangeResult> ConfirmAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OrderStatusChangeResult> CancelAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<OrderResponse>> GetPagedAsync(
        OrderQueryParameters parameters,
        CancellationToken cancellationToken = default);
}