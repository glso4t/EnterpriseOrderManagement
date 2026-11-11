import { apiRequest } from "./apiClient";
import type {
    Customer,
    CreateCustomerRequest,
    UpdateCustomerRequest
} from "../types";

// Checks whether the API response has the expected structure
function isCustomer(
    value: unknown
): value is Customer {
    if (typeof value !== "object" || value === null) {
        return false;
    }
    const customer = value as Record<string, unknown>;
    return (
        typeof customer.id === "number" &&
        Number.isInteger(customer.id) &&
        typeof customer.name === "string" &&
        typeof customer.email === "string"
    );
}

// maps a CustomerApiResponse to a Customer
function mapCustomerResponse(value: unknown): Customer {
    if (!isCustomer(value)) {
        throw new Error(
            "The API returned a customer with an invalid structure."
        );
    }
    return value;
}


// (GET) fetches the list of customers from the API and returns them as an array of Customer objects
export async function getCustomers(): Promise<Customer[]> {
    const data = await apiRequest<unknown>(
        "/api/v1/customers"
    );
    if (!Array.isArray(data)) {
        throw new Error(
            "Unexpected API response. Expected an array of customers."
        );
    }
    const apiCustomers: unknown[] = data;
    return apiCustomers.map(mapCustomerResponse);
}

// (GET BY ID) fetches a customer by ID from the API and returns it as a Customer object
export async function getCustomerById(
    customerId: number
): Promise<Customer> {
    if (!Number.isInteger(customerId) || customerId <= 0) {
        throw new Error("Invalid customer ID.");
    }
    const data = await apiRequest<unknown>(
        `/api/v1/customers/${customerId}`
    );
    return mapCustomerResponse(data);
}

// (POST) creates a new customer in the API and returns the created Customer object
export async function createCustomer(
    customerData: CreateCustomerRequest
): Promise<Customer> {
    const data = await apiRequest<unknown>(
        "/api/v1/customers",
        {
            method: "POST",
            body: JSON.stringify(customerData)
        }
    );
    return mapCustomerResponse(data);
}

// (PUT) updates an existing customer in the API and returns the updated Customer object
export async function updateCustomer(
    customerId: number,
    customerData: UpdateCustomerRequest
): Promise<Customer> {
    // Validate the customer ID
    if (!Number.isInteger(customerId) || customerId <= 0) {
        throw new Error("Invalid customer ID.");
    }
    const data = await apiRequest<unknown>(
        `/api/v1/customers/${customerId}`,
        {
            method: "PUT",
            body: JSON.stringify(customerData)
        }
    );
    const updatedCustomer = mapCustomerResponse(data);
    // Ensure that the API returned the requested customer
    if (updatedCustomer.id !== customerId) {
        throw new Error(
            "The API returned an unexpected customer."
        );
    }
    return updatedCustomer;
}

// (DELETE) deletes a customer by ID from the API
export async function deleteCustomer(
    customerId: number
): Promise<void> {
    // Validate the customer ID
    if (!Number.isInteger(customerId) || customerId <= 0) {
        throw new Error("Invalid customer ID.");
    }
    await apiRequest<void>(
        `/api/v1/customers/${customerId}`,
        {
            method: "DELETE"
        }
    );
}