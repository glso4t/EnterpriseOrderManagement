import type { ReactNode } from "react";
import { Navigate } from "react-router";

import { useAuth } from "./useAuth";
import type { UserRole } from "./authTypes";

interface ProtectedRouteProps {
    children: ReactNode;
    allowedRoles?: UserRole[];
}

export default function ProtectedRoute({    //θέλουμε να εμποδίσουμε έναν μη authenticated χρήστη να ανοίξει τις σελίδες της εφαρμογής
    children,
    allowedRoles
}: ProtectedRouteProps) {

    const { user, isAuthenticated } = useAuth();

    // User is not authenticated.
    if (!isAuthenticated) {
        return <Navigate to="/login" replace />;
    }

    // User is authenticated but does not have the required role.
    if (
        allowedRoles &&
        user &&
        !allowedRoles.includes(user.role)
    ) {
        return <Navigate to="/products" replace />;
    }

    return children;
}