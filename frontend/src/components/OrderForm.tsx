import { useState, type FormEvent } from "react";

import type {
    Customer,
    Product,
    CreateOrderRequest,
    Order
} from "../types";


interface OrderFormItem {
    productId: number;
    quantity: number | "";
}
interface OrderFormProps {
    customers: Customer[];
    products: Product[];
    onCreateOrder: (
        request: CreateOrderRequest
    ) => Promise<Order>;
    onCancel: () => void;
    isSubmitting: boolean;
}


const currencyFormatter = new Intl.NumberFormat(
    "el-GR",
    {
        style: "currency",
        currency: "EUR"
    }
);


// Component for creating a new order
export default function OrderForm({
    customers,
    products,
    onCreateOrder,
    onCancel,
    isSubmitting
}: OrderFormProps) {

    //States

    // Customer selection
    const [customerId, setCustomerId] = useState("");

    // Product selection
    const [productToAddId, setProductToAddId] =
        useState("");

    // Dynamic order items
    const [items, setItems] = useState<OrderFormItem[]>([]);

    // Validation and API errors
    const [validationErrors, setValidationErrors] =
        useState<string[]>([]);

    const [apiError, setApiError] =
        useState("");



    // Products that have stock and have not already been added
    const availableProducts = products.filter(
        (product) =>
            product.stock > 0 &&
            !items.some(
                (item) => item.productId === product.id
            )
    );

    // Calculate the estimated order total using integer cents
    const estimatedTotalInCents = items.reduce(
        (total, item) => {
            const product = products.find(
                (currentProduct) =>
                    currentProduct.id === item.productId
            );
            if (
                !product ||
                typeof item.quantity !== "number" ||
                !Number.isInteger(item.quantity) ||
                item.quantity <= 0
            ) {
                return total;
            }
            const unitPriceInCents = Math.round(
                product.price * 100
            );
            return total + (
                unitPriceInCents * item.quantity
            );
        },
        0
    );



    //Event handlers

    // Adds a selected product to the order
    function handleAddProduct(): void {
        const selectedProductId = Number(
            productToAddId
        );
        const product = products.find(
            (currentProduct) =>
                currentProduct.id === selectedProductId
        );
        if (
            !product ||
            product.stock <= 0 ||
            items.some(
                (item) => item.productId === selectedProductId
            )
        ) {
            setValidationErrors([
                "Select a valid product with available stock."
            ]);

            return;
        }
        setItems((currentItems) => [
            ...currentItems,
            {
                productId: selectedProductId,
                quantity: 1
            }
        ]);
        setProductToAddId("");
        setValidationErrors([]);
        setApiError("");
    }

    // Updates the quantity of an existing order item
    function handleQuantityChange(
        productId: number,
        value: number | ""
    ): void {
        setItems((currentItems) =>
            currentItems.map((item) =>
                item.productId === productId
                    ? {
                        ...item,
                        quantity: value
                    }
                    : item
            )
        );
        setValidationErrors([]);
        setApiError("");
    }

    // Αφαίρεση Product με filter()  (Κρατάμε όλα τα Order Items εκτός από εκείνο που θέλουμε να αφαιρέσουμε)
    function handleRemoveProduct(
        productId: number
    ): void {
        setItems((currentItems) =>
            currentItems.filter(
                (item) => item.productId !== productId
            )
        );
        setValidationErrors([]);
        setApiError("");
    }

    // Handles form submission
    async function handleSubmit(
        event: FormEvent<HTMLFormElement>
    ): Promise<void> {
        event.preventDefault();
        if (isSubmitting) {
            return;
        }
        const validationErrors: string[] = [];
        const selectedCustomerId = Number(customerId);
        const selectedCustomer = customers.find(
            (customer) =>
                customer.id === selectedCustomerId
        );
        // Customer validation
        if (!selectedCustomer || selectedCustomerId <= 0) {
            validationErrors.push(
                "Please select a valid customer."
            );
        }
        // Order items validation
        if (items.length === 0) {
            validationErrors.push(
                "Add at least one product to the order."
            );
        }
        const productIds = new Set<number>();
        for (const item of items) {
            const product = products.find(
                (currentProduct) =>
                    currentProduct.id === item.productId
            );
            if (!product) {
                validationErrors.push(
                    `Product ${item.productId} is no longer available.`
                );
                continue;
            }
            if (productIds.has(item.productId)) {
                validationErrors.push(
                    `${product.name} appears more than once.`
                );
            }
            productIds.add(item.productId);
            if (
                item.quantity === "" ||
                !Number.isInteger(item.quantity) ||
                item.quantity <= 0
            ) {
                validationErrors.push(
                    `${product.name}: quantity must be a positive integer.`
                );
                continue;
            }
            if (item.quantity > product.stock) {
                validationErrors.push(
                    `${product.name}: only ${product.stock} unit(s) are available.`
                );
            }
        }
        // Stop submission if validation fails
        if (validationErrors.length > 0) {
            setValidationErrors(validationErrors);
            setApiError("");
            return;
        }
        // Build the request object
        const request: CreateOrderRequest = {
            customerId: selectedCustomerId,
            items: items.map((item) => ({
                productId: item.productId,
                quantity: Number(item.quantity)
            }))
        };
        // Send request through the parent component
        try {
            setValidationErrors([]);
            setApiError("");
            await onCreateOrder(request);
            // Close the form after successful creation
            onCancel();
        } catch (error: unknown) {
            const message =
                error instanceof Error
                    ? error.message
                    : "An unexpected error occurred.";
            setApiError(message);
        }
    }


    return (

        <section className="order-form-section">

            <div className="order-form-heading">

                <div>
                    <h2>Create Order</h2>

                    <p>
                        Select a customer and add products to the order.
                    </p>
                </div>

                <button
                    className="filter-button"
                    type="button"
                    onClick={onCancel}
                    disabled={isSubmitting}
                >
                    Close
                </button>

            </div>


            <form
                className="order-form"
                onSubmit={handleSubmit}
                noValidate
            >

                {/* Customer Selection */}

                <div className="form-field">

                    <label htmlFor="order-customer">
                        Customer
                    </label>

                    <select
                        id="order-customer"
                        value={customerId}
                        onChange={(event) =>
                            setCustomerId(event.target.value)
                        }
                        disabled={isSubmitting}
                        required
                    >

                        <option value="">
                            Select a customer
                        </option>

                        {customers.map((customer) => (

                            <option
                                key={customer.id}
                                value={customer.id}
                            >
                                {customer.name} — {customer.email}
                            </option>

                        ))}

                    </select>

                </div>


                {/* Product Selection */}

                <div className="order-product-selector">

                    <div className="form-field">

                        <label htmlFor="order-product">
                            Add product
                        </label>

                        <select
                            id="order-product"
                            value={productToAddId}
                            onChange={(event) =>
                                setProductToAddId(event.target.value)
                            }
                            disabled={
                                isSubmitting ||
                                availableProducts.length === 0
                            }
                        >

                            <option value="">
                                Select a product
                            </option>

                            {availableProducts.map((product) => (

                                <option
                                    key={product.id}
                                    value={product.id}
                                >
                                    {product.name} —{" "}
                                    {currencyFormatter.format(product.price)}
                                    {" · Stock: "}
                                    {product.stock}
                                </option>

                            ))}

                        </select>

                    </div>


                    <button
                        className="filter-button"
                        type="button"
                        onClick={handleAddProduct}
                        disabled={
                            isSubmitting ||
                            productToAddId === ""
                        }
                    >
                        Add product
                    </button>

                </div>


                {availableProducts.length === 0 && (

                    <p className="order-form-hint">
                        No additional products with available stock.
                    </p>

                )}


                {/* Selected Order Items */}

                <div className="order-selected-items">

                    <h3>Order Items</h3>


                    {items.length === 0 ? (

                        <p className="order-form-hint">
                            No products have been added yet.
                        </p>

                    ) : (

                        items.map((item) => {

                            const product = products.find(
                                (currentProduct) =>
                                    currentProduct.id === item.productId
                            );

                            if (!product) {
                                return null;
                            }


                            const quantity =
                                typeof item.quantity === "number"
                                    ? item.quantity
                                    : 0;

                            const lineTotalInCents =
                                Math.round(product.price * 100) *
                                (
                                    Number.isInteger(quantity) &&
                                    quantity > 0
                                        ? quantity
                                        : 0
                                );


                            return (

                                <div
                                    className="order-form-item"
                                    key={product.id}
                                >

                                    <div className="order-form-item-info">

                                        <strong>
                                            {product.name}
                                        </strong>

                                        <span>
                                            {currencyFormatter.format(
                                                product.price
                                            )}
                                            {" · "}
                                            Available: {product.stock}
                                        </span>

                                    </div>


                                    <div className="order-form-item-controls">

                                        <label
                                            htmlFor={`order-quantity-${product.id}`}
                                        >
                                            Quantity
                                        </label>

                                        <input
                                            id={`order-quantity-${product.id}`}
                                            type="number"
                                            min={1}
                                            max={product.stock}
                                            step={1}
                                            value={item.quantity}
                                            onChange={(event) =>
                                                handleQuantityChange(
                                                    product.id,
                                                    event.target.value === ""
                                                        ? ""
                                                        : Number(event.target.value)
                                                )
                                            }
                                            disabled={isSubmitting}
                                        />

                                    </div>


                                    <strong className="order-form-line-total">
                                        {currencyFormatter.format(
                                            lineTotalInCents / 100
                                        )}
                                    </strong>


                                    <button
                                        className="remove-order-item-button"
                                        type="button"
                                        onClick={() =>
                                            handleRemoveProduct(product.id)
                                        }
                                        disabled={isSubmitting}
                                        aria-label={`Remove ${product.name}`}
                                    >
                                        Remove
                                    </button>

                                </div>

                            );
                        })

                    )}

                </div>


                {/* Order Summary */}

                <div className="order-form-summary">

                    <span>Estimated Total</span>

                    <strong>
                        {currencyFormatter.format(
                            estimatedTotalInCents / 100
                        )}
                    </strong>

                    <p>
                        Final prices and stock availability are confirmed by the server.
                    </p>

                </div>


                {/* Validation Errors */}

                {validationErrors.length > 0 && (

                    <div className="form-errors" role="alert">

                        <p>Please fix the following errors:</p>

                        <ul>
                            {validationErrors.map((error, index) => (
                                <li key={`${error}-${index}`}>
                                    {error}
                                </li>
                            ))}
                        </ul>

                    </div>

                )}


                {/* API Errors */}

                {apiError !== "" && (

                    <div className="form-errors" role="alert">

                        <p>Could not create order.</p>

                        <p>{apiError}</p>

                    </div>

                )}


                {/* Submit */}

                <div className="order-form-actions">

                    <button
                        className="submit-button"
                        type="submit"
                        disabled={isSubmitting}
                    >
                        {isSubmitting
                            ? "Creating order..."
                            : "Create order"}
                    </button>

                    <button
                        className="filter-button"
                        type="button"
                        onClick={onCancel}
                        disabled={isSubmitting}
                    >
                        Cancel
                    </button>

                </div>

            </form>

        </section>
    );
}