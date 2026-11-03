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