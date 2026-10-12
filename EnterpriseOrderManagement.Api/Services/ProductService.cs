using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services.Interfaces;

namespace EnterpriseOrderManagement.Api.Services;

public class ProductService(
    IProductRepository productRepository)
    : IProductService
{
    public Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return productRepository.GetAllAsync(cancellationToken);
    }
}