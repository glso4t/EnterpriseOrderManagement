using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Dtos.Products;
using EnterpriseOrderManagement.Api.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseOrderManagement.Api.Services;

public class ProductService(
    IProductRepository productRepository,
    ILogger<ProductService> logger,
    IOptions<InventorySettings> inventoryOptions) 
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

    private void LogLowStock(Product product)
    {
        var threshold = inventoryOptions.Value.LowStockThreshold;
        if (product.Stock <= threshold)
        {
            logger.LogWarning(
                "Product {ProductId} has low stock. Stock: {Stock}. Threshold: {Threshold}",
                product.Id,
                product.Stock,
                threshold);
        }
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await productRepository.GetAllAsync(
            cancellationToken);
        logger.LogDebug(
            "Retrieved {ProductCount} products",
            products.Count);
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
        logger.LogDebug(
            "Product with ID {ProductId} was not found",
            id);
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
        logger.LogInformation(
            "Created product with ID {ProductId}",
            createdProduct.Id);
        LogLowStock(createdProduct);
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
            logger.LogDebug(
                "Cannot update product {ProductId} because it was not found",
                id);
            return null;
        }
        product.Name = request.Name.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;
        var updatedProduct = await productRepository.UpdateAsync(
            product,
            cancellationToken);
        logger.LogInformation(
            "Updated product with ID {ProductId}",
            updatedProduct.Id);
        LogLowStock(updatedProduct);
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
            logger.LogDebug(
                "Cannot patch product {ProductId} because it was not found",
                id);
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
        logger.LogInformation(
            "Patched product with ID {ProductId}",
            updatedProduct.Id);
        LogLowStock(updatedProduct);
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
            logger.LogDebug(
                "Cannot delete product {ProductId} because it was not found",
                id);
            return false;
        }
        await productRepository.DeleteAsync(
            product,
            cancellationToken);
        logger.LogInformation(
            "Deleted product with ID {ProductId}",
            id);
        return true;
    }
}