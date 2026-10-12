using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Repositories.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);
}