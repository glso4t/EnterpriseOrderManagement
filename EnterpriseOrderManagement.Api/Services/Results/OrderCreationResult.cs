using EnterpriseOrderManagement.Api.Dtos.Orders;

namespace EnterpriseOrderManagement.Api.Services.Results;

public enum OrderCreationStatus
{
    Success,
    CustomerNotFound,
    ProductNotFound,
    InsufficientStock,
    DuplicateProductItems
}

public sealed record OrderCreationResult(
    OrderCreationStatus Status,
    OrderResponse? Order = null,
    int? RelatedId = null); //το ID του πελάτη ή του προϊόντος που σχετίζεται με το σφάλμα