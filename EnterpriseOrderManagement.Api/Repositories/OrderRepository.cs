using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.Repositories;

public sealed class OrderRepository(
    EnterpriseOrderManagementDbContext dbContext)
    : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)  //τα items να φορτώνονται μαζί με την παραγγελία
            .FirstOrDefaultAsync(
                order => order.Id == id,
                cancellationToken);
    }

    public async Task<Order> CreateAsync(
        Order order,
        IReadOnlyCollection<Product> productsToUpdate,
        CancellationToken cancellationToken = default)
    {
        foreach (var product in productsToUpdate)
        {
            dbContext.Products.Attach(product);
            dbContext.Entry(product)
                .Property(product => product.Stock)
                .IsModified = true;
        }
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }
}