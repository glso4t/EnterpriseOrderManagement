using EnterpriseOrderManagement.Api.Dtos.Customers;

namespace EnterpriseOrderManagement.Api.Services.Results;

public enum CustomerWriteStatus
{
    Success,
    NotFound,
    EmailAlreadyExists
}

public sealed record CustomerWriteResult(
    CustomerWriteStatus Status,
    CustomerResponse? Customer = null);