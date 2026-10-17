using System.ComponentModel.DataAnnotations;

namespace EnterpriseOrderManagement.Api.Dtos.Customers;

public sealed class CreateCustomerRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    [RegularExpression(@".*\S.*",
        ErrorMessage = "Name cannot contain only whitespace.")]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [RegularExpression(@"^\S+$", ErrorMessage = "Email cannot contain whitespace.")]
    [StringLength(320)]
    public string Email { get; init; } = string.Empty;
}