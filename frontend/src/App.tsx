import "./App.css";

import ProductCard from "./components/ProductCard";
import type { Product } from "./types";

const initialProducts: Product[] = [
    {
        id: 1,
        name: "Laptop",
        price: 899.99,
        stock: 5,
        category: "Electronics"
    },
    {
        id: 2,
        name: "Keyboard",
        price: 49.99,
        stock: 12,
        category: "Accessories"
    },
    {
        id: 3,
        name: "Mouse",
        price: 19.99,
        stock: 0,
        category: "Accessories"
    },
    {
        id: 4,
        name: "Monitor",
        price: 199.99,
        stock: 2,
        category: "Electronics"
    },
    {
    id: 5,
    name: "Webcam",
    price: 69.99,
    stock: 4,
    category: "Accessories"
    }
];

function App() {
    return (
        <div className="app-shell">
            <header className="topbar">
                <a className="brand" href="/" aria-label="Home">
                    <span className="brand-mark">logo</span>

                    <span className="brand-text">
                        <strong>Enterprise</strong>
                        <small>ORDER MANAGEMENT</small>
                    </span>
                </a>

                <div className="environment-label">
                    <span className="status-dot"></span>
                    React frontend demo
                </div>
            </header>

            <main className="page-content">
                <section className="page-intro">
                    <div>
                        <p className="eyebrow">
                            INVENTORY / OVERVIEW
                        </p>

                        <h1>Product Management</h1>

                        <p className="intro-description">
                            View the products in your catalog and
                            check their current stock levels.
                        </p>
                    </div>

                    <div className="summary-card">
                        <span>Catalog products</span>
                        <strong>{initialProducts.length}</strong>
                        <small>Sample data</small>
                    </div>
                </section>

                <section
                    className="catalog-section" aria-labelledby="products-heading">
                    <div className="section-heading">
                        <div>
                            <h2 id="products-heading">Products</h2>

                            <p>
                                Products currently displayed in the catalog.
                            </p>
                        </div>

                        <span className="count-pill">
                            {initialProducts.length} items
                        </span>
                    </div>

                    <div className="product-grid">
                        {initialProducts.map((product) => (
                            <ProductCard
                                key={product.id}
                                product={product}
                            />
                        ))}
                    </div>
                </section>
            </main>

            <footer className="page-footer">
                <span>EnterpriseOrderManagement</span>
                <span>Konstantinos Goulas</span>
            </footer>
        </div>
    );
}

export default App;