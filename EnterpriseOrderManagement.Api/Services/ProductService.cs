using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Dtos.Products;

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

    public Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return productRepository.GetByIdAsync(
            id,
            cancellationToken);
    }

    public Task<Product> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            Stock = request.Stock
        };
        return productRepository.AddAsync(
            product,
            cancellationToken);
    }

    public async Task<Product?> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(
            id,
            cancellationToken);
        if (product is null)
        {
            return null;
        }
        product.Name = request.Name.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;
        return await productRepository.UpdateAsync(
            product,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(
            id,
            cancellationToken);
        if (product is null)
        {
            return false;
        }
        await productRepository.DeleteAsync(
            product,
            cancellationToken);
        return true;
    }
}