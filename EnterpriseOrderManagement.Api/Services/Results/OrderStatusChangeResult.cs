using EnterpriseOrderManagement.Api.Dtos.Orders;

namespace EnterpriseOrderManagement.Api.Services.Results;

public enum OrderStatusChangeStatus
{
    Success,
    OrderNotFound,  //η παραγγελία δεν υπάρχει
    InvalidTransition,  //η ζητούμενη μετάβαση δεν επιτρέπεται
    ProductNotFound //δεν βρέθηκε προϊόν που απαιτείται για την επιστροφή αποθέματος
}

public sealed record OrderStatusChangeResult(
    OrderStatusChangeStatus Status,
    OrderResponse? Order = null,
    int? RelatedId = null);