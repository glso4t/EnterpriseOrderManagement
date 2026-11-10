
using EnterpriseOrderManagement.Api.Data;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnterpriseOrderManagement.Tests;

public sealed class SqlServerTestWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _testConnectionString =
        $@"Server=(localdb)\MSSQLLocalDB;Database=EnterpriseOrderManagement_Tests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";

    private bool _disposed;

    private bool _databaseInitialized;


    // Configure settings BEFORE Program executes.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    // Isolated database for this test fixture.
                    ["ConnectionStrings:DefaultConnection"] =
                        _testConnectionString,

                    // Test-only JWT configuration.
                    ["Jwt:Issuer"] =
                        "EnterpriseOrderManagement.Tests",

                    ["Jwt:Audience"] =
                        "EnterpriseOrderManagement.Tests",

                    ["Jwt:SecretKey"] =
                        "EOM-TEST-ONLY-SECRET-KEY-AT-LEAST-32-BYTES-2026",

                    ["Jwt:AccessTokenExpirationMinutes"] =
                        "15",

                    // Identity seeding must not run before migrations.
                    ["IdentitySeeding:Enabled"] = "false"
                });
        });

        return base.CreateHost(builder);
    }


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme =
                        TestAuthHandler.SchemeName;

                    options.DefaultAuthenticateScheme =
                        TestAuthHandler.SchemeName;

                    options.DefaultChallengeScheme =
                        TestAuthHandler.SchemeName;

                    options.DefaultForbidScheme =
                        TestAuthHandler.SchemeName;
                })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }


    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<
                EnterpriseOrderManagementDbContext>();

        // Create the test database and apply all migrations.
        dbContext.Database.Migrate();

        _databaseInitialized = true;
    }


    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing && _databaseInitialized)
        {
            try
            {
                using var scope = Services.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<
                        EnterpriseOrderManagementDbContext>();

                db.Database.EnsureDeleted();
            }
            catch (ObjectDisposedException)
            {
                // The host has already been disposed.
            }
        }

        base.Dispose(disposing);
    }
}
