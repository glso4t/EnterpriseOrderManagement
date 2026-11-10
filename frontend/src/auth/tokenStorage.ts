
import type { AuthUser, UserRole } from "./authTypes";

interface JwtPayload {
    sub?: string;
    exp?: number;
    role?: string;
    roles?: string[];

    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string;
}

const TOKEN_STORAGE_KEY =
    "enterprise_order_management_access_token";


export function getStoredToken(): string | null {
    return sessionStorage.getItem(TOKEN_STORAGE_KEY);
}


export function saveToken(token: string): void {
    sessionStorage.setItem(TOKEN_STORAGE_KEY, token);
}


export function removeToken(): void {
    sessionStorage.removeItem(TOKEN_STORAGE_KEY);
}


// Decodes the JWT and validates the claims needed by the UI.
// This does NOT validate the JWT signature.
export function getUserFromToken(token: string): AuthUser | null {
    try {
        const parts = token.split(".");

        if (parts.length !== 3) {
            return null;
        }

        const payload = parts[1];

        const base64 = payload
            .replace(/-/g, "+")
            .replace(/_/g, "/");

        const paddedBase64 = base64.padEnd(
            base64.length + (4 - (base64.length % 4)) % 4,
            "="
        );

        const decodedPayload = atob(paddedBase64);

        const jwtPayload = JSON.parse(
            decodedPayload
        ) as JwtPayload;


        // Validate token expiration.
        if (
            typeof jwtPayload.exp !== "number" ||
            jwtPayload.exp * 1000 <= Date.now()
        ) {
            return null;
        }


        // Extract the role claim.
        const roleClaim =
            jwtPayload.role ??
            jwtPayload.roles?.[0] ??
            jwtPayload[
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            ];

        if (
            roleClaim !== "Admin" &&
            roleClaim !== "User"
        ) {
            return null;
        }


        // Extract the user ID.
        const userId = jwtPayload.sub;

        if (!userId) {
            return null;
        }

        const role: UserRole = roleClaim;

        return {
            id: userId,
            role
        };

    } catch {
        return null;
    }
}
