import { apiRequest } from "./apiClient";
import type { Customer } from "../types";

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
    const response = await apiRequest(
        "/api/v1/customers"
    );
    const data: unknown = await response.json();
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
    const response = await apiRequest(
        `/api/v1/customers/${customerId}`
    );
    const data: unknown = await response.json();
    return mapCustomerResponse(data);
}