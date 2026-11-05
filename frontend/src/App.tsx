import { useEffect, useState } from "react";
import "./App.css";

import CustomersPage from "./pages/CustomersPage";
import ProductsPage from "./pages/ProductsPage";
import OrdersPage from "./pages/OrdersPage";

type ActiveScreen = "products" | "customers" | "orders";

function App() {


    // States

    // State to track the currently active screen (products or customers)
    const [activeScreen, setActiveScreen] =
        useState<ActiveScreen>("products");

    const [isOnline, setIsOnline] = 
        useState(true);

    // State to track the list of products
    const [hasVisitedCustomers, setHasVisitedCustomers] =
        useState(false);

    //state to track the list of orders
    const [hasVisitedOrders, setHasVisitedOrders] =
        useState(false);



    //Effects

    //Effect: update the online status whenever the browser goes online or offline
    useEffect(() => {
        function updateOnlineStatus(): void {
            setIsOnline(navigator.onLine);
        }
        updateOnlineStatus();
        window.addEventListener(    //Όταν επανέλθει η σύνδεση, εκτελείται το updateOnlineStatus
            "online",
            updateOnlineStatus
        );
        window.addEventListener(    //Όταν χαθεί η σύνδεση, εκτελείται το updateOnlineStatus
            "offline",
            updateOnlineStatus
        );
        return () => {  //Cleanup
            window.removeEventListener(
                "online",
                updateOnlineStatus
            );
            window.removeEventListener(
                "offline",
                updateOnlineStatus
            );
        };
    }, []);


    // Handlers

    // Navigation handler: opens the Products screen
    function handleProductsNavigation(): void {
        setActiveScreen("products");
    }

    // Navigation handler: opens the Customers screen and marks that the user has visited the Customers page
        function handleCustomersNavigation(): void {
        setHasVisitedCustomers(true);
        setActiveScreen("customers");
    }

    // Navigation handler: opens the Orders screen
    function handleOrdersNavigation(): void {
        setHasVisitedOrders(true);
        setActiveScreen("orders");
    }



    return (
        <div className="app-shell">
            <header className="topbar">
                <a
                    className="brand"
                    href="/"
                    aria-label="Home"
                >
                    <span className="brand-mark">
                        logo
                    </span>

                    <span className="brand-text">
                        <strong>Enterprise</strong>
                        <small>ORDER MANAGEMENT</small>
                    </span>
                </a>

                <nav
                    className="main-navigation"
                    aria-label="Main navigation"
                >
                    <button
                        type="button"
                        className={
                            activeScreen === "products"
                                ? "screen-nav-button active"
                                : "screen-nav-button"
                        }
                        aria-current={
                            activeScreen === "products"
                                ? "page"
                                : undefined
                        }
                        onClick={handleProductsNavigation}
                    >
                        Products
                    </button>

                    <button
                        type="button"
                        className={
                            activeScreen === "customers"
                                ? "screen-nav-button active"
                                : "screen-nav-button"
                        }
                        aria-current={
                            activeScreen === "customers"
                                ? "page"
                                : undefined
                        }
                        onClick={handleCustomersNavigation}
                    >
                        Customers
                    </button>

                    <button
                        type="button"
                        className={
                            activeScreen === "orders"
                                ? "screen-nav-button active"
                                : "screen-nav-button"
                        }
                        aria-current={
                            activeScreen === "orders"
                                ? "page"
                                : undefined
                        }
                        onClick={handleOrdersNavigation}
                    >
                        Orders
                </button>
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
            </header>

            <main className="page-content">
                <div hidden={activeScreen !== "products"}>
                    <ProductsPage 
                        isActive={activeScreen === "products"}
                    />
                </div>

                {hasVisitedCustomers && (
                    <div hidden={activeScreen !== "customers"}>
                        <CustomersPage />
                    </div>
                )}

                {hasVisitedOrders && (
                    <div hidden={activeScreen !== "orders"}>
                        <OrdersPage
                            isActive={activeScreen === "orders"}
                        />
                    </div>
                )}
            </main>

            <footer className="page-footer">
                <span>EnterpriseOrderManagement</span>
                <span>Konstantinos Goulas</span>
            </footer>
        </div>
    );
}

export default App;