using EnterpriseOrderManagement.Api.Repositories;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Data;
using Microsoft.EntityFrameworkCore;
using EnterpriseOrderManagement.Api.Middleware;
using EnterpriseOrderManagement.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<EnterpriseOrderManagementDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection")));

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
    IOrderRepository, OrderRepository
    >();

builder.Services.AddScoped<
    IOrderService, 
    OrderService
    >();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }