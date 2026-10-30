import type {
    Product,
    ProductApiResponse
} from "../types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
const API_TOKEN = import.meta.env.VITE_API_TOKEN;

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

export async function getProducts(): Promise<Product[]> {
    if (!API_BASE_URL) {
        throw new Error(
            "VITE_API_BASE_URL is not configured."
        );
    }

    if (!API_TOKEN) {
        throw new Error(
            "VITE_API_TOKEN is not configured."
        );
    }

    const baseUrl = API_BASE_URL.replace(/\/+$/, "");

    const response = await fetch(
        `${baseUrl}/api/v1/products`,
        {
            method: "GET",
            headers: {
                Accept: "application/json",
                Authorization: `Bearer ${API_TOKEN}`
            }
        }
    );

    if (response.status === 401) {
        throw new Error(
            "401 Unauthorized: the development token is missing, invalid or expired."
        );
    }

    if (response.status === 403) {
        throw new Error(
            "403 Forbidden: the authenticated user does not have permission."
        );
    }

    if (!response.ok) {
        throw new Error(
            `Failed to load products. HTTP ${response.status}`
        );
    }

    const data: unknown = await response.json();

    if (!Array.isArray(data)) {
        throw new Error(
            "Unexpected API response. Expected an array."
        );
    }

    const apiProducts: unknown[] = data;

    return apiProducts.map((item) => {
        if (!isProductApiResponse(item)) {
            throw new Error(
                "The API returned a product with an invalid structure."
            );
        }

        return {
            ...item,
            category: "Uncategorized"
        };
    });
}