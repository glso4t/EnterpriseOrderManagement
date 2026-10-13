using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Dtos.Products;

namespace EnterpriseOrderManagement.Api.Services.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Product> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);
}