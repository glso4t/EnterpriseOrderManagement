export type UserRole = "Admin" | "User";

export interface AuthUser {
    id: string;
    role: UserRole;
}

export interface AuthContextType {
    user: AuthUser | null;
    isAuthenticated: boolean;
    isInitializing: boolean;
    login: (token: string) => void;
    logout: () => Promise<void>;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    email: string;
    password: string;
}

export interface AuthApiResponse {
    accessToken: string;
    tokenType: string;
    expiresAtUtc: string;
    user: {
        id: string;
        email: string;
        role: UserRole;
    };
}