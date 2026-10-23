using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Services.Queries;

public sealed class OrderQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public int? CustomerId { get; init; }

    public OrderStatus? Status { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public string SortBy { get; init; } = "orderDate";

    public string SortDirection { get; init; } = "desc";
}