using System.ComponentModel.DataAnnotations;
using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Dtos.Orders;

public sealed class OrderQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    public int? CustomerId { get; init; }

    public OrderStatus? Status { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public string SortBy { get; init; } = "orderDate";

    public string SortDirection { get; init; } = "desc";
}