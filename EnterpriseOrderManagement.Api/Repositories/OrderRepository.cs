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
                .ThenInclude(item => item.Product)  //και τα αντίστοιχα προϊόντα
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

public async Task<Order> UpdateStatusAsync(
    Order order,
    IReadOnlyCollection<Product> productsToUpdate,
    CancellationToken cancellationToken = default)
{
    var orderToUpdate = new Order    // Δημιουργούμε μια ελαφριά οντότητα για την ενημέρωση της κατάστασης
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        OrderDate = order.OrderDate,
        Status = order.Status,
        RowVersion = order.RowVersion
    };
    dbContext.Orders.Attach(orderToUpdate);
    dbContext.Entry(orderToUpdate)    // Αλλάζουμε μόνο το Status
        .Property(entity => entity.Status)
        .IsModified = true;
    foreach (var product in productsToUpdate)    // Προσθέτουμε τις αλλαγές αποθέματος στην ίδια αποθήκευση
    {
        dbContext.Products.Attach(product);
        dbContext.Entry(product)
            .Property(entity => entity.Stock)
            .IsModified = true;
    }
    await dbContext.SaveChangesAsync(cancellationToken);
    order.RowVersion = orderToUpdate.RowVersion;    // Διατηρούμε στο αρχικό object τη νέα έκδοση που παρήγαγε η βάση
    return order;
}
}