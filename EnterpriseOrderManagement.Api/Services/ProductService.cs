using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Dtos.Products;

namespace EnterpriseOrderManagement.Api.Services;

public class ProductService(
    IProductRepository productRepository)
    : IProductService
{
    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        };
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await productRepository.GetAllAsync(
            cancellationToken);

        return products
            .Select(ToResponse)
            .ToList();    
    }

    public async Task<ProductResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(
            id,
            cancellationToken);
        if (product is null)
        {
            return null;
        }
        return ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            Stock = request.Stock
        };
        var createdProduct = await productRepository.AddAsync(
            product,
            cancellationToken);
        return ToResponse(createdProduct);
    }

    public async Task<ProductResponse?> UpdateAsync(
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
        var updatedProduct = await productRepository.UpdateAsync(
            product,
            cancellationToken);
        return ToResponse(updatedProduct);
    }

    public async Task<ProductResponse?> PatchAsync(
        int id,
        PatchProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(
            id,
            cancellationToken);
        if (product is null)
        {
            return null;
        }
        if (request.Name is not null)
        {
            product.Name = request.Name.Trim();
        }
        if (request.Price.HasValue)
        {
            product.Price = request.Price.Value;    //To .Value προκύπτει από τον τύπο δεδομένων του πεδίου, επειδή το κάναμε Nullable (decimal? και int?).
        }
        if (request.Stock.HasValue)
        {
            product.Stock = request.Stock.Value;    //To .Value προκύπτει από τον τύπο δεδομένων του πεδίου, επειδή το κάναμε Nullable (decimal? και int?).
        }
        var updatedProduct = await productRepository.UpdateAsync(
            product,
            cancellationToken);
        return ToResponse(updatedProduct);
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