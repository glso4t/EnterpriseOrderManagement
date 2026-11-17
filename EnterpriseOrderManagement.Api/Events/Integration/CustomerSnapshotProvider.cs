using EnterpriseOrderManagement.Api.Data;

using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.Events.Integration;

public interface ICustomerSnapshotProvider
{
    Task<CustomerSnapshot> GetAsync(
        int customerId,
        CancellationToken cancellationToken = default);
}

public sealed class CustomerSnapshotProvider(
    EnterpriseOrderManagementDbContext dbContext)
    : ICustomerSnapshotProvider
{
    public async Task<CustomerSnapshot> GetAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        // Runs on the caller's DbContext, so it participates in the
        // same transaction and sees consistent data.
        return await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == customerId)
            .Select(customer => new CustomerSnapshot(
                customer.Name,
                customer.Email))
            .SingleAsync(cancellationToken);
    }
}