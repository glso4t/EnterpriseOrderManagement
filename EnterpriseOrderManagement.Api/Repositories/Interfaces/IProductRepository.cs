using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Repositories.Interfaces;

public interface IProductRepository
{
    IReadOnlyList<Product> GetAll();
}