import { apiRequest } from "./apiClient";

import type {
    Order,
    OrderItem,
    OrderQueryParams,
    PagedResponse,
    OrderStatus,
    CreateOrderRequest
} from "../types";


// Checks whether a value is a non-null object
function isRecord(
    value: unknown
): value is Record<string, unknown> {
    return (
        typeof value === "object" &&
        value !== null &&
        !Array.isArray(value)
    );
}

// Checks whether a value is a valid OrderStatus
function isOrderStatus(
    value: unknown
): value is OrderStatus {
    return (
        value === "Pending" ||
        value === "Confirmed" ||
        value === "Cancelled"
    );
}

// Validates the structure of an OrderItem
function isOrderItem(
    value: unknown
): value is OrderItem {
    if (!isRecord(value)) {
        return false;
    }
    return (
        Number.isInteger(value.productId) &&
        typeof value.productName === "string" &&
        Number.isInteger(value.quantity) &&
        Number.isFinite(value.unitPrice) &&
        Number.isFinite(value.lineTotal)
    );
}

// Validates the structure of an Order
function isOrder(
    value: unknown
): value is Order {
    if (!isRecord(value)) {
        return false;
    }
    return (
        Number.isInteger(value.id) &&
        Number.isInteger(value.customerId) &&
        typeof value.orderDate === "string" &&
        !Number.isNaN(Date.parse(value.orderDate)) &&
        isOrderStatus(value.status) &&
        Array.isArray(value.items) &&
        value.items.every(isOrderItem) &&
        Number.isFinite(value.totalAmount)
    );
}

// Validates a paginated API response
function isPagedResponse<T>(
    value: unknown,
    isItem: (item: unknown) => item is T    //Όταν γράφουμε: isPagedResponse(data, isOrder) λέμε «Έλεγξε αν έχω ένα έγκυρο PagedResponse, του οποίου κάθε item είναι Order».
): value is PagedResponse<T> {
    if (!isRecord(value)) {
        return false;
    }
    return (
        Array.isArray(value.items) &&
        value.items.every(isItem) &&
        Number.isInteger(value.page) &&
        typeof value.page === "number" &&
        value.page >= 1 &&
        Number.isInteger(value.pageSize) &&
        typeof value.pageSize === "number" &&
        value.pageSize >= 1 &&
        Number.isInteger(value.totalCount) &&
        typeof value.totalCount === "number" &&
        value.totalCount >= 0 &&
        Number.isInteger(value.totalPages) &&
        typeof value.totalPages === "number" &&
        value.totalPages >= 0
    );
}

// Validates the pagination and filter parameters
function validateOrderQueryParams(
    filters: OrderQueryParams
): void {
    const page = filters.page ?? 1;
    const pageSize = filters.pageSize ?? 10;
    if (!Number.isInteger(page) || page < 1) {
        throw new Error("Page must be a positive integer.");
    }
    if (
        !Number.isInteger(pageSize) ||
        pageSize < 1 ||
        pageSize > 100
    ) {
        throw new Error(
            "Page size must be between 1 and 100."
        );
    }
    if (
        filters.customerId !== undefined &&
        (
            !Number.isInteger(filters.customerId) ||
            filters.customerId <= 0
        )
    ) {
        throw new Error("Invalid customer ID.");
    }
    if (
        filters.fromDate !== undefined &&
        Number.isNaN(Date.parse(filters.fromDate))
    ) {
        throw new Error("Invalid fromDate value.");
    }
    if (
        filters.toDate !== undefined &&
        Number.isNaN(Date.parse(filters.toDate))
    ) {
        throw new Error("Invalid toDate value.");
    }
    if (
        filters.fromDate !== undefined &&
        filters.toDate !== undefined &&
        Date.parse(filters.fromDate) > Date.parse(filters.toDate)
    ) {
        throw new Error(
            "fromDate cannot be later than toDate."
        );
    }
}

//CRUD Operations for Orders

