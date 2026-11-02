import { useState, type FormEvent } from "react";
import type { Product, UpdateProductRequest } from "../types";

interface ProductEditFormProps {
    product: Product;
    onSave: (
        productId: number,
        productData: UpdateProductRequest
    ) => Promise<void>;
    onCancel: () => void;
    isSaving: boolean;
}
// Component for editing an existing product
export default function ProductEditForm({
    product,
    onSave,
    onCancel,
    isSaving
}: ProductEditFormProps) {
    const [name, setName] = useState(product.name);
    const [price, setPrice] = useState(String(product.price));
    const [stock, setStock] = useState(String(product.stock));

    const [errors, setErrors] = useState<string[]>([]);
    const [apiError, setApiError] = useState("");

    async function handleSubmit(    //Event handler for form submission
        event: FormEvent<HTMLFormElement>
    ): Promise<void> {
        event.preventDefault();

        const trimmedName = name.trim();

        const numericPrice = Number(price);
        const numericStock = Number(stock);

        const validationErrors: string[] = [];

        if (    // Validate product name
            trimmedName === "" ||
            trimmedName.length > 200
        ) {
            validationErrors.push(
                "Product name must contain 1 to 200 characters."
            );
        }
        if (    // Validate price
            price.trim() === "" ||
            !Number.isFinite(numericPrice) ||
            numericPrice < 0.01 ||
            numericPrice > 999999.99
        ) {
            validationErrors.push(
                "Price must be between 0.01 and 999999.99."
            );
        }
        if (    // Validate stock
            stock.trim() === "" ||
            !Number.isInteger(numericStock) ||
            numericStock < 0 ||
            numericStock > 2147483647
        ) {
            validationErrors.push(
                "Stock must be a non-negative integer."
            );
        }
        if (validationErrors.length > 0) {  // If there are validation errors, set the errors state and return early
            setErrors(validationErrors);
            setApiError("");
            return;
        }
        try {   // If validation passes, call the onSave prop with the updated product data
            setErrors([]);
            setApiError("");
            await onSave(product.id, {
                name: trimmedName,
                price: numericPrice,
                stock: numericStock
            });
            onCancel(); // After saving, call onCancel to close the form
        } catch (error: unknown) {
            const message =
                error instanceof Error
                    ? error.message
                    : "An unexpected error occurred.";
            setApiError(message);
        }
    }

    return (
        <section className="product-edit-form">
            <h4>Edit product</h4>

            <form
                className="product-form"
                onSubmit={handleSubmit}
                noValidate
            >
                <div className="form-field">
                    <label htmlFor={`edit-name-${product.id}`}>
                        Product name
                    </label>

                    <input
                        id={`edit-name-${product.id}`}
                        type="text"
                        maxLength={200}
                        value={name}
                        onChange={(event) =>
                            setName(event.target.value)
                        }
                        disabled={isSaving}
                        required
                    />
                </div>

                <div className="form-field">
                    <label htmlFor={`edit-price-${product.id}`}>
                        Price (€)
                    </label>

                    <input
                        id={`edit-price-${product.id}`}
                        type="number"
                        min="0.01"
                        max="999999.99"
                        step="0.01"
                        value={price}
                        onChange={(event) =>
                            setPrice(event.target.value)
                        }
                        disabled={isSaving}
                        required
                    />
                </div>

                <div className="form-field">
                    <label htmlFor={`edit-stock-${product.id}`}>
                        Stock
                    </label>

                    <input
                        id={`edit-stock-${product.id}`}
                        type="number"
                        min="0"
                        max="2147483647"
                        step="1"
                        value={stock}
                        onChange={(event) =>
                            setStock(event.target.value)
                        }
                        disabled={isSaving}
                        required
                    />
                </div>

                <div className="edit-form-actions">
                    <button
                        className="submit-button"
                        type="submit"
                        disabled={isSaving}
                    >
                        {isSaving ? "Saving..." : "Save changes"}
                    </button>

                    <button
                        className="filter-button"
                        type="button"
                        onClick={onCancel}
                        disabled={isSaving}
                    >
                        Cancel
                    </button>
                </div>

                {errors.length > 0 && (
                    <div className="form-errors" role="alert">
                        <p>Please fix the following errors:</p>

                        <ul>
                            {errors.map((error) => (
                                <li key={error}>{error}</li>
                            ))}
                        </ul>
                    </div>
                )}

                {apiError !== "" && (
                    <div className="form-errors" role="alert">
                        <p>Could not update product.</p>
                        <p>{apiError}</p>
                    </div>
                )}
            </form>
        </section>
    );
}