import { useState, type FormEvent } from "react";
import type { CreateProductRequest } from "../types";

interface ProductFormProps {
    onAddProduct: (
        productData: CreateProductRequest
    ) => Promise<void>;
}

export default function ProductForm({
    onAddProduct
}: ProductFormProps) {
    const [name, setName] = useState("");
    const [price, setPrice] = useState("");
    const [stock, setStock] = useState("0");

    const [errors, setErrors] = useState<string[]>([]);
    const [apiError, setApiError] = useState("");

    const [successMessage, setSuccessMessage] = useState("");

    const [isSubmitting, setIsSubmitting] = useState(false);

    async function handleSubmit(
        event: FormEvent<HTMLFormElement>
    ): Promise<void> {
        event.preventDefault();
        const trimmedName = name.trim();
        const numericPrice = Number(price);
        const numericStock = Number(stock);
        const validationErrors: string[] = [];

        //name
        if (trimmedName === "") {
            validationErrors.push(
                "Product name is required."
            );
        } else if (trimmedName.length > 200) {
            validationErrors.push(
                "Product name cannot exceed 200 characters."
            );
        }//price
        if (
            price.trim() === "" ||
            !Number.isFinite(numericPrice) ||
            numericPrice < 0.01 ||
            numericPrice > 999999.99
        ) {
            validationErrors.push(
                "Price must be between 0.01 and 999999.99."
            );
        }//stock
        if (
            stock.trim() === "" ||
            !Number.isInteger(numericStock) ||
            numericStock < 0 ||
            numericStock > 2147483647
        ) {
            validationErrors.push(
                "Stock must be a non-negative integer."
            );
        }
        if (validationErrors.length > 0) {
            setErrors(validationErrors);
            setApiError("");
            setSuccessMessage("");
            return;
        }
        try {
            setIsSubmitting(true);
            setErrors([]);
            setApiError("");
            setSuccessMessage("");
            await onAddProduct({
                name: trimmedName,
                price: numericPrice,
                stock: numericStock
            });
            setName("");
            setPrice("");
            setStock("0");
            setSuccessMessage(
                `${trimmedName} was created successfully.`
            );
        } catch (error: unknown) {
            const message =
                error instanceof Error
                    ? error.message
                    : "An unexpected error occurred.";
            setApiError(message);
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <section className="product-form-section">
            <h2>Add new product</h2>

            <p className="form-description">
                Enter the details of the new product.
            </p>

            <form
                className="product-form"
                onSubmit={handleSubmit}
                noValidate
            >
                <div className="product-form-grid">
                    <div className="form-field">
                        <label htmlFor="product-name">
                            Product name
                        </label>
                        <input
                            id="product-name"
                            name="name"
                            type="text"
                            value={name}
                            onChange={(event) =>
                                setName(event.target.value)
                            }
                            required
                        />
                    </div>

                    <div className="form-field">
                        <label htmlFor="product-price">
                            Price (€)
                        </label>
                        <input
                            id="product-price"
                            name="price"
                            type="number"
                            min="0.01"
                            max="999999.99"
                            step="0.01"
                            value={price}
                            onChange={(event) =>
                                setPrice(event.target.value)
                            }
                            required
                        />
                    </div>

                    <div className="form-field">
                        <label htmlFor="product-stock">
                            Stock
                        </label>
                        <input
                            id="product-stock"
                            name="stock"
                            type="number"
                            min="0"
                            max="2147483647"
                            step="1"
                            value={stock}
                            onChange={(event) =>
                                setStock(event.target.value)
                            }
                            required
                        />
                    </div>
                </div>

                <button
                    className="submit-button"
                    type="submit"                    
                    disabled={isSubmitting}
                >
                    {isSubmitting
                        ? "Creating..."
                        : "Add product"}
                </button>
                {errors.length > 0 && (
                    <div
                        className="form-errors"
                        role="alert"
                    >
                        <p>Please fix the following errors:</p>

                        <ul>
                            {errors.map((error) => (
                                <li key={error}>{error}</li>
                            ))}
                        </ul>
                    </div>
                )}

                {apiError !== "" && (
                    <div
                        className="form-errors"
                        role="alert"
                    >
                        <p>Could not create product.</p>
                        <p>{apiError}</p>
                    </div>
                )}

                {successMessage !== "" && (
                    <p
                        className="form-success"
                        role="status"
                    >
                        {successMessage}
                    </p>
                )}
            </form>
        </section>
    );
}