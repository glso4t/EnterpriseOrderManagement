import { Link, Navigate, Route, Routes } from "react-router";
import AppLayout from "./components/AppLayout";
import "./App.css";

import CustomersPage from "./pages/CustomersPage";
import ProductsPage from "./pages/ProductsPage";
import OrdersPage from "./pages/OrdersPage";

function NotFoundPage() {
    return (
        <section className="not-found-page">
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
            <Route element={<AppLayout />}>
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