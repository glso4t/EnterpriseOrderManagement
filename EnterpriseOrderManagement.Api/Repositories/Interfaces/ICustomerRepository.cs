using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Repositories.Interfaces;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Customer> AddAsync(
        Customer customer,
        CancellationToken cancellationToken = default);

    Task<Customer> UpdateAsync(
        Customer customer,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Customer customer,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        int? excludingCustomerId = null,
        CancellationToken cancellationToken = default);
}