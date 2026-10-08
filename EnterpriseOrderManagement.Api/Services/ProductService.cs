using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services.Interfaces;

namespace EnterpriseOrderManagement.Api.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public IReadOnlyList<Product> GetAll()
    {
        return _productRepository.GetAll();
    }
}