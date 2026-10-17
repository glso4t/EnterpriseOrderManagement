using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Order> CreateAsync(
        Order order,
        IReadOnlyCollection<Product> productsToUpdate,
        CancellationToken cancellationToken = default);
}