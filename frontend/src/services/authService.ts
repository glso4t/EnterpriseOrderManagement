import { apiRequest } from "./apiClient";

import type {
    AuthApiResponse,
    LoginRequest,
    RegisterRequest
} from "../auth/authTypes";

export async function loginUser(
    request: LoginRequest
): Promise<AuthApiResponse> {
    const response = await apiRequest(
        "/api/v1/auth/login",
        {
            method: "POST",
            body: JSON.stringify(request)
        }
    );
    return response.json();
}

export async function registerUser(
    request: RegisterRequest
): Promise<{ message: string }> {
    const response = await apiRequest(
        "/api/v1/auth/register",
        {
            method: "POST",
            body: JSON.stringify(request)
        }
    );
    return response.json();
}