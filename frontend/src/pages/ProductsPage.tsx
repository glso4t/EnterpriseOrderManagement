import { useEffect, useState } from "react";

import ProductCard from "../components/ProductCard";
import ProductForm from "../components/ProductForm";
import PageFeedback from "../components/PageFeedback";
import { useAuth } from "../auth/useAuth";

import {
    createProduct,
    getProducts,
    patchProduct,
    updateProduct,
    deleteProduct
} from "../services/productService";

import type {
    CreateProductRequest,
    Product,
    UpdateProductRequest
} from "../types";


interface ProductsPageProps {
    isActive: boolean;
}

export default function ProductsPage({ isActive }: ProductsPageProps) {

    // States

    const [products, setProducts] =
        useState<Product[]>([]);

    const [isLoading, setIsLoading] = 
        useState(true); 

    const [loadError, setLoadError] = 
        useState<string | null>(null);  

    const [searchTerm, setSearchTerm] = 
        useState("");

    const [showInStockOnly, setShowInStockOnly] =
        useState(false);

    // State to track which products are currently being updated
    const [updatingProductIds, setUpdatingProductIds] = 
        useState<number[]>([]);

    // State to track any errors that occur during stock updates
    const [stockUpdateError, setStockUpdateError] =
        useState<string | null>(null);

    // περιέχει τα IDs των προϊόντων που βρίσκονται αυτή τη στιγμή σε διαδικασία διαγραφής
    const [deletingProductIds, setDeletingProductIds] =
        useState<number[]>([]);

    // κρατά το μήνυμα σφάλματος όταν το DELETE request αποτυγχάνει
    const [deleteError, setDeleteError] =
        useState<string | null>(null);

    const { user } = useAuth();

    const isAdmin = user?.role === "Admin";


    //Effects

    //Effect: update the document title whenever the number of products changes
    useEffect(() => {
        document.title = `Inventory Dashboard (${products.length})`;
    }, [products.length]);

    // Effect: load products from the ASP.NET Core API
    useEffect(() => {
        if (!isActive) {    // If the ProductsPage is not active, do not load products
            return;
        }
        let ignore = false;
        async function loadProducts(): Promise<void> {  //Load products from the API and update the state
            try {
                setIsLoading(true);
                setLoadError(null);
                const loadedProducts = await getProducts();
                if (!ignore) {
                    setProducts(loadedProducts);
                }
            } catch (error: unknown) {
                if (!ignore) {
                    const message =
                        error instanceof Error
                            ? error.message
                            : "An unexpected error occurred.";
                    setLoadError(message);
                }
            } finally {
                if (!ignore) {
                    setIsLoading(false);
                }
            }
        }
        loadProducts();
        return () => {
            ignore = true;
        };
    }, [isActive]);


    //Event handlers

    //Event handler: delete a product
    async function handleDeleteProduct(
        productId: number
    ): Promise<void> {
        if (        //Αν το προϊόν είναι ήδη σε διαδικασία διαγραφής ή ενημέρωσης, δεν κάνουμε τίποτα
            deletingProductIds.includes(productId) ||
            updatingProductIds.includes(productId)
        ) {
            return;
        }
        setDeletingProductIds((currentIds) => [     //Προσθέτουμε το productId στη λίστα των προϊόντων που διαγράφονται
            ...currentIds,
            productId
        ]);
        setDeleteError(null);
        try {       //Καλούμε τη συνάρτηση deleteProduct για να διαγράψουμε το προϊόν από τον server. Αν η διαγραφή είναι επιτυχής, ενημερώνουμε το state των προϊόντων αφαιρώντας το διαγραμμένο προϊόν από τη λίστα. Αν η διαγραφή αποτύχει, εμφανίζουμε το μήνυμα σφάλματος.
            await deleteProduct(productId);
            setProducts((currentProducts) =>
                currentProducts.filter(
                    (product) => product.id !== productId
                )
            );
        } catch (error: unknown) {
            const message =
                error instanceof Error
                    ? error.message
                    : "An unexpected error occurred.";
            setDeleteError(message);
        } finally {     //Αφαιρούμε το productId από τη λίστα των προϊόντων που διαγράφονται, ανεξάρτητα από το αν η διαγραφή ήταν επιτυχής ή όχι
            setDeletingProductIds((currentIds) =>
                currentIds.filter(
                    (id) => id !== productId
                )
            );
        }
    }

    //Event handler: update a product
    async function handleUpdateProduct(
        productId: number,
        productData: UpdateProductRequest
    ): Promise<void> {
        if (    //Αν το προϊόν είναι ήδη σε διαδικασία ενημέρωσης ή διαγραφής, δεν κάνουμε τίποτα
            updatingProductIds.includes(productId) ||
            deletingProductIds.includes(productId)
        ) {
            throw new Error(
                "This product is already being processed."
            );
        }
        setUpdatingProductIds((currentIds) => [ //Προσθέτουμε το productId στη λίστα των προϊόντων που ενημερώνονται
            ...currentIds,
            productId
        ]);
        try {       //Καλούμε τη συνάρτηση updateProduct για να ενημερώσουμε το προϊόν στον server. Αν η ενημέρωση είναι επιτυχής, ενημερώνουμε το state των προϊόντων αντικαθιστώντας το ενημερωμένο προϊόν στη λίστα. Αν η ενημέρωση αποτύχει, εμφανίζουμε το μήνυμα σφάλματος.
            const updatedProduct = await updateProduct(
                productId,
                productData
            );
            setProducts((currentProducts) =>
                currentProducts.map((product) =>
                    product.id === updatedProduct.id
                        ? {
                            ...updatedProduct,
                            category: product.category
                        }
                        : product
                )
            );  //δεν υπάρχει catch εδώ, επειδή θέλουμε το σφάλμα να μεταφερθεί πίσω στο ProductEditForm, το οποίο έχει ήδη δικό του try/catch και εμφανίζει το apiError.
        } finally { //Αφαιρούμε το productId από τη λίστα των προϊόντων που ενημερώνονται, ανεξάρτητα από το αν η ενημέρωση ήταν επιτυχής ή όχι
            setUpdatingProductIds((currentIds) =>
                currentIds.filter(
                    (id) => id !== productId
                )
            );
        }
    }

    //Event handler: update the stock of a product
    async function updateProductStock(
        productId: number,
        newStock: number
    ): Promise<void> {
        if (updatingProductIds.includes(productId)) {       //Αν το προϊόν είναι ήδη σε διαδικασία ενημέρωσης, δεν κάνουμε τίποτα
            return;
        }
        setUpdatingProductIds((currentIds) => [     //Προσθέτουμε το productId στη λίστα των προϊόντων που ενημερώνονται
            ...currentIds,
            productId
        ]);
        setStockUpdateError(null);
        try {
            const updatedProduct = await patchProduct(      
                productId,
                {
                    stock: newStock     //Απαιτείται μόνο το stock για την ενημέρωση
                }
            );
            setProducts((currentProducts) =>        //Ενημέρωση του state των προϊόντων με το ενημερωμένο προϊόν
                currentProducts.map((product) =>
                    product.id === updatedProduct.id        //Αν το productId ταιριάζει με το ενημερωμένο προϊόν, αντικαθιστούμε το προϊόν με το ενημερωμένο προϊόν, αλλιώς κρατάμε το υπάρχον προϊόν
                        ? {
                            ...updatedProduct,      
                            category: product.category
                        }
                        : product
                )
            );
        } catch (error: unknown) {
            const message =
                error instanceof Error
                    ? error.message
                    : "An unexpected error occurred.";
            setStockUpdateError(message);
        } finally {     //Αφαιρούμε το productId από τη λίστα των προϊόντων που ενημερώνονται, ανεξάρτητα από το αν η ενημέρωση ήταν επιτυχής ή όχι
            setUpdatingProductIds((currentIds) =>
                currentIds.filter(
                    (id) => id !== productId
                )
            );
        }
    }

    // Event handler: increase stock for a product
    async function handleRestock(productId: number): Promise<void> {
        const product = products.find(
            (item) => item.id === productId
        );
        if (!product) {
            return;
        }
        await updateProductStock(
            productId,
            product.stock + 1
        );
    }

    // Event handler: decrease stock for a product
    async function handleRemoveStock(productId: number): Promise<void> {
        const product = products.find(
            (item) => item.id === productId
        );
        if (!product || product.stock <= 0) {
            return;
        }
        await updateProductStock(
            productId,
            product.stock - 1
        );
    }

    // Event handler: add a new product 
    async function handleAddProduct(
        productData: CreateProductRequest
    ): Promise<void> {
        const createdProduct = await createProduct(productData);
        setProducts((currentProducts) => [
            ...currentProducts,
            createdProduct
        ]);
    }


    //Filtering & derived values

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
    <>
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
                <strong>{products.length}</strong>
                <small>{inStockCount} with stock</small>
            </div>
        </section>
        {isAdmin && (
            <ProductForm onAddProduct={handleAddProduct} />
        )}
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

                {stockUpdateError !== null && (
                    <div
                        className="catalog-error"
                        role="alert"
                    >
                        <p>Could not update product stock.</p>
                        <p>{stockUpdateError}</p>
                    </div>
                )}

                {deleteError !== null && (
                    <div className="catalog-error" role="alert">
                        <h3>Could not delete product</h3>
                        <p>{deleteError}</p>
                    </div>
                )}

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

            {isLoading ? (
                <PageFeedback
                    variant="loading"
                    title="Loading products"
                    message="Please wait while products are being loaded..."
                />
            ) : loadError ? (
                <PageFeedback
                    variant="error"
                    title="Could not load products"
                    message={loadError}
                />
            ) : filteredProducts.length === 0 ? (
                <PageFeedback
                    variant="empty"
                    title="No products found"
                    message="Try another search term or change the stock filter."
                    actionLabel="Clear search"
                    onAction={() => {
                        setSearchTerm("");
                        setShowInStockOnly(false);
                    }}
                />
            ) : (
                    <div className="product-grid">
                        {filteredProducts.map((product) => (
                            <ProductCard
                                key={product.id}
                                product={product}
                                isAdmin={isAdmin}
                                onRestock={handleRestock}
                                onRemoveStock={handleRemoveStock}
                                onUpdate={handleUpdateProduct}
                                onDelete={handleDeleteProduct}
                                isUpdating={updatingProductIds.includes(product.id)}
                                isDeleting={deletingProductIds.includes(product.id)}
                            />
                        ))}
                    </div>
                )}
            </section>
        </>
    );


}