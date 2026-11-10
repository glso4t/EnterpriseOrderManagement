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
using System.Threading.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

// --- 1. JWT Settings Registration (Μετακινήθηκε εδώ για να είναι διαθέσιμο στο AddAuthentication) ---
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


// --- 2. JWT Bearer Authentication & Validation (Αντικατάσταση του παλιού άδειου AddJwtBearer) ---
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
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

//identity registration
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
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
    .AddEntityFrameworkStores<EnterpriseOrderManagementDbContext>()
    .AddDefaultTokenProviders();


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

await IdentitySeeder.SeedAsync(
    app.Services,
    app.Configuration
);

app.Run();

public partial class Program { }