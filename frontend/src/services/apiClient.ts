import { getStoredToken } from "../auth/tokenStorage";

import {
    AUTH_SESSION_EXPIRED_EVENT
} from "../auth/authEvents";


const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

// checks if the API_BASE_URL is configured and removes any trailing slashes from the URL
function getBaseUrl(): string {
    if (!API_BASE_URL) {
        throw new Error(
            "VITE_API_BASE_URL is not configured."
        );
    }
    return API_BASE_URL.replace(/\/+$/, "");
}


function isAuthenticationEndpoint(path: string): boolean {
    return (
        path.startsWith("/api/v1/auth/login") ||
        path.startsWith("/api/v1/auth/register") ||
        path.startsWith("/api/v1/auth/refresh")
    );
}

// returns the headers for API requests, including the current user's stored JWT
function getApiHeaders(hasBody: boolean): HeadersInit {
    const headers: Record<string, string> = {
        Accept: "application/json"
    };
    const token = getStoredToken();
    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }
    if (hasBody) {
        headers["Content-Type"] = "application/json";
    }
    return headers;
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

// makes an API request to the specified endpoint with the provided options, handling errors and returning the response
export async function apiRequest(
    endpoint: string,
    options: RequestInit = {}
): Promise<Response> {
    const path = endpoint.startsWith("/")
        ? endpoint
        : `/${endpoint}`;
    const hasBody =
        options.body !== undefined &&
        options.body !== null;
    const headers = new Headers(
        getApiHeaders(hasBody)
    );
    // Merge additional headers provided by the caller.
    if (options.headers) {
        const additionalHeaders = new Headers(
            options.headers
        );
        additionalHeaders.forEach((value, key) => {
            headers.set(key, value);
        });
    }
    const response = await fetch(
        `${getBaseUrl()}${path}`,
        {
            ...options,
            headers
        }
    );
    if (!response.ok) {
        // Authentication failed on a protected endpoint.
        if (
            response.status === 401 &&
            !isAuthenticationEndpoint(path)
        ) {
            window.dispatchEvent(
                new Event(AUTH_SESSION_EXPIRED_EVENT)
            );
        }
        throw new Error(
            await getApiErrorMessage(response)
        );
    }
    return response;
}


