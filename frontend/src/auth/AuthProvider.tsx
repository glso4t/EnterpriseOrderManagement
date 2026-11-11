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
    getTokenExpirationTime,
    removeToken,
    saveToken
} from "./tokenStorage";

import { AuthContext } from "./authContext";

import {
    AUTH_SESSION_EXPIRED_EVENT
} from "./authEvents";


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


    // Listen for session expiration events from the API layer.
    useEffect(() => {
        function handleSessionExpired(): void {
            removeToken();
            setUser(null);
        }
        window.addEventListener(
            AUTH_SESSION_EXPIRED_EVENT,
            handleSessionExpired
        );
        return () => {
            window.removeEventListener(
                AUTH_SESSION_EXPIRED_EVENT,
                handleSessionExpired
            );
        };
    }, []);


    // Automatically expire the frontend session
    // when the access token reaches its expiration time.
    useEffect(() => {
        if (!user) {
            return;
        }
        const token = getStoredToken();
        if (!token) {
            return;
        }
        const expiresAt = getTokenExpirationTime(token);
        if (expiresAt === null) {
            return;
        }
        const remainingTime = Math.max(
            0,
            expiresAt - Date.now()
        );
        const timeoutId = window.setTimeout(
            () => {
                window.dispatchEvent(
                    new Event(AUTH_SESSION_EXPIRED_EVENT)
                );
            },
            remainingTime
        );
        return () => {
            window.clearTimeout(timeoutId);
        };

    }, [user]);



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