//(GET) fetches paginated orders with optional filters
export async function getOrders(
    filters: OrderQueryParams = {}
): Promise<PagedResponse<Order>> {
    validateOrderQueryParams(filters);
    const params = new URLSearchParams();
    // Pagination defaults
    params.set("page", String(filters.page ?? 1));
    params.set("pageSize", String(filters.pageSize ?? 10));
    // Optional filters
    if (filters.customerId !== undefined) {
        params.set(
            "customerId",
            String(filters.customerId)
        );
    }
    if (filters.status !== undefined) {
        params.set("status", filters.status);
    }
    if (filters.fromDate !== undefined) {
        params.set("fromDate", filters.fromDate);
    }
    if (filters.toDate !== undefined) {
        params.set("toDate", filters.toDate);
    }
    if (filters.sortBy !== undefined) {
        params.set("sortBy", filters.sortBy);
    }
    if (filters.sortDirection !== undefined) {
        params.set("sortDirection", filters.sortDirection);
    }
    const endpoint =    //Build the endpoint with query parameters
        `/api/v1/orders?${params.toString()}`;
    const data = await apiRequest<unknown>(endpoint);    //Make the API request to fetch orders
    if (!isPagedResponse(data, isOrder)) {  //Validate the structure of the paginated response
        throw new Error(
            "The API returned orders with an invalid structure."
        );
    }
    return data;
}

// (GET by ID) fetches a specific order by its ID and returns it as an Order object
export async function getOrderById(
    orderId: number
): Promise<Order> {
    // Validate the order ID
    if (!Number.isInteger(orderId) || orderId <= 0) {
        throw new Error("Invalid order ID.");
    }
    const data = await apiRequest<unknown>(
        `/api/v1/orders/${orderId}`
    );
    if (!isOrder(data)) {
        throw new Error(
            "The API returned an order with an invalid structure."
        );
    }
    if (data.id !== orderId) {
        throw new Error(
            "The API returned an unexpected order."
        );
    }
    return data;
}

// (POST) creates a new order
export async function createOrder(
    request: CreateOrderRequest
): Promise<Order> {
    if (        //Validates customer ID
        !Number.isInteger(request.customerId) ||
        request.customerId <= 0
    ) {
        throw new Error("Invalid customer ID.");
    }
    if (!Array.isArray(request.items) || request.items.length === 0) {      // Validates that the order contains at least one item
        throw new Error(
            "An order must contain at least one product."
        );
    }
    const productIds = new Set<number>();
    for (const item of request.items) {
        if (
            !Number.isInteger(item.productId) ||
            item.productId <= 0
        ) {
            throw new Error("Invalid product ID.");
        }
        if (
            !Number.isInteger(item.quantity) ||
            item.quantity <= 0
        ) {
            throw new Error(
                "Product quantities must be positive integers."
            );
        }
        if (productIds.has(item.productId)) {               // Prevent duplicate product IDs
            throw new Error(
                "An order cannot contain duplicate products."
            );
        }
        productIds.add(item.productId);
    }
    // Send POST request
    const data = await apiRequest<unknown>(
        "/api/v1/orders",
        {
            method: "POST",
            body: JSON.stringify(request)
        }
    );
    if (!isOrder(data)) {       // Validate the returned Order
        throw new Error(
            "The API returned an order with an invalid structure."
        );
    }
    if (data.customerId !== request.customerId) {   // Verify the returned customer ID
        throw new Error(
            "The API returned an unexpected customer."
        );
    }
    return data;
}

    // (POST : CONFIRM) confirms an existing order
    export async function confirmOrder(
        orderId: number
    ): Promise<void> {
        // Validate order ID
        if (!Number.isInteger(orderId) || orderId <= 0) {
            throw new Error("Invalid order ID.");
        }
        await apiRequest<void>(
            `/api/v1/orders/${orderId}/confirm`,
            {
                method: "POST"
            }
        );
    }

    // (POST : CANCEL) cancels an existing order
    export async function cancelOrder(
        orderId: number
    ): Promise<void> {
        // Validate order ID
        if (!Number.isInteger(orderId) || orderId <= 0) {
            throw new Error("Invalid order ID.");
        }
        await apiRequest<void>(
            `/api/v1/orders/${orderId}/cancel`,
            {
                method: "POST"
            }
        );
    }