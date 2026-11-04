import { useState, type FormEvent } from "react";

import type {
    Customer,
    UpdateCustomerRequest
} from "../types";


interface CustomerEditFormProps {
    customer: Customer;
    onSave: (
        customerId: number,
        customerData: UpdateCustomerRequest
    ) => Promise<void>;
    onCancel: () => void;
    isSaving: boolean;
}

// Component for editing an existing customer
export default function CustomerEditForm({
    customer,
    onSave,
    onCancel,
    isSaving
}: CustomerEditFormProps) {
    // Initialize the inputs with the existing customer data
    const [name, setName] = useState(customer.name);
    const [email, setEmail] = useState(customer.email);

    const [errors, setErrors] = useState<string[]>([]);
    const [apiError, setApiError] = useState("");

    async function handleSubmit(    //Event handler for form submission
        event: FormEvent<HTMLFormElement>
    ): Promise<void> {

        event.preventDefault();

        const trimmedName = name.trim();
        const trimmedEmail = email.trim();

        const validationErrors: string[] = [];

        if (trimmedName === "") {   // Validate customer name
            validationErrors.push(
                "Customer name is required."
            );
        } else if (trimmedName.length > 200) {
            validationErrors.push(
                "Customer name cannot exceed 200 characters."
            );
        }
        if (trimmedEmail === "") { // Validate email
            validationErrors.push(
                "Email address is required."
            );
        } else if (trimmedEmail.length > 320) {
            validationErrors.push(
                "Email cannot exceed 320 characters."
            );
        } else if (/\s/.test(trimmedEmail)) {
            validationErrors.push(
                "Email cannot contain whitespace."
            );
        } else if (
            !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)
        ) {
            validationErrors.push(
                "Please enter a valid email address."
            );
        }
        if (validationErrors.length > 0) { // If there are validation errors, set the errors state and return early
            setErrors(validationErrors);
            setApiError("");
            return;
        }
        try {  // If validation passes, call the onSave prop with the updated product data
            setErrors([]);
            setApiError("");
            await onSave(customer.id, {
                name: trimmedName,
                email: trimmedEmail
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
        <section className="customer-edit-form">

            <h3>Edit customer</h3>

            <form
                className="customer-edit-form-fields"
                onSubmit={handleSubmit}
                noValidate
            >

                <div className="form-field">

                    <label htmlFor={`edit-customer-name-${customer.id}`}>
                        Customer name
                    </label>

                    <input
                        id={`edit-customer-name-${customer.id}`}
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

                    <label htmlFor={`edit-customer-email-${customer.id}`}>
                        Email address
                    </label>

                    <input
                        id={`edit-customer-email-${customer.id}`}
                        type="email"
                        maxLength={320}
                        autoComplete="email"
                        value={email}
                        onChange={(event) =>
                            setEmail(event.target.value)
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
                        {isSaving
                            ? "Saving..."
                            : "Save changes"}
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
                                <li key={error}>
                                    {error}
                                </li>
                            ))}
                        </ul>

                    </div>
                )}


                {apiError !== "" && (
                    <div className="form-errors" role="alert">

                        <p>Could not update customer.</p>

                        <p>{apiError}</p>

                    </div>
                )}

            </form>

        </section>
    );
}