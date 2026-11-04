//Product interface
export interface Product {
    id: number;
    name: string;
    price: number;
    stock: number;
    category: string;
}

export interface ProductApiResponse {
    id: number;
    name: string;
    price: number;
    stock: number;
}

export interface CreateProductRequest {
    name: string;
    price: number;
    stock: number;
}

export interface PatchProductRequest {
    name?: string;
    price?: number;
    stock?: number;
}

export interface UpdateProductRequest {
    name: string;
    price: number;
    stock: number;
}

//Customer interface
export interface Customer {
    id: number;
    name: string;
    email: string;
}

export interface CreateCustomerRequest {
    name: string;
    email: string;
}

export interface UpdateCustomerRequest {
    name: string;
    email: string;
}

//Order interface

export type OrderStatus =
    | "Pending"
    | "Confirmed"
    | "Cancelled";

    export interface OrderItem {
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
}

export interface Order {
    id: number;
    customerId: number;
    orderDate: string;
    status: OrderStatus;
    items: OrderItem[];
    totalAmount: number;
}

//Request interfaces for creating an order
export interface CreateOrderItemRequest {
    productId: number;
    quantity: number;
}
export interface CreateOrderRequest {
    customerId: number;
    items: CreateOrderItemRequest[];
}

export interface PagedResponse<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
}

//τύπος που περιγράφει τα φίλτρα του GET Orders endpoint
export interface OrderQueryParams {
    page?: number;
    pageSize?: number;
    customerId?: number;
    status?: OrderStatus;
    fromDate?: string;
    toDate?: string;
    sortBy?: string;
    sortDirection?: "asc" | "desc";
}