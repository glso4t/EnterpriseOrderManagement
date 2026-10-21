using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Dtos.Orders;

namespace EnterpriseOrderManagement.Api.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Order> CreateAsync(
        Order order,
        IReadOnlyCollection<Product> productsToUpdate,
        CancellationToken cancellationToken = default);

    Task<Order> UpdateStatusAsync(
        Order order,
        IReadOnlyCollection<Product> productsToUpdate,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(   //η μέθοδος επιστρέφει τη λίστα των παραγγελιών της συγκεκριμένης σελίδας και το συνολικό πλήθος των παραγγελιών που ταιριάζουν στα φίλτρα
        OrderQueryParameters parameters,
        CancellationToken cancellationToken = default);
}   