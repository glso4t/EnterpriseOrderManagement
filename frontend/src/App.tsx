import { useState } from "react";
import "./App.css";

import ProductCard from "./components/ProductCard";
import ProductForm from "./components/ProductForm";
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

    // State
    const [products, setProducts] =
        useState<Product[]>(initialProducts);

    const [searchTerm, setSearchTerm] = useState("");

    const [showInStockOnly, setShowInStockOnly] =
        useState(false);

    // Event handler: increase stock for a product
    function handleRestock(productId: number): void {
        setProducts((currentProducts) =>
            currentProducts.map((product) =>
                product.id === productId
                    ? {
                        ...product,
                        stock: product.stock + 1
                    }
                    : product
            )
        );
    }

    // Event handler: decrease stock for a product
    function handleRemoveStock(productId: number): void {
        setProducts((currentProducts) =>
            currentProducts.map((product) =>
                product.id === productId && product.stock > 0
                    ? {
                        ...product,
                        stock: product.stock - 1
                    }
                    : product
            )
        );
    }

    // Event handler: add a new product
    function handleAddProduct(
        productData: Omit<Product, "id">
    ): void {
        setProducts((currentProducts) => {
            const nextId =
                currentProducts.reduce(
                    (maxId, product) =>
                        Math.max(maxId, product.id),
                    0
                ) + 1;

            return [
                ...currentProducts,
                {
                    id: nextId,
                    ...productData
                }
            ];
        });
    }

    // Filter the products according to the current state
    const normalizedSearch = searchTerm.trim().toLowerCase();   //Αρχικά καθαρίζουμε τα κενά στις άκρες του κειμένου και μετατρέπουμε το κείμενο σε πεζά γράμματα. Έτσι, η αναζήτηση LAPTOP, Laptop ή laptop αντιμετωπίζεται με τον ίδιο τρόπο

    const filteredProducts = products.filter((product) => {
        const matchesSearch =   //ελέγχει αν το όνομα ή η κατηγορία περιέχει το κείμενο αναζήτησης
            product.name.toLowerCase().includes(normalizedSearch) ||
            product.category.toLowerCase().includes(normalizedSearch);

        const matchesStock =    //επιτρέπει όλα τα προϊόντα όταν το φίλτρο είναι απενεργοποιημένο. Όταν ενεργοποιείται, επιτρέπει μόνο όσα ειναι in stock και έχουν stock > 0
            !showInStockOnly || product.stock > 0;

        return matchesSearch && matchesStock;   //Ένα προϊόν εμφανίζεται μόνο όταν ικανοποιεί και τα 2 φίλτρα
    });

    const inStockCount = products.filter(
        (product) => product.stock > 0
    ).length;

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
                            Search products, filter the catalog,
                            and update stock levels.
                        </p>
                    </div>

                    <div className="summary-card">
                        <span>Catalog products</span>
                        <strong>{initialProducts.length}</strong>
                        <small>{inStockCount} with stock</small>
                    </div>
                </section>
                    <ProductForm onAddProduct={handleAddProduct} />
                <section
                    className="catalog-section" aria-labelledby="products-heading">
                    <div className="section-heading">
                        <div>
                            <h2 id="products-heading">Products</h2>

                            <p>
                                Manage the products in your catalog.
                            </p>
                        </div>

                        <span className="count-pill">
                            {filteredProducts.length} shown
                        </span>
                    </div>

                    <div className="product-controls">
                        <label
                            className="search-field"
                            htmlFor="product-search"
                        >
                            <span>Search products</span>

                            <input
                                id="product-search"
                                type="text"
                                value={searchTerm}
                                onChange={(event) =>
                                    setSearchTerm(event.target.value)
                                }
                                placeholder="Search by name or category..."
                            />
                            <button
                                className="filter-button"
                                type="button"
                                onClick={() => setSearchTerm("")}
                            >
                                Clear search
                            </button>
                        </label>

                        <button
                            className="filter-button"
                            type="button"
                            aria-pressed={showInStockOnly}
                            onClick={() =>
                                setShowInStockOnly((currentValue) => !currentValue) //Η συνάρτηση ενημέρωσης παίρνει την τρέχουσα τιμή και επιστρέφει την αντίθετή της
                            }
                        >
                            {showInStockOnly
                                ? "Show all products"
                                : "Show in-stock only"}
                        </button>
                    </div>

                    {showInStockOnly && (
                        <p className="filter-message">
                            Showing products that have available stock.
                        </p>
                    )}

                    {filteredProducts.length === 0 ? (
                        <div className="empty-state">
                            <h3>No products found</h3>

                            <p>
                                Try another search term or change the stock filter.
                            </p>
                        </div>
                    ) : (
                        <div className="product-grid">
                            {filteredProducts.map((product) => (
                                <ProductCard
                                    key={product.id}
                                    product={product}
                                    onRestock={handleRestock}
                                    onRemoveStock={handleRemoveStock}
                                />
                            ))}
                        </div>
                    )}
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