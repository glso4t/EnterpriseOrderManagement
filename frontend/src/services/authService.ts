import { apiRequest } from "./apiClient";

import type {
    AuthApiResponse,
    LoginRequest,
    RegisterRequest
} from "../auth/authTypes";

export async function loginUser(
    request: LoginRequest
): Promise<AuthApiResponse> {
    // Δίνουμε <AuthApiResponse> για να ξέρει το TS τι τύπο επιστρέφει
    return apiRequest<AuthApiResponse>(
        "/api/v1/auth/login",
        {
            method: "POST",
            body: JSON.stringify(request),
            credentials: "include"
        }
    );
}

export async function registerUser(
    request: RegisterRequest
): Promise<{ message: string }> {
    // Δίνουμε <{ message: string }> ως generic τύπο
    return apiRequest<{ message: string }>(
        "/api/v1/auth/register",
        {
            method: "POST",
            body: JSON.stringify(request)
        }
    );
}

export async function logoutUser(): Promise<void> {

    await apiRequest(
        "/api/v1/auth/logout",
        {
            method: "POST",
            credentials: "include",
            headers: {
                "X-Refresh-Request": "1"
            }
        }
    );
}
