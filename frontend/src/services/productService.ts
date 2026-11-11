import { apiRequest } from "./apiClient";
import type {
    Product,
    ProductApiResponse,
    CreateProductRequest,
    PatchProductRequest,
    UpdateProductRequest
} from "../types";


    // checks if a value is a ProductApiResponse
function isProductApiResponse(
    value: unknown
): value is ProductApiResponse {
    if (typeof value !== "object" || value === null) {
        return false;
    }
    const product = value as Record<string, unknown>;
    return (
        typeof product.id === "number" &&
        Number.isInteger(product.id) &&
        typeof product.name === "string" &&
        typeof product.price === "number" &&
        Number.isFinite(product.price) &&
        typeof product.stock === "number" &&
        Number.isInteger(product.stock) &&
        product.stock >= 0
    );
}

// maps a ProductApiResponse to a Product, adding a default category of "Uncategorized"
function mapProductResponse(value: unknown): Product {
    if (!isProductApiResponse(value)) {
        throw new Error(
            "The API returned a product with an invalid structure."
        );
    }
    return {
        ...value,
        category: "Uncategorized"
    };
}


//(GET) fetches the list of products from the API and returns them as an array of Product objects
export async function getProducts(): Promise<Product[]> {
    const data: unknown = await apiRequest<unknown>(
        "/api/v1/products"
    );
    if (!Array.isArray(data)) {
        throw new Error(
            "Unexpected API response. Expected an array."
        );
    }
    const apiProducts: unknown[] = data;
    return apiProducts.map(mapProductResponse);
}

//(POST) creates a new product
export async function createProduct(
    request: CreateProductRequest
): Promise<Product> {
    const data: unknown = await apiRequest<unknown>(
        "/api/v1/products",
        {
            method: "POST",
            body: JSON.stringify(request)
        }
    );
    return mapProductResponse(data);
}

//(PATCH) updates an existing product
export async function patchProduct(
    productId: number,
    request: PatchProductRequest
): Promise<Product> {
    if (!Number.isInteger(productId) || productId <= 0) {
        throw new Error("Invalid product ID.");
    }
    const data = await apiRequest<unknown>(
        `/api/v1/products/${productId}`,
        {
            method: "PATCH",
            body: JSON.stringify(request)
        }
    );
    const updatedProduct = mapProductResponse(data);
    // Ensure that the API returned the requested product
    if (updatedProduct.id !== productId) {
        throw new Error(
            "The API returned an unexpected product."
        );
    }
    return updatedProduct;
}

// (PUT) updates an existing product with a complete replacement of its data
export async function updateProduct(
    productId: number,
    request: UpdateProductRequest
): Promise<Product> {
    if (!Number.isInteger(productId) || productId <= 0) {
        throw new Error("Invalid product ID.");
    }
    const data = await apiRequest<unknown>(
        `/api/v1/products/${productId}`,
        {
            method: "PUT",
            body: JSON.stringify(request)
        }
    );
    const updatedProduct = mapProductResponse(data);
    // Ensure that the API returned the requested product
    if (updatedProduct.id !== productId) {
        throw new Error(
            "The API returned an unexpected product."
        );
    }
    return updatedProduct;
}


// (DELETE) deletes a product by its ID
export async function deleteProduct(
    productId: number
): Promise<void> {
    if (!Number.isInteger(productId) || productId <= 0) {
        throw new Error("Invalid product ID.");
    }
    await apiRequest<void>(
        `/api/v1/products/${productId}`,
        {
            method: "DELETE"
        }
    );
    // The API returns 204 No Content.
    // There is no JSON response body to read.
}
