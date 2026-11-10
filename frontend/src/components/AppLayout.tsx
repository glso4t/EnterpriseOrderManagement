import { useEffect, useState } from "react";
import { Link, NavLink, Outlet, useNavigate } from "react-router";
import { useAuth } from "../auth/useAuth";

export default function AppLayout() {

    const [isOnline, setIsOnline] = 
        useState(() => navigator.onLine);

    const { user, logout } = useAuth();
    const navigate = useNavigate();


    function handleLogout(): void {
        logout();
        navigate("/login", { replace: true });
    }


    useEffect(() => {
        const handleOnline = () => {
            setIsOnline(true);
        };
        const handleOffline = () => {
            setIsOnline(false);
        };
        window.addEventListener("online", handleOnline);
        window.addEventListener("offline", handleOffline);
        return () => {
            window.removeEventListener("online", handleOnline);
            window.removeEventListener("offline", handleOffline);
        };
    }, []);

    return (
        <div className="app-shell">

            <header className="topbar">

                <Link
                    className="brand"
                    to="/"
                    aria-label="Home"
                >
                    <span className="brand-mark">
                        logo
                    </span>

                    <span className="brand-text">
                        <strong>Enterprise</strong>
                        <small>ORDER MANAGEMENT</small>
                    </span>
                </Link>

                <nav
                    className="main-navigation"
                    aria-label="Main navigation"
                >
                    <NavLink
                        to="/products"
                        className={({ isActive }) =>
                            isActive
                                ? "screen-nav-button active"
                                : "screen-nav-button"
                        }
                    >
                        Products
                    </NavLink>

                    <NavLink
                        to="/customers"
                        className={({ isActive }) =>
                            isActive
                                ? "screen-nav-button active"
                                : "screen-nav-button"
                        }
                    >
                        Customers
                    </NavLink>

                    <NavLink
                        to="/orders"
                        className={({ isActive }) =>
                            isActive
                                ? "screen-nav-button active"
                                : "screen-nav-button"
                        }
                    >
                        Orders
                    </NavLink>
                </nav>

                <div
                    className="connection-status"
                    aria-live="polite"
                >
                    <span
                        className={`connection-dot ${
                            isOnline ? "online" : "offline"
                        }`}
                    />

                    <span>
                        {isOnline ? "Online" : "Offline"}
                    </span>
                </div>

                <div className="environment-label">
                    <span className="status-dot"></span>
                    React frontend demo 2026 v1.0.0
                </div>

                <div className="user-session">
                    <span>
                        {user?.role}
                    </span>

                    <button
                        type="button"
                        className="screen-nav-button"
                        onClick={handleLogout}
                    >
                        Logout
                    </button>
                </div>

            </header>

            <main className="page-content">
                <Outlet />
            </main>

            <footer className="page-footer">
                <span>EnterpriseOrderManagement</span>
                <span>Konstantinos Goulas</span>
            </footer>

        </div>
    );
}