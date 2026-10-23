using EnterpriseOrderManagement.Api.Dtos.Orders;
using EnterpriseOrderManagement.Api.Dtos.Common;
using EnterpriseOrderManagement.Api.Dtos.Orders;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Repositories.Interfaces;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Services.Results;
using EnterpriseOrderManagement.Api.Services.Queries;

namespace EnterpriseOrderManagement.Api.Services;

public sealed class OrderService(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository) : IOrderService
{
    private static OrderResponse ToResponse(Order order)
    {
        var items = order.Items
            .Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice
            })
            .ToList();
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            Items = items,
            TotalAmount = items.Sum(item => item.LineTotal)
        };
    }

    public async Task<OrderResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(
            id,
            cancellationToken);
        if(order is null)
        {
            return null;
        }
        return ToResponse(order);
    }

    public async Task<OrderCreationResult> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var productIds = request.Items
            .Select(item => item.ProductId)
            .ToList();
        if (productIds.Distinct().Count() != productIds.Count)  //το Distinct() αφαιρεί τα διπλότυπα. Αν ο αριθμός των μοναδικών προϊόντων διαφέρει από τον συνολικό αριθμό, σταματάει αμέσως και επιστρέφει σφάλμα
        {
            return new OrderCreationResult(
                OrderCreationStatus.DuplicateProductItems);
        }
        var customer = await customerRepository.GetByIdAsync(
            request.CustomerId,
            cancellationToken);
        if (customer is null)   //έλεγχος αν υπάρχει ο customer
        {
            return new OrderCreationResult(
                OrderCreationStatus.CustomerNotFound,
                RelatedId: request.CustomerId);
        }
        var productsToUpdate = new List<Product>();
        var orderItems = new List<OrderItem>();
        foreach (var itemRequest in request.Items)
        {
            var product = await productRepository.GetByIdAsync(
                itemRequest.ProductId,
                cancellationToken);
            if (product is null)    //Για κάθε item, έλεγχος αν υπάρχει το product
            {
                return new OrderCreationResult(
                    OrderCreationStatus.ProductNotFound,
                    RelatedId: itemRequest.ProductId);
            }
            if (product.Stock < itemRequest.Quantity)   //έλεγχος αν υπάρχει αρκετό stock για το ζητούμενο quantity
            {
                return new OrderCreationResult(
                    OrderCreationStatus.InsufficientStock,
                    RelatedId: product.Id);
            }
            product.Stock -= itemRequest.Quantity;  //Μείωση του stock στη μνήμη
            productsToUpdate.Add(product);
            orderItems.Add(new OrderItem    //δημιουργία της γραμμής παραγγελίας
            {
                ProductId = product.Id,
                Product = product,
                Quantity = itemRequest.Quantity,
                UnitPrice = product.Price   //Κλειδώνουμε την τρέχουσα τιμή
            });
        }
        var order = new Order   //δημιουργία Order και ανάθεση τις γραμμές
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items = orderItems
        };
        var createdOrder = await orderRepository.CreateAsync(
            order,
            productsToUpdate,
            cancellationToken);
        return new OrderCreationResult(
            OrderCreationStatus.Success,
            ToResponse(createdOrder));
    }

    public async Task<OrderStatusChangeResult> ConfirmAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(
            id,
            cancellationToken);
        if (order is null)
        {
            return new OrderStatusChangeResult(
                OrderStatusChangeStatus.OrderNotFound);
        }
        if (order.Status != OrderStatus.Pending)    //Μια order μπορεί να confirmed μόνο αν είναι σε κατάσταση Pending
        {
            return new OrderStatusChangeResult(
                OrderStatusChangeStatus.InvalidTransition);
        }
        order.Status = OrderStatus.Confirmed;
        var updatedOrder = await orderRepository.UpdateStatusAsync(
            order,
            Array.Empty<Product>(),
            cancellationToken);

        return new OrderStatusChangeResult(
            OrderStatusChangeStatus.Success,
            ToResponse(updatedOrder));
    }

    public async Task<OrderStatusChangeResult> CancelAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(
            id,
            cancellationToken);
        if (order is null)
        {
            return new OrderStatusChangeResult(
                OrderStatusChangeStatus.OrderNotFound);
        }
        if (order.Status is not (
            OrderStatus.Pending or OrderStatus.Confirmed))  //Μπορούμε να ακυρώσουμε μια παραγγελία μόνο αν είναι Pending ή Confirmed
        {   // Αν είναι ήδη Cancelled, ο έλεγχος αποτυγχάνει (αποφεύγουμε να επιστραφεί το stock διπλή φορά)
            return new OrderStatusChangeResult(
                OrderStatusChangeStatus.InvalidTransition);
        }
        var productsToUpdate = new List<Product>();
        foreach (var item in order.Items)   //Επιστροφή των προϊόντων στο Stock
        {
            var product = await productRepository.GetByIdAsync(
                item.ProductId,
                cancellationToken);
            if (product is null)
            {
                return new OrderStatusChangeResult(
                    OrderStatusChangeStatus.ProductNotFound,
                    RelatedId: item.ProductId);
            }
            product.Stock += item.Quantity; //// επιστρέφουμε τα τεμάχια πίσω
            productsToUpdate.Add(product);
        }
        order.Status = OrderStatus.Cancelled;
        var updatedOrder = await orderRepository.UpdateStatusAsync(
            order,
            productsToUpdate,
            cancellationToken);
        return new OrderStatusChangeResult(
            OrderStatusChangeStatus.Success,
            ToResponse(updatedOrder));
    }

    public async Task<PagedResponse<OrderResponse>> GetPagedAsync(
    OrderQuery query,
    CancellationToken cancellationToken = default)
    {
        var result = await orderRepository.GetPagedAsync(
            query,
            cancellationToken);
        var responses = result.Items
            .Select(ToResponse)
            .ToList();
        return new PagedResponse<OrderResponse>
        {
            Items = responses,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount
        };
    }
}