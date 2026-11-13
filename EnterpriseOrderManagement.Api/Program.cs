
using EnterpriseOrderManagement.Api.Repositories;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Services.Auth;
using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Middleware;
using EnterpriseOrderManagement.Api.Configuration;
using EnterpriseOrderManagement.Api.OpenApi;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Events.Domain;

using System.Threading.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

const string FrontendCorsPolicy = "FrontendPolicy";

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// OpenAPI documentation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "EnterpriseOrderManagement API",
            Version = "v1",
            Description =
                "REST API for managing Products, Customers and Orders."
        };

        return Task.CompletedTask;
    });

    options.AddDocumentTransformer<
        BearerSecuritySchemeTransformer>();

    options.AddOperationTransformer<
        BearerSecurityRequirementTransformer>();
});


// JWT Settings Registration
var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JWT settings are missing."
    );

if (Encoding.UTF8.GetByteCount(jwtSettings.SecretKey) < 32)
{
    throw new InvalidOperationException(
        "JWT SecretKey must contain at least 32 bytes."
    );
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
    string.IsNullOrWhiteSpace(jwtSettings.Audience) ||
    jwtSettings.AccessTokenExpirationMinutes <= 0)
{
    throw new InvalidOperationException(
        "JWT configuration is invalid."
    );
}

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName)
);

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<
    IRefreshTokenService,
    RefreshTokenService
>();

// JWT Bearer Authentication & Validation
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
                ),

                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                NameClaimType = JwtRegisteredClaimNames.Sub,

                RoleClaimType = "role"
            };
    });


// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});


// CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .WithMethods(
                "GET",
                "POST",
                "PUT",
                "PATCH",
                "DELETE")
            .WithHeaders(
                "Authorization",
                "Content-Type",
                "X-Refresh-Request")
            .AllowCredentials();
    });
});


// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey:
                        httpContext.Connection.RemoteIpAddress
                            ?.ToString()
                        ?? "unknown",

                    factory: _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));
});


// Error Handling
builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Provides access to the current authenticated HTTP user.
builder.Services.AddHttpContextAccessor();

// Database
builder.Services.AddDbContext<EnterpriseOrderManagementDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection"))
);


// Identity Registration
// Important: AddIdentityCore does not override the JWT Bearer
// authentication scheme with Identity application cookies.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // User configuration
        options.User.RequireUniqueEmail = true;

        // Password configuration
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Account lockout
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);

        options.Lockout.MaxFailedAccessAttempts = 5;

        options.Lockout.AllowedForNewUsers = true;

        // Development: email confirmation is not yet implemented.
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<EnterpriseOrderManagementDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();


// Inventory Settings
builder.Services.Configure<InventorySettings>(
    builder.Configuration.GetSection(
        InventorySettings.SectionName));


// Product Services
builder.Services.AddScoped<
    IProductRepository,
    ProductRepository
>();

builder.Services.AddScoped<
    IProductService,
    ProductService
>();


// Customer Services
builder.Services.AddScoped<
    ICustomerRepository,
    CustomerRepository
>();

builder.Services.AddScoped<
    ICustomerService,
    CustomerService
>();


// Order Services
builder.Services.AddScoped<
    IOrderRepository,
    OrderRepository
>();

builder.Services.AddScoped<
    IOrderService,
    OrderService
>();


// Domain Events
builder.Services.AddScoped<
    IDomainEventDispatcher,
    DomainEventDispatcher>();

var app = builder.Build();


// Centralized Exception Handling
app.UseExceptionHandler();

app.UseStatusCodePages();


// HSTS only outside Development
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();


// Routing
app.UseRouting();


// CORS
app.UseCors(FrontendCorsPolicy);


// Rate Limiting
app.UseRateLimiter();


// Authentication must run before Authorization.
app.UseAuthentication();

app.UseAuthorization();


// Map Controllers
app.MapControllers();


// OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


// Identity Seeding
// Runs only in Development.
// Integration tests use the Testing environment,
// so seeding does not run before their database migrations.
if (app.Environment.IsDevelopment())
{
    await IdentitySeeder.SeedAsync(
        app.Services,
        app.Configuration
    );
}


app.Run();

public partial class Program { }
