import { useState, type FormEvent } from "react";
import type { Product } from "../types";

interface ProductFormProps {
    onAddProduct: (productData: Omit<Product, "id">) => void;
}

export default function ProductForm({
    onAddProduct
}: ProductFormProps) {
    const [name, setName] = useState("");
    const [category, setCategory] = useState("");
    const [price, setPrice] = useState("");
    const [stock, setStock] = useState("0");

    const [errors, setErrors] = useState<string[]>([]);
    const [successMessage, setSuccessMessage] = useState("");

    function handleSubmit(
        event: FormEvent<HTMLFormElement>
    ): void {
        event.preventDefault();

        const trimmedName = name.trim();
        const trimmedCategory = category.trim();

        const numericPrice = Number(price);
        const numericStock = Number(stock);

        const validationErrors: string[] = [];

        if (trimmedName === "") {
            validationErrors.push(
                "Product name is required."
            );
        }

        if (trimmedCategory === "") {
            validationErrors.push(
                "Category is required."
            );
        }

        if (
            price.trim() === "" ||
            !Number.isFinite(numericPrice) ||
            numericPrice <= 0
        ) {
            validationErrors.push(
                "Price must be a valid number greater than 0."
            );
        }

        if (
            stock.trim() === "" ||
            !Number.isInteger(numericStock) ||
            numericStock < 0
        ) {
            validationErrors.push(
                "Stock must be a non-negative integer."
            );
        }

        if (validationErrors.length > 0) {
            setErrors(validationErrors);
            setSuccessMessage("");
            return;
        }

        onAddProduct({
            name: trimmedName,
            category: trimmedCategory,
            price: numericPrice,
            stock: numericStock
        });

        setName("");
        setCategory("");
        setPrice("");
        setStock("0");

        setErrors([]);
        setSuccessMessage(
            `${trimmedName} was added successfully.`
        );
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
                        <label htmlFor="product-category">
                            Category
                        </label>

                        <input
                            id="product-category"
                            name="category"
                            type="text"
                            value={category}
                            onChange={(event) =>
                                setCategory(event.target.value)
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
                >
                    Add product
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