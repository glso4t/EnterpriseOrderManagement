using EnterpriseOrderManagement.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;

namespace EnterpriseOrderManagement.Tests;

public sealed class SqlServerTestWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _testConnectionString =
        $@"Server=(localdb)\MSSQLLocalDB;Database=EnterpriseOrderManagement_Tests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
    private bool _disposed;

    private bool _databaseInitialized;

    protected override void ConfigureWebHost(   //Παρεμβαίνει στις ρυθμίσεις του web host και αντικαθιστά το κανονικό connection string της εφαρμογής με το προσωρινό test connection string
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

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();   //ανοίγει ένα dependency injection scope
        var dbContext = scope.ServiceProvider
            .GetRequiredService<
                EnterpriseOrderManagementDbContext>();  //παίρνει το Entity Framework DbContext
        // Δημιουργεί την test database και εφαρμόζει
        // όλες τις εκκρεμείς migrations
        dbContext.Database.Migrate();
        _databaseInitialized = true;
    }

    protected override void Dispose(bool disposing) //cleans up resources όταν τελειώσουν τα tests
    {
        if (_disposed) return;
        _disposed = true;
        if (disposing && _databaseInitialized)
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