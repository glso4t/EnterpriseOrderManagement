import { useState } from "react";
import ProductEditForm from "./ProductEditForm";
import type { Product, UpdateProductRequest } from "../types";

interface ProductCardProps {
    product: Product;
    onRestock: (productId: number) => Promise<void>;
    onRemoveStock: (productId: number) => Promise<void>;
    onDelete: (productId: number) => Promise<void>;
    onUpdate: (productId: number, productData: UpdateProductRequest) => Promise<void>;
    isUpdating: boolean;
    isDeleting: boolean;
}

export default function ProductCard({
    product,
    onRestock,
    onRemoveStock,
    onDelete,
    onUpdate,
    isUpdating,
    isDeleting
}: ProductCardProps) {
    let stockLabel: string;
    let stockClass: string;
    const [isEditing, setIsEditing] = useState(false);

    if (product.stock === 0) {
        stockLabel = "Out of stock";
        stockClass = "out-of-stock";
    } else if (product.stock <= 3) {
        stockLabel = "Low stock";
        stockClass = "low-stock";
    } else {
        stockLabel = "In stock";
        stockClass = "in-stock";
    }

    function handleDelete(): void {
    const confirmed = window.confirm(
        `Are you sure you want to delete "${product.name}"?`
    );
    if (confirmed) {
        void onDelete(product.id);
    }
}

    return (
        <article className="product-card">
            <div className="product-card-top">
                <span className="product-category">
                    {product.category}
                </span>

                <span className={`stock-badge ${stockClass}`}>
                    {stockLabel}
                </span>
            </div>

            <p className="product-id">
                PRODUCT #{String(product.id).padStart(3, "0")}
            </p>

            <h3>{product.name}</h3>

            <div className="product-card-bottom">
                <div>
                    <p className="price-label">Unit price</p>

                    <p className="product-price">
                        {product.price.toFixed(2)} <span>€</span>
                    </p>
                </div>

                <div className="stock-count">
                    <strong>{product.stock}</strong>
                    <span>units</span>
                </div>
            </div>
            <div className="stock-actions">
                <button
                    className="restock-button"
                    type="button"
                    disabled={isUpdating || isDeleting || isEditing}
                    onClick={() => void onRestock(product.id)}
                >
                    {isUpdating ? "Updating..." : "Restock +1"}
                </button>
                <button
                    className="restock-button"
                    type="button"
                    disabled={isUpdating || isDeleting ||  product.stock === 0 || isEditing}
                    onClick={() => void onRemoveStock(product.id)}
                >
                    {isUpdating ? "Updating..." : "Remove -1"}
                </button>

                {isEditing ? (
                    <ProductEditForm
                        product={product}
                        onSave={onUpdate}
                        onCancel={() => setIsEditing(false)}
                        isSaving={isUpdating}
                    />
                ) : (
                    <button
                        className="edit-button"
                        type="button"
                        disabled={isUpdating || isDeleting}
                        onClick={() => setIsEditing(true)}
                    >
                        Edit 
                    </button>
                )}

                <button
                    className="delete-button"
                    type="button"
                    disabled={isUpdating || isDeleting || isEditing}
                    onClick={handleDelete}
                >
                    {isDeleting ? "Deleting..." : "Delete"}
                </button>
            </div>
        </article>
    );
}