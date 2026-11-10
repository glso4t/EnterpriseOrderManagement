import {
    useEffect,
    useState
} from "react";
import type { ReactNode } from "react";

import type {
    AuthContextType,
    AuthUser
} from "./authTypes";

import {
    getStoredToken,
    getUserFromToken,
    removeToken,
    saveToken
} from "./tokenStorage";

import { AuthContext } from "./authContext";


interface AuthProviderProps {
    children: ReactNode;
}

export function AuthProvider({
    children
}: AuthProviderProps) {

    const [user, setUser] = useState<AuthUser | null>(() => {
        const token = getStoredToken();
        if (!token) {
            return null;
        }
        return getUserFromToken(token);
    });

    // Remove an expired or invalid stored token.
    useEffect(() => {
        const token = getStoredToken();
        if (token && !getUserFromToken(token)) {
            removeToken();
        }
    }, []);

    function login(token: string): void {
        const authenticatedUser = getUserFromToken(token);
        if (!authenticatedUser) {
            throw new Error("Invalid or expired token.");
        }
        saveToken(token);
        setUser(authenticatedUser);
    }

    function logout(): void {
        removeToken();
        setUser(null);
    }

    const value: AuthContextType = {
        user,
        isAuthenticated: user !== null,
        login,
        logout
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}
