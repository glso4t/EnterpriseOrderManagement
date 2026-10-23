using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Dtos.Orders;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services.Queries;
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

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
            OrderQuery query,
            CancellationToken cancellationToken = default)
    {
        IQueryable<Order> ordersQuery = dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
                .ThenInclude(item => item.Product);
        //Ελέγχει αν ο χρήστης έδωσε φίλτρα,
        //αν κάποιο φίλτρο έχει τιμή, προσθέτει αυτόματα το αντίστοιχο WHERE στο ερώτημα της SQL
        if (query.CustomerId.HasValue)
        {
            ordersQuery = ordersQuery.Where(
                order => order.CustomerId == query.CustomerId.Value);
        }
        if (query.Status.HasValue)
        {
            ordersQuery = ordersQuery.Where(
                order => order.Status == query.Status.Value);
        }
        if (query.FromDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(
                order => order.OrderDate >= query.FromDate.Value);
        }
        if (query.ToDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(
                order => order.OrderDate <= query.ToDate.Value);
        }
        var totalCount = await ordersQuery.CountAsync(cancellationToken);//μετράει πόσες συνολικά παραγγελίες ταιριάζουν με τα φίλτρα που βάλαμε (χωρίς να επηρεάζεται από τη σελιδοποίηση)
        var descending = string.Equals(
            query.SortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);
        //Δυναμική Ταξινόμηση (Sorting)
        IOrderedQueryable<Order> orderedQuery =
            query.SortBy.ToLowerInvariant() switch //πώς θέλει o χρήστης να sort τις παραγγελίες
            {
                "id" => descending
                    ? ordersQuery.OrderByDescending(order => order.Id)
                    : ordersQuery.OrderBy(order => order.Id),
                "customerid" => descending
                    ? ordersQuery.OrderByDescending(order => order.CustomerId)
                    : ordersQuery.OrderBy(order => order.CustomerId),
                _ => descending //όλα τα άλλα
                    ? ordersQuery.OrderByDescending(order => order.OrderDate) //προεπιλογή το OrderDate
                    : ordersQuery.OrderBy(order => order.OrderDate)
            };
        orderedQuery = orderedQuery.ThenByDescending(   //αν βρει orders με την ίδια ημερομηνία,
            order => order.Id);                         // sort βάσει του id τους
        var items = await orderedQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}