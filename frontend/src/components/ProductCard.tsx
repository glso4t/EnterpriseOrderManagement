import type { Product } from "../types";

interface ProductCardProps {
    product: Product;
    onRestock: (productId: number) => void;
    onRemoveStock: (productId: number) => void;
}

export default function ProductCard({
    product,
    onRestock,
    onRemoveStock
}: ProductCardProps) {
    let stockLabel: string;
    let stockClass: string;

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
            <button
                className="restock-button"
                onClick={() => onRestock(product.id)}
            >
                Restock +1
            </button>
            <button
                className="restock-button"
                type="button"
                disabled={product.stock === 0}
                onClick={() => onRemoveStock(product.id)}
            >
                Remove -1
            </button>
        </article>
    );
}