import { getStoredToken, 
    saveToken 
} from "../auth/tokenStorage";

import {
    AUTH_SESSION_EXPIRED_EVENT,
    AUTH_TOKEN_REFRESHED_EVENT
} from "../auth/authEvents";


let refreshPromise: Promise<string | null> | null = null;
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

// makes an API request to the specified endpoint with the provided options, handling errors and returning the response
export async function apiRequest<T>(
    endpoint: string,
    options: RequestInit = {}
): Promise<T> {
    const normalizedEndpoint = endpoint.startsWith("/")
        ? endpoint
        : `/${endpoint}`;
    let response = await sendApiRequest(
        normalizedEndpoint,
        options
    );
    // Try to refresh only for protected API requests.
    if (
        response.status === 401 &&
        !isAuthenticationEndpoint(normalizedEndpoint)
    ) {
        const newAccessToken = await refreshAccessToken();
        if (!newAccessToken) {
            window.dispatchEvent(
                new Event(AUTH_SESSION_EXPIRED_EVENT)
            );
        } else {
            // Retry the original request exactly once.
            response = await sendApiRequest(
                normalizedEndpoint,
                options
            );
            // If it still returns 401, end the session.
            if (response.status === 401) {
                window.dispatchEvent(
                    new Event(AUTH_SESSION_EXPIRED_EVENT)
                );
            }
        }
    }
    if (!response.ok) {
        throw new Error(
            await getApiErrorMessage(response)
        );
    }
    if (response.status === 204) {
        return undefined as T;
    }
    const responseText = await response.text();
    if (!responseText.trim()) {
        return undefined as T;
    }
    return JSON.parse(responseText) as T;
}

//support functions

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

function isAuthenticationEndpoint(endpoint: string): boolean {
    return (
        endpoint.startsWith("/api/v1/auth/login") ||
        endpoint.startsWith("/api/v1/auth/register") ||
        endpoint.startsWith("/api/v1/auth/refresh") ||
        endpoint.startsWith("/api/v1/auth/logout")
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

async function sendApiRequest(
    endpoint: string,
    options: RequestInit
): Promise<Response> {
    const hasBody =
        options.body !== undefined &&
        options.body !== null;
    const headers = new Headers(
        getApiHeaders(hasBody)
    );
    if (options.headers) {
        const additionalHeaders = new Headers(options.headers);
        additionalHeaders.forEach((value, key) => {
            headers.set(key, value);
        });
    }
    return fetch(
        `${getBaseUrl()}${endpoint}`,
        {
            ...options,
            headers
        }
    );
}

export async function refreshAccessToken(): Promise<string | null> {
    if (!refreshPromise) {
        refreshPromise = (async () => {
            const response = await fetch(
                `${getBaseUrl()}/api/v1/auth/refresh`,
                {
                    method: "POST",
                    credentials: "include",
                    headers: {
                        Accept: "application/json",
                        "X-Refresh-Request": "1"
                    }
                }
            );
            // The refresh cookie is invalid or expired.
            if (
                response.status === 401 ||
                response.status === 403
            ) {
                return null;
            }
            if (!response.ok) {
                throw new Error(
                    "Could not refresh the authentication session."
                );
            }
            const data = await response.json() as {
                accessToken?: unknown;
            };
            if (typeof data.accessToken !== "string") {
                throw new Error(
                    "Invalid refresh response."
                );
            }
            // Store the new access token.
            saveToken(data.accessToken);
            // Notify React that the access token changed.
            window.dispatchEvent(
                new Event(AUTH_TOKEN_REFRESHED_EVENT)
            );
            return data.accessToken;
        })().finally(() => {
            refreshPromise = null;
        });
    }
    return refreshPromise;
}
