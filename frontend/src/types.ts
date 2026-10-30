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