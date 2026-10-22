
using EnterpriseOrderManagement.Api.Repositories;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Middleware;
using EnterpriseOrderManagement.Api.Configuration;
using EnterpriseOrderManagement.Api.OpenApi;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.OpenApi;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

const string FrontendCorsPolicy = "FrontendPolicy";


var builder = WebApplication.CreateBuilder(args);

//controllers
builder.Services.AddControllers();

//OpenAPI documentation
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

// JWT Authentication: Αναγνώριση του χρήστη μέσω JWT Bearer
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Authorization: Έλεγχος δικαιωμάτων
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
                "Content-Type");
    });
});

// Rate limiting
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


//error handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

//db
builder.Services.AddDbContext<EnterpriseOrderManagementDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection")));

//inventory settings
builder.Services.Configure<InventorySettings>(
    builder.Configuration.GetSection(
        InventorySettings.SectionName));

builder.Services.AddScoped<
    IProductRepository,
    ProductRepository
>();

builder.Services.AddScoped<
    IProductService,
    ProductService
>();

builder.Services.AddScoped<
    ICustomerRepository, 
    CustomerRepository
    >();

builder.Services.AddScoped<
    ICustomerService, 
    CustomerService
    >();

builder.Services.AddScoped<
    IOrderRepository, 
    OrderRepository
    >();

builder.Services.AddScoped<
    IOrderService, 
    OrderService
    >();

var app = builder.Build();

// Centralized exception handling
app.UseExceptionHandler();
app.UseStatusCodePages();

// HSTS only outside Development
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Routing must run before CORS and endpoint-aware middleware.
app.UseRouting();

// CORS before authentication/authorization
app.UseCors(FrontendCorsPolicy);

// Limit incoming request rates
app.UseRateLimiter();

// Πρώτα αναγνωρίζουμε τον χρήστη
app.UseAuthentication();

// και μετά ελέγχουμε τα δικαιώματά του
app.UseAuthorization();

//map endpoints
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

public partial class Program { }