export type UserRole = "Admin" | "User";

export interface AuthUser {
    id: string;
    role: UserRole;
}

export interface AuthContextType {
    user: AuthUser | null;
    isAuthenticated: boolean;
    login: (token: string) => void;
    logout: () => void;
}