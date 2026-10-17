using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.Repositories;

public sealed class CustomerRepository(
    EnterpriseOrderManagementDbContext dbContext)
    : ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                customer => customer.Id == id,
                cancellationToken);
    }

    public async Task<Customer> AddAsync(
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        dbContext.Customers.Add(customer);    // λεμε στο EFCore να προσθέσει το αντικείμενο στη μνήμη
        await dbContext.SaveChangesAsync(cancellationToken);    //Γράφονται τα δεδομένα μόνιμα (data persistence) στη SQL Server database
        return customer;
    }

    public async Task<Customer> UpdateAsync(
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        dbContext.Customers.Update(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task DeleteAsync(
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        dbContext.Customers.Remove(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        int? excludingCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Customer> query = dbContext.Customers;
        if (excludingCustomerId.HasValue)
        {
            var customerId = excludingCustomerId.Value;
            query = query.Where(
                customer => customer.Id != customerId);
        }
        return await query.AnyAsync(    //ελέγχει αν υπάρχει έστω μία εγγραφή που ικανοποιεί την προϋπόθεση
            customer => customer.Email == email,
            cancellationToken);
    }
}