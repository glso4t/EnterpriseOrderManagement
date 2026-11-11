import { useState, type FormEvent } from "react";
import type { CreateCustomerRequest } from "../types";
import SuccessMessage from "./SuccessMessage";

interface CustomerFormProps {
    onCreateCustomer: (
        customerData: CreateCustomerRequest
    ) => Promise<void>;
}

export default function CustomerForm({
    onCreateCustomer
}: CustomerFormProps) {

    // States

    const [name, setName] = useState("");
    const [email, setEmail] = useState("");

    const [errors, setErrors] = useState<string[]>([]);
    const [apiError, setApiError] = useState("");

    const [successMessage, setSuccessMessage] = useState("");

    const [isSubmitting, setIsSubmitting] = useState(false);


    // Event handler: handles form submission

    async function handleSubmit(
        event: FormEvent<HTMLFormElement>
    ): Promise<void> {

        event.preventDefault(); // Prevents the default form submission behavior

        const trimmedName = name.trim();
        const trimmedEmail = email.trim();
        const validationErrors: string[] = [];


        // Name validation

        if (trimmedName === "") {
            validationErrors.push(
                "Customer name is required."
            );
        } else if (trimmedName.length > 200) {
            validationErrors.push(
                "Customer name cannot exceed 200 characters."
            );
        }

        // Email validation

        if (trimmedEmail === "") {
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


        // Stop submission if validation fails

        if (validationErrors.length > 0) {
            setErrors(validationErrors);
            setApiError("");
            setSuccessMessage("");
            return;
        }


        // Send POST request

        try {   // If validation passes, proceed to submit the form
            setIsSubmitting(true);

            setErrors([]);
            setApiError("");
            setSuccessMessage("");
            await onCreateCustomer({
                name: trimmedName,
                email: trimmedEmail
            });
            setName("");
            setEmail("");
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
        <section className="customer-create-form">

            <div>
                <h3>Add new customer</h3>

                <p className="form-description">
                    Enter the details of the new customer.
                </p>
            </div>

            <form
                className="customer-create-form-fields"
                onSubmit={handleSubmit}
                noValidate
            >

                <div className="form-field">

                    <label htmlFor="customer-name">
                        Customer name
                    </label>

                    <input
                        id="customer-name"
                        name="name"
                        type="text"
                        maxLength={200}
                        value={name}
                        onChange={(event) =>
                            setName(event.target.value)
                        }
                        disabled={isSubmitting}
                        required
                    />

                </div>


                <div className="form-field">

                    <label htmlFor="customer-email">
                        Email address
                    </label>

                    <input
                        id="customer-email"
                        name="email"
                        type="email"
                        maxLength={320}
                        autoComplete="email"
                        value={email}
                        onChange={(event) =>
                            setEmail(event.target.value)
                        }
                        disabled={isSubmitting}
                        required
                    />

                </div>


                <button
                    className="submit-button"
                    type="submit"
                    disabled={isSubmitting}
                >
                    {isSubmitting
                        ? "Creating..."
                        : "Create customer"}
                </button>


                {errors.length > 0 && (
                    <div
                        className="form-errors"
                        role="alert"
                    >

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
                    <div
                        className="form-errors"
                        role="alert"
                    >

                        <p>Could not create customer.</p>

                        <p>{apiError}</p>

                    </div>
                )}


                {successMessage !== "" && (
                    <SuccessMessage 
                        message={successMessage} 
                    />
                )}

            </form>

        </section>
    );
}