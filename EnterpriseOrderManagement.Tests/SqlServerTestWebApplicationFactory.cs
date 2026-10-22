using EnterpriseOrderManagement.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseOrderManagement.Tests;

public sealed class SqlServerTestWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _testConnectionString =
        $@"Server=(localdb)\MSSQLLocalDB;Database=EnterpriseOrderManagement_Tests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
    private bool _disposed;

    private bool _databaseInitialized;

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            _testConnectionString
                    });
            });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<
                EnterpriseOrderManagementDbContext>();
        // Δημιουργεί την test database και εφαρμόζει
        // όλες τις εκκρεμείς migrations
        dbContext.Database.Migrate();
        _databaseInitialized = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (disposing)
        {
            try
            {
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider
                    .GetRequiredService<EnterpriseOrderManagementDbContext>();
                db.Database.EnsureDeleted();
            }
            catch (ObjectDisposedException)
            {
                // ο host έχει ήδη γίνει dispose
            }
        }
        base.Dispose(disposing); // ΠΑΝΤΑ τελευταίο
    }
}