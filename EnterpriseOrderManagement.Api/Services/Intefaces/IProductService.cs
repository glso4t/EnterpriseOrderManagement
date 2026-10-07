using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Services.Interfaces;

public interface IProductService
{
    IEnumerable<Product> GetAll();
}