using System.ComponentModel.DataAnnotations;

namespace EnterpriseOrderManagement.Api.Dtos.Products;

public sealed class UpdateProductRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, 999999.99)]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int Stock { get; init; }
}