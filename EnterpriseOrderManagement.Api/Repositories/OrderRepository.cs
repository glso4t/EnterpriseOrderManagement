using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Dtos.Orders;
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

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
            OrderQueryParameters parameters,
            CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
                .ThenInclude(item => item.Product);
        //Ελέγχει αν ο χρήστης έδωσε φίλτρα,
        //αν κάποιο φίλτρο έχει τιμή, προσθέτει αυτόματα το αντίστοιχο WHERE στο ερώτημα της SQL
        if (parameters.CustomerId.HasValue)
        {
            query = query.Where(
                order => order.CustomerId == parameters.CustomerId.Value);
        }
        if (parameters.Status.HasValue)
        {
            query = query.Where(
                order => order.Status == parameters.Status.Value);
        }
        if (parameters.FromDate.HasValue)
        {
            query = query.Where(
                order => order.OrderDate >= parameters.FromDate.Value);
        }
        if (parameters.ToDate.HasValue)
        {
            query = query.Where(
                order => order.OrderDate <= parameters.ToDate.Value);
        }
        var totalCount = await query.CountAsync(cancellationToken);//μετράει πόσες συνολικά παραγγελίες ταιριάζουν με τα φίλτρα που βάλαμε (χωρίς να επηρεάζεται από τη σελιδοποίηση)
        var descending = string.Equals(
            parameters.SortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);
        //Δυναμική Ταξινόμηση (Sorting)
        IOrderedQueryable<Order> orderedQuery =
            parameters.SortBy.ToLowerInvariant() switch //πώς θέλει o χρήστης να sort τις παραγγελίες
            {
                "id" => descending
                    ? query.OrderByDescending(order => order.Id)
                    : query.OrderBy(order => order.Id),
                "customerid" => descending
                    ? query.OrderByDescending(order => order.CustomerId)
                    : query.OrderBy(order => order.CustomerId),
                _ => descending //όλα τα άλλα
                    ? query.OrderByDescending(order => order.OrderDate) //προεπιλογή το OrderDate
                    : query.OrderBy(order => order.OrderDate)
            };
        orderedQuery = orderedQuery.ThenByDescending(   //αν βρει orders με την ίδια ημερομηνία,
            order => order.Id);                         // sort βάσει του id τους
        var items = await orderedQuery
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}