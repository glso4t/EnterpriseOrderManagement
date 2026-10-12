using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Services.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);
}