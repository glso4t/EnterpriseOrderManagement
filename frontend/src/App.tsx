import { Link, Navigate, Route, Routes } from "react-router";

import AppLayout from "./components/AppLayout";

import ProtectedRoute from "./auth/ProtectedRoute";

import LoginPage from "./pages/LoginPage";
import CustomersPage from "./pages/CustomersPage";
import ProductsPage from "./pages/ProductsPage";
import OrdersPage from "./pages/OrdersPage";

import "./App.css";

function NotFoundPage() {
    return (
        <section>
            <h2>404 - Page Not Found</h2>

            <p>
                The page you are looking for does not exist.
            </p>

            <Link to="/products">
                Return to Products
            </Link>
        </section>
    );
}

function App() {
    return (
        <Routes>

            <Route
                path="/login"
                element={<LoginPage />}
            />

            <Route
                element={
                    <ProtectedRoute>
                        <AppLayout />
                    </ProtectedRoute>
                }
            >

                <Route
                    index
                    element={<Navigate to="/products" replace />}
                />

                <Route
                    path="products"
                    element={<ProductsPage isActive={true} />}
                />

                <Route
                    path="customers"
                    element={<CustomersPage />}
                />

                <Route
                    path="orders"
                    element={<OrdersPage isActive={true} />}
                />

                <Route
                    path="*"
                    element={<NotFoundPage />}
                />

            </Route>

        </Routes>
    );
}

export default App;