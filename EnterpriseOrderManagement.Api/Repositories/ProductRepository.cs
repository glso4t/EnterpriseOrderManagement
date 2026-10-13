using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.Repositories;

public class ProductRepository(
    EnterpriseOrderManagementDbContext dbContext)
    : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    { 
        return await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);
    }

    public async Task<Product> AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        dbContext.Products.Add(product);    // λεμε στο EFCore να προσθέσει το αντικείμενο στη μνήμη
        await dbContext.SaveChangesAsync(cancellationToken);    //Γράφονται τα δεδομένα μόνιμα (data persistence) στη SQL Server database
        return product;
    }
}