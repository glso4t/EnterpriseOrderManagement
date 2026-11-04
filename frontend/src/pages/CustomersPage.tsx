import { useEffect, useState } from "react";

import CustomerForm from "../components/CustomerForm";

import type {
    Customer,
    CreateCustomerRequest
} from "../types";
import {
    getCustomers,
    getCustomerById,
    createCustomer
} from "../services/customerService";

export default function CustomersPage() {
    //States

    // Customers list state
    const [customers, setCustomers] = useState<Customer[]>([]);

    // Selected customer state
    const [selectedCustomerId, setSelectedCustomerId] =
        useState<number | null>(null);

    const [selectedCustomer, setSelectedCustomer] =
        useState<Customer | null>(null);

    // List loading and error states
    const [isLoading, setIsLoading] = useState(true);

    const [loadError, setLoadError] =
        useState<string | null>(null);

    // Details loading and error states
    const [isDetailsLoading, setIsDetailsLoading] =
        useState(false);

    const [detailsError, setDetailsError] =
        useState<string | null>(null);

    //controls whether the customer creation form is visible
    const [isCreateFormOpen, setIsCreateFormOpen] =
        useState(false);


    // Effects  

    // Effect: load all customers
    useEffect(() => {
        let ignore = false;
        async function loadCustomers(): Promise<void> {
            try {
                setIsLoading(true);
                setLoadError(null);
                const loadedCustomers = await getCustomers();
                if (!ignore) {  // Only update state if the component is still mounted
                    setCustomers(loadedCustomers);
                    if (loadedCustomers.length > 0) {
                        setSelectedCustomerId(
                            loadedCustomers[0].id
                        );
                    }
                }
            } catch (error: unknown) {
                if (!ignore) {
                    const message =
                        error instanceof Error
                            ? error.message
                            : "An unexpected error occurred.";
                    setLoadError(message);
                }
            } finally { // Ensure that loading state is updated even if an error occurs
                if (!ignore) {
                    setIsLoading(false);
                }
            }
        }
        loadCustomers();
        return () => {
            ignore = true;
        };
    }, []);


    // Effect: load details for the selected customer
    //**Όταν επιλέγεις έναν πελάτη από τη λίστα και δεν έχουμε τα στοιχεία του, γίνεται GET by ID.
    //**Όταν δημιουργείς έναν νέο πελάτη, έχουμε ήδη την απάντηση του POST και δεν χρειάζεται δεύτερο GET.
    //**Αν επιλέξεις άλλον πελάτη, η συνθήκη δεν ισχύει και το GET εκτελείται κανονικά.
    useEffect(() => {
        if (selectedCustomerId === null) {
            setSelectedCustomer(null);
            return;
        }
        // Customer details are already available
        if (selectedCustomer?.id === selectedCustomerId) {
            return;
        }
        const customerId = selectedCustomerId;
        let ignore = false;
        async function loadCustomerDetails(): Promise<void> {
            try {
                setIsDetailsLoading(true);
                setDetailsError(null);
                const customer = await getCustomerById(
                    customerId
                );
                if (!ignore) {  // Only update state if the component is still mounted
                    setSelectedCustomer(customer);
                }
            } catch (error: unknown) {
                if (!ignore) {
                    const message =
                        error instanceof Error
                            ? error.message
                            : "An unexpected error occurred.";
                    setDetailsError(message);
                }
            } finally {
                if (!ignore) {
                    setIsDetailsLoading(false);
                }
            }
        }
        loadCustomerDetails();
        return () => {
            ignore = true;
        };
    }, [selectedCustomerId, selectedCustomer]); // Dependency array includes selectedCustomer to ensure that the effect runs when the selected customer changes

    // Event handlers

    // Event handler: handles the selection of a customer from the list
    function handleSelectCustomer(customerId: number): void {
        setSelectedCustomerId(customerId);
        setSelectedCustomer(null);
        setDetailsError(null);
    }

    // Event handler: creates a new customer
    async function handleCreateCustomer(
        customerData: CreateCustomerRequest
    ): Promise<void> {
        const createdCustomer = await createCustomer(
            customerData
        );
        // Add the new customer to the existing list
        setCustomers((currentCustomers) => [
            createdCustomer,
            ...currentCustomers
        ]);
        // Select the newly created customer
        setSelectedCustomerId(createdCustomer.id);
        setSelectedCustomer(createdCustomer);
        // Clear any previous details loading or errors
        setIsDetailsLoading(false);
        setDetailsError(null);
    }


    return (
        <section className="customers-page">
            <div className="page-intro">
                <div>
                    <p className="eyebrow">
                        CUSTOMERS / OVERVIEW
                    </p>

                    <h1>Customer Management</h1>

                    <p className="intro-description">
                        View your customers and their details.
                    </p>
                </div>

                <div className="summary-card">
                    <span>Total customers</span>
                    <strong>{customers.length}</strong>
                    <small>Registered customers</small>
                </div>
            </div>

            <div className="customers-layout">
                <section
                    className="customer-list-panel"
                    aria-labelledby="customer-list-heading"
                >
                    <div className="section-heading">
                        <div>
                            <h2 id="customer-list-heading">
                                Customers
                            </h2>

                            <p>Select a customer to view details.</p>
                        </div>

                        <div className="customer-list-actions">

                            <span className="count-pill">
                                {customers.length} total
                            </span>

                            <button
                                className="filter-button"
                                type="button"
                                onClick={() =>
                                    setIsCreateFormOpen((currentValue) => !currentValue)
                                }
                            >
                                {isCreateFormOpen
                                    ? "Close form"
                                    : "Add customer"}
                            </button>

                        </div>
                    </div>

                    {isCreateFormOpen && (
                        <CustomerForm
                            onCreateCustomer={handleCreateCustomer}
                        />
                    )}
                    {isLoading ? (
                        <p className="catalog-message">
                            Loading customers...
                        </p>
                    ) : loadError !== null ? (
                        <div className="catalog-error" role="alert">
                            <h3>Could not load customers</h3>
                            <p>{loadError}</p>
                        </div>
                    ) : customers.length === 0 ? (
                        <div className="empty-state">
                            <h3>No customers found</h3>
                            <p>
                                There are currently no registered customers.
                            </p>
                        </div>
                    ) : (
                        <div className="customer-list">
                            {customers.map((customer) => (
                                <button
                                    key={customer.id}
                                    type="button"
                                    className={`customer-list-item ${
                                        selectedCustomerId === customer.id
                                            ? "active"
                                            : ""
                                    }`}
                                    aria-pressed={
                                        selectedCustomerId === customer.id
                                    }
                                    onClick={() =>
                                        handleSelectCustomer(customer.id)
                                    }
                                >
                                    <span className="customer-list-name">
                                        {customer.name}
                                    </span>

                                    <span className="customer-list-email">
                                        {customer.email}
                                    </span>

                                    <span className="customer-list-id">
                                        CUSTOMER #{customer.id}
                                    </span>
                                </button>
                            ))}
                        </div>
                    )}
                </section>

                <section
                    className="customer-details-panel"
                    aria-labelledby="customer-details-heading"
                >
                    <div className="section-heading">
                        <div>
                            <h2 id="customer-details-heading">
                                Customer details
                            </h2>

                            <p>Selected customer information.</p>
                        </div>
                    </div>

                    {selectedCustomerId === null ? (
                        <div className="empty-state">
                            <h3>Select a customer</h3>
                            <p>
                                Choose a customer from the list to view details.
                            </p>
                        </div>
                    ) : isDetailsLoading ? (
                        <p className="catalog-message">
                            Loading customer details...
                        </p>
                    ) : detailsError !== null ? (
                        <div className="catalog-error" role="alert">
                            <h3>Could not load details</h3>
                            <p>{detailsError}</p>
                        </div>
                    ) : selectedCustomer !== null ? (
                        <div className="customer-details">
                            <div className="customer-avatar">
                                {selectedCustomer.name
                                    .charAt(0)
                                    .toUpperCase()}
                            </div>

                            <div className="customer-detail-item">
                                <span>Customer ID</span>
                                <strong>{selectedCustomer.id}</strong>
                            </div>

                            <div className="customer-detail-item">
                                <span>Full name</span>
                                <strong>{selectedCustomer.name}</strong>
                            </div>

                            <div className="customer-detail-item">
                                <span>Email address</span>
                                <strong>{selectedCustomer.email}</strong>
                            </div>
                        </div>
                    ) : null}
                </section>
            </div>
        </section>
    );
}