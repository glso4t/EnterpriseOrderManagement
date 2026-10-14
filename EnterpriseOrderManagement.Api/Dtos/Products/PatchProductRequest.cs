using System.ComponentModel.DataAnnotations;

namespace EnterpriseOrderManagement.Api.Dtos.Products;

public sealed class PatchProductRequest : IValidatableObject
{
    [StringLength(200, MinimumLength = 1)]
    [RegularExpression(@".*\S.*",
        ErrorMessage = "Name cannot contain only whitespace.")]
    public string? Name { get; init; }

    [Range(0.01, 999999.99)]
    public decimal? Price { get; init; }

    [Range(0, int.MaxValue)]
    public int? Stock { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Name is null && Price is null && Stock is null)
        {
            yield return new ValidationResult(
                "At least one field must be provided.");
        }
    }
}