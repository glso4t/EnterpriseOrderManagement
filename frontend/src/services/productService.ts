import type {
    Product,
    ProductApiResponse,
    CreateProductRequest
} from "../types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
const API_TOKEN = import.meta.env.VITE_API_TOKEN;

// checks if the API_BASE_URL is configured and removes any trailing slashes from the URL
function getBaseUrl(): string {
    if (!API_BASE_URL) {
        throw new Error(
            "VITE_API_BASE_URL is not configured."
        );
    }
    return API_BASE_URL.replace(/\/+$/, "");
}

// checks if the API_TOKEN is configured and returns the headers for API requests
function getApiHeaders(
    includeContentType: boolean = false
): Record<string, string> {
    if (!API_TOKEN) {
        throw new Error(
            "VITE_API_TOKEN is not configured."
        );
    }
    const headers: Record<string, string> = {
        Accept: "application/json",
        Authorization: `Bearer ${API_TOKEN}`
    };
    if (includeContentType) {
        headers["Content-Type"] = "application/json";
    }
    return headers;
}

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

// extracts a meaningful error message from an API response, handling common error structures
async function getApiErrorMessage(
    response: Response
): Promise<string> {
    if (response.status === 401) {
        return "401 Unauthorized: the token is missing, invalid or expired.";
    }
    if (response.status === 403) {
        return "403 Forbidden: Admin role is required for this operation.";
    }
    try {   // Attempt to parse the response body as JSON
        const body: unknown = await response.json();
        if (typeof body === "object" && body !== null) {    
            const errorBody = body as Record<string, unknown>;
            if (
                typeof errorBody.errors === "object" &&
                errorBody.errors !== null
            ) {
                const validationMessages = Object.values(
                    errorBody.errors as Record<string, unknown>
                ).flatMap((value) =>
                    Array.isArray(value)
                        ? value.filter(
                            (message): message is string =>
                                typeof message === "string"
                        )
                        : []
                );
                if (validationMessages.length > 0) {
                    return validationMessages.join(" ");
                }
            }
            if (typeof errorBody.detail === "string") {
                return errorBody.detail;
            }
            if (typeof errorBody.title === "string") {
                return errorBody.title;
            }
        }
    } catch {
        // The response did not contain readable JSON.
    }
    return `Request failed. HTTP ${response.status}`;
}

//(GET) fetches the list of products from the API and returns them as an array of Product objects
export async function getProducts(): Promise<Product[]> {
    const response = await fetch(
        `${getBaseUrl()}/api/v1/products`,
        {
            method: "GET",
            headers: getApiHeaders()
        }
    );
    if (!response.ok) {
        throw new Error(
            await getApiErrorMessage(response)
        );
    }
    const data: unknown = await response.json();
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
    const response = await fetch(
        `${getBaseUrl()}/api/v1/products`,
        {
            method: "POST",
            headers: getApiHeaders(true),
            body: JSON.stringify(request)
        }
    );
    if (!response.ok) {
        throw new Error(
            await getApiErrorMessage(response)
        );
    }
    const data: unknown = await response.json();
    return mapProductResponse(data);
}