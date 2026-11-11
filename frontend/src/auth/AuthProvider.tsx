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

import { refreshAccessToken } from "../services/apiClient";
import { logoutUser } from "../services/authService";

import {
    AUTH_SESSION_EXPIRED_EVENT,
    AUTH_TOKEN_REFRESHED_EVENT
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

    const [isInitializing, setIsInitializing] = useState(true);

    const [tokenVersion, setTokenVersion] = useState(0);


    useEffect(() => {
        let cancelled = false;
        async function initializeSession(): Promise<void> {
            const existingToken = getStoredToken();
            if (existingToken) {
                const existingUser =
                    getUserFromToken(existingToken);
                if (existingUser) {
                    if (!cancelled) {
                        setUser(existingUser);
                    }
                    if (!cancelled) {
                        setIsInitializing(false);
                    }
                    return;
                }
            }
            removeToken();
            try {
                const newToken = await refreshAccessToken();
                if (!cancelled && newToken) {
                    const refreshedUser =
                        getUserFromToken(newToken);
                    setUser(refreshedUser);
                }
            } catch {
                // A temporary network error does not prove
                // that the refresh session is invalid.
            } finally {
                if (!cancelled) {
                    setIsInitializing(false);
                }
            }
        }
        void initializeSession();
        return () => {
            cancelled = true;
        };
    }, []);

    // Listen for session expiration events from the API layer.
    useEffect(() => {
        function handleSessionExpired(): void {
            removeToken();
            setUser(null);
            setTokenVersion(version => version + 1);
        }
        function handleTokenRefreshed(): void {
            const token = getStoredToken();
            const refreshedUser = token
                ? getUserFromToken(token)
                : null;
            setUser(refreshedUser);
            setTokenVersion(version => version + 1);
        }
        window.addEventListener(
            AUTH_SESSION_EXPIRED_EVENT,
            handleSessionExpired
        );
        window.addEventListener(
            AUTH_TOKEN_REFRESHED_EVENT,
            handleTokenRefreshed
        );
        return () => {
            window.removeEventListener(
                AUTH_SESSION_EXPIRED_EVENT,
                handleSessionExpired
            );
            window.removeEventListener(
                AUTH_TOKEN_REFRESHED_EVENT,
                handleTokenRefreshed
            );
        };
    }, []);


    // Remove an expired or invalid stored token.
    useEffect(() => {
        const token = getStoredToken();
        if (token && !getUserFromToken(token)) {
            removeToken();
        }
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
        // Try refreshing 60 seconds before expiration.
        const refreshIn = Math.max(
            0,
            expiresAt - Date.now() - 60_000
        );
        const timeoutId = window.setTimeout(
            async () => {
                try {
                    const newToken = await refreshAccessToken();
                    if (!newToken) {
                        window.dispatchEvent(
                            new Event(AUTH_SESSION_EXPIRED_EVENT)
                        );
                    }
                } catch {
                    // Leave the session state unchanged
                    // during a temporary network/server error.
                    // The API client can retry on the next request.
                }
            },
            refreshIn
        );
        return () => {
            window.clearTimeout(timeoutId);
        };
    }, [user, tokenVersion]);



    function login(token: string): void {
        const authenticatedUser = getUserFromToken(token);
        if (!authenticatedUser) {
            throw new Error("Invalid or expired token.");
        }
        saveToken(token);
        setUser(authenticatedUser);
    }

    async function logout(): Promise<void> {
        try {
            await logoutUser();
        } catch {
            // Local logout must still happen.
        } finally {
            removeToken();
            setUser(null);
        }
    }

    const value: AuthContextType = {
        user,
        isAuthenticated: user !== null,
        isInitializing,
        login,
        logout
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}
