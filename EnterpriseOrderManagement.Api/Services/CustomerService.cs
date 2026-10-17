using EnterpriseOrderManagement.Api.Dtos.Customers;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Services.Results;

namespace EnterpriseOrderManagement.Api.Services;

public sealed class CustomerService(
    ICustomerRepository customerRepository) : ICustomerService
{
    private static CustomerResponse ToResponse(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email
        };
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var customers = await customerRepository.GetAllAsync(
            cancellationToken);
        return customers
            .Select(ToResponse)
            .ToList();
    }

    public async Task<CustomerResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(
            id,
            cancellationToken);
        if (customer is null)
        {
        return null;
        }
        return ToResponse(customer);
    }

    public async Task<CustomerWriteResult> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        if (await customerRepository.EmailExistsAsync(
            email,
            cancellationToken: cancellationToken))
        {
            return new CustomerWriteResult(
                CustomerWriteStatus.EmailAlreadyExists);
        }
        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = email
        };
        var createdCustomer = await customerRepository.AddAsync(
            customer,
            cancellationToken);
        return new CustomerWriteResult(
            CustomerWriteStatus.Success,
            ToResponse(createdCustomer));
    }

    public async Task<CustomerWriteResult> UpdateAsync(
        int id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(
            id,
            cancellationToken);
        if (customer is null)
        {
            return new CustomerWriteResult(
                CustomerWriteStatus.NotFound);
        }
        var email = request.Email.Trim();
        if (await customerRepository.EmailExistsAsync(
            email,
            id,
            cancellationToken))
        {
            return new CustomerWriteResult(
                CustomerWriteStatus.EmailAlreadyExists);
        }
        customer.Name = request.Name.Trim();
        customer.Email = email;
        var updatedCustomer = await customerRepository.UpdateAsync(
            customer,
            cancellationToken);
        return new CustomerWriteResult(
            CustomerWriteStatus.Success,
            ToResponse(updatedCustomer));
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(
            id,
            cancellationToken);
        if (customer is null)
        {
            return false;
        }
        await customerRepository.DeleteAsync(
            customer,
            cancellationToken);
        return true;
    }
}