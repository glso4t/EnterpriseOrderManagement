import { useEffect, useState } from "react";
import { getCustomers } from "../services/customerService";
import { getProducts } from "../services/productService";   
import OrderCard from "../components/OrderCard";
import OrderForm from "../components/OrderForm";

import type {
    Customer,
    Order,
    OrderQueryParams,
    OrderStatus,
    Product,
    CreateOrderRequest
} from "../types";
import {
    getOrders,
    getOrderById,
    createOrder
} from "../services/orderService";



interface OrdersPageProps {
    isActive: boolean;
}

const currencyFormatter = new Intl.NumberFormat(
    "el-GR",
    {
        style: "currency",
        currency: "EUR"
    }
);
const dateFormatter = new Intl.DateTimeFormat(
    "el-GR",
    {
        dateStyle: "medium",
        timeStyle: "short"
    }
);


function OrdersPage({ isActive }: OrdersPageProps) {

    //states

    // Orders data
    const [orders, setOrders] = 
        useState<Order[]>([]);
    const [totalCount, setTotalCount] = 
        useState(0);
    const [totalPages, setTotalPages] = 
        useState(0);


    // Customers lookup data
    const [customers, setCustomers] = 
        useState<Customer[]>([]);
    const [customersError, setCustomersError] =
        useState<string | null>(null);

    // Products lookup data
    const [products, setProducts] = useState<Product[]>([]);
    const [hasLoadedProducts, setHasLoadedProducts] =
        useState(false);
    const [productsError, setProductsError] =
        useState<string | null>(null);


    // Loading and error states
    const [isLoading, setIsLoading] = 
        useState(true);
    const [hasLoadedOrders, setHasLoadedOrders] = 
        useState(false);
    const [loadError, setLoadError] = 
        useState<string | null>(null);

        // Customers loading state
    const [hasLoadedCustomers, setHasLoadedCustomers] =
        useState(false);


    // Pagination + filters
    const [page, setPage] = 
        useState(1);
    const [pageSize, setPageSize] = 
        useState(10);
    const [customerFilter, setCustomerFilter] = 
        useState("");
    const [statusFilter, setStatusFilter] =
        useState<OrderStatus | "All">("All");   // Default to "All" to show all orders regardless of status (Το "All" είναι επιλογή του frontend. Δεν θα το στείλουμε στο backend ως status filter.)
    const [fromDate, setFromDate] = 
        useState("");
    const [toDate, setToDate] = 
        useState("");
    const [sortDirection, setSortDirection] =
        useState<"asc" | "desc">("desc");

    // Selected order details
    const [selectedOrderId, setSelectedOrderId] =   //αναγνωρίζει ποια παραγγελία επέλεξε ο χρήστης
        useState<number | null>(null);
    const [selectedOrder, setSelectedOrder] =   //αποθηκεύει τα πλήρη στοιχεία που επιστρέφει το getOrderById()
        useState<Order | null>(null);
    const [isDetailsLoading, setIsDetailsLoading] =
        useState(false);
    const [detailsError, setDetailsError] =
        useState<string | null>(null);


        // Refresh triggers (Αποθηκεύουμε έναν αριθμό που θα λειτουργεί ως trigger για το αντίστοιχο useEffect. 
        // Αν ο αριθμός αλλάξει από 0 σε 1, το effect θα εκτελεστεί ξανά, επειδή έχει αλλάξει dependency.)
    const [productsRefreshKey, setProductsRefreshKey] =
        useState(0);
    const [customersRefreshKey, setCustomersRefreshKey] =
        useState(0);
    const [ordersRefreshKey, setOrdersRefreshKey] =
        useState(0);


    // Create Order state
    const [isCreateFormOpen, setIsCreateFormOpen] =
        useState(false);
    const [isCreatingOrder, setIsCreatingOrder] =
        useState(false);
    const [createOrderSuccess, setCreateOrderSuccess] =
        useState<string | null>(null);



    // Event handlers

    // Προετοιμασία πριν από νέο GET. Όταν αλλάζει κάποιο φίλτρο ή η σελίδα, καθαρίζουμε την προηγούμενη λίστα και τα details.
    function prepareForOrdersReload(): void {
        setIsLoading(true);
        setOrders([]);
        setHasLoadedOrders(false);
        setTotalCount(0);
        setTotalPages(0);
        setSelectedOrderId(null);
        setSelectedOrder(null);
        setIsDetailsLoading(false);
        setDetailsError(null);
    }

    // Event handler for changing the customer filter
    function handleCustomerFilterChange(
        value: string
    ): void {
        prepareForOrdersReload();
        setCustomerFilter(value);
        setPage(1);
    }

    // Event handler for changing the date range filter (fromDate and toDate)
    function handleFromDateChange(value: string): void {
        prepareForOrdersReload();
        setFromDate(value);
        setPage(1);
    }
    function handleToDateChange(value: string): void {
        prepareForOrdersReload();
        setToDate(value);
        setPage(1);
    }

    // Event handler for changing the sort direction filter
    function handleSortDirectionChange(
        value: string
    ): void {
        prepareForOrdersReload();
        setSortDirection(value as "asc" | "desc");
        setPage(1);
    }

    // Event handler for changing the status filter
    function handlePageSizeChange(value: number): void {
        prepareForOrdersReload();
        setPageSize(value);
        setPage(1);
    }

    // Event handler for changing the page filter
    function handlePageChange(nextPage: number): void {
        if (
            nextPage < 1 ||
            nextPage > totalPages ||
            nextPage === page
        ) {
            return;
        }
        prepareForOrdersReload();
        setPage(nextPage);
    }

    // Event handler for selecting an order. 
    // Το selectedOrderId αλλάζει και ενεργοποιεί το αντίστοιχο useEffect. 
    // Αυτός με τη σειρά του εκτελεί getOrderById()
    function handleSelectOrder(orderId: number): void {
        // Do not reload an already selected order
        if (selectedOrderId === orderId) {
            return;
        }
        setSelectedOrderId(orderId);
        setSelectedOrder(null);
        setDetailsError(null);
        setIsDetailsLoading(true);
    }

    // Event handler for changing the status filter
    function handleStatusFilterChange(
        value: string
    ): void {
        prepareForOrdersReload();
        setStatusFilter(value as OrderStatus | "All");
        setPage(1);
    }

    // Event handler: creates a new order
    async function handleCreateOrder(
        request: CreateOrderRequest
    ): Promise<Order> {
        setIsCreatingOrder(true);
        try {
            const createdOrder = await createOrder(request);            // Create the order through the backend
            setCreateOrderSuccess(
                `Order #${createdOrder.id} was created successfully.`
            );
            // Reset filters so the new Pending order is visible
            setCustomerFilter("");
            setStatusFilter("All");
            setFromDate("");
            setToDate("");
            setSortDirection("desc");
            setPage(1);
            // Trigger an Orders refresh
            setIsLoading(true);
            setHasLoadedOrders(false);
            setLoadError(null);
            setOrdersRefreshKey(
                (currentValue) => currentValue + 1
            );
            // Select the newly created order
            setSelectedOrderId(createdOrder.id);
            setSelectedOrder(null);
            setDetailsError(null);
            setIsDetailsLoading(true);
            // Refresh products because their stock has changed
            setProductsError(null);
            setProductsRefreshKey(
                (currentValue) => currentValue + 1
            );
            return createdOrder;
        } catch (error: unknown) {
            // Refresh Products even after a failed request,
            // because stock may have changed due to another user.
            setProductsRefreshKey(
                (currentValue) => currentValue + 1
            );
            throw error;
        } finally {
            setIsCreatingOrder(false);
        }
    }

    // Event handler: resets all order filters
    function handleClearFilters(): void {
        const hasActiveFilters =
            customerFilter !== "" ||
            statusFilter !== "All" ||
            fromDate !== "" ||
            toDate !== "" ||
            sortDirection !== "desc" ||
            page !== 1 ||
            pageSize !== 10;
        if (!hasActiveFilters) {
            return;
        }
        prepareForOrdersReload();
        setCustomerFilter("");
        setStatusFilter("All");
        setFromDate("");
        setToDate("");
        setSortDirection("desc");
        setPage(1);
        setPageSize(10);
    }




    // Effects 
    
    // Effect: load orders whenever the page is active
    // or pagination and filter values change
    useEffect(() => {
        if (!isActive) {    // If the page is not active, we don't want to load orders
            return;
        }
        let ignore = false;
        async function loadOrders(): Promise<void> {    // Load orders (function) from the API based on current filters and pagination
            const filters: OrderQueryParams = {
                page,
                pageSize,
                sortBy: "orderDate",
                sortDirection
            };
            if (customerFilter !== "") {    // If a customer filter is applied, add it to the filters object
                filters.customerId = Number(customerFilter);
            }
            if (statusFilter !== "All") {   
                filters.status = statusFilter;
            }
            if (fromDate !== "") {  // If a fromDate filter is applied, add it to the filters object
                filters.fromDate = fromDate;
            }
            if (toDate !== "") {
                filters.toDate = toDate;
            }
            try {   // Fetch orders from the API using the current filters
                const result = await getOrders(filters);
                if (!ignore) {  //ενημέρωση του state μόνο αν το component είναι ακόμα mounted (για να αποφύγουμε memory leaks)
                    setOrders(result.items);
                    setPage(result.page);
                    setTotalCount(result.totalCount);
                    setTotalPages(result.totalPages);
                    setLoadError(null);
                    setHasLoadedOrders(true);
                }
            } catch (error: unknown) {
                if (!ignore) {
                    const message =
                        error instanceof Error
                            ? error.message
                            : "An unexpected error occurred.";
                    setLoadError(message);
                }
            } finally { // Set loading state to false after the API call is complete
                if (!ignore) {
                    setIsLoading(false);
                }
            }
        }
        loadOrders();   // Call the loadOrders function to fetch orders from the API
        return () => {  // Cleanup function to set ignore to true when the component unmounts or dependencies change, preventing state updates on an unmounted component
            ignore = true;
        };
    }, [
        isActive,
        page,
        pageSize,
        customerFilter,
        statusFilter,
        fromDate,
        toDate,
        sortDirection,
        ordersRefreshKey
    ]);

    // Effect: load customers for the order customer lookup
    //**Όταν ανοίγουμε πρώτη φορά το Orders, φορτώνονται οι Customers.
    //**Όταν επιστρέφουμε στο Orders μετά από άλλη σελίδα, φορτώνονται ξανά.
    //**Όταν αλλάζουμε page ή status filter, δεν χρειάζεται να ξαναφορτώσουμε Customers.
    useEffect(() => {
        if (!isActive) {
            return;
        }
        let ignore = false;
        async function loadCustomers(): Promise<void> {
            try {
                const loadedCustomers = await getCustomers();
                if (!ignore) {
                    setCustomers(loadedCustomers);
                    setCustomersError(null);
                    setHasLoadedCustomers(true);
                }
            } catch (error: unknown) {
                if (!ignore) {
                    const message =
                        error instanceof Error
                            ? error.message
                            : "Could not load customers.";
                    setCustomersError(message);
                    setHasLoadedCustomers(true);
                }
            }
        }
        loadCustomers();
        return () => {
            ignore = true;
        };
    }, [isActive, customersRefreshKey]);

    // Effect: load products needed for order creation
    useEffect(() => {
        if (!isActive) {
            return;
        }
        let ignore = false;
        async function loadProducts(): Promise<void> {
            try {
                const loadedProducts = await getProducts();
                if (!ignore) {
                    setProducts(loadedProducts);
                    setProductsError(null);
                    setHasLoadedProducts(true);
                }
            } catch (error: unknown) {
                if (!ignore) {
                    const message =
                        error instanceof Error
                            ? error.message
                            : "Could not load products.";
                    setProductsError(message);
                    setHasLoadedProducts(true);
                }
            }
        }
        loadProducts();
        return () => {
            ignore = true;
        };
    }, [isActive, productsRefreshKey]);

    // Effect: load the details of the selected order
    useEffect(() => {
        if (!isActive || selectedOrderId === null) {
            return;
        }
        const orderId = selectedOrderId;
        let ignore = false;
        async function loadOrderDetails(): Promise<void> {
            try {
                const order = await getOrderById(orderId); // Χρησιμοποιούμε το ασφαλές orderIdif (!ignore) {
                if (!ignore) {
                    setSelectedOrder(order);
                    setDetailsError(null);
                }
            } catch (error: unknown) {
                if (!ignore) {
                    const message =
                        error instanceof Error
                            ? error.message
                            : "Could not load order details.";
                    setDetailsError(message);
                }
            } finally {
                if (!ignore) {
                    setIsDetailsLoading(false);
                }
            }
        }
        loadOrderDetails();
        return () => {
            ignore = true;
        };
    }, [isActive, selectedOrderId]);

    return (

        <div className="orders-page">

            {/* Page Header */}

            <section className="orders-page-header">

                <div>
                    <p className="eyebrow">
                        SALES / MANAGEMENT
                    </p>

                    <h1>Order Management</h1>

                    <p className="intro-description">
                        View, search and manage customer orders.
                    </p>
                </div>


                <div className="orders-summary-card">

                    <span>Total orders</span>

                    <strong>{totalCount}</strong>

                    <small>
                        Page {page} of {Math.max(totalPages, 1)}
                    </small>

                </div>

                <button
                    className="submit-button"
                    type="button"
                    onClick={() => {
                        if (isCreateFormOpen) {
                            setIsCreateFormOpen(false);
                        } else {
                            setCreateOrderSuccess(null);
                            setIsCreateFormOpen(true);
                        }
                    }}
                >
                    {isCreateFormOpen
                        ? "Close form"
                        : "Create order"}
                </button>
            </section>

            {createOrderSuccess !== null && (

                <p className="form-success" role="status">
                    {createOrderSuccess}
                </p>

            )}

            {isCreateFormOpen && (

                <div className="orders-create-section">

                    {!hasLoadedCustomers || !hasLoadedProducts ? (

                        <p className="catalog-message">
                            Loading customers and products...
                        </p>

                    ) : customersError !== null || productsError !== null ? (

                        <div className="catalog-error" role="alert">

                            <h3>Could not load order form data</h3>

                            {customersError !== null && (
                                <p>{customersError}</p>
                            )}

                            {productsError !== null && (
                                <p>{productsError}</p>
                            )}


                            <button
                                className="filter-button"
                                type="button"
                                onClick={() => {

                                    setCustomersError(null);
                                    setProductsError(null);

                                    setHasLoadedCustomers(false);
                                    setHasLoadedProducts(false);

                                    setCustomersRefreshKey(
                                        (currentValue) => currentValue + 1
                                    );

                                    setProductsRefreshKey(
                                        (currentValue) => currentValue + 1
                                    );
                                }}
                            >
                                Retry
                            </button>

                        </div>

                    ) : customers.length === 0 ? (

                        <div className="empty-state">

                            <h3>No customers available</h3>

                            <p>
                                Create a customer before creating an order.
                            </p>

                        </div>

                    ) : products.length === 0 ? (

                        <div className="empty-state">

                            <h3>No products available</h3>

                            <p>
                                Create products before creating an order.
                            </p>

                        </div>

                    ) : (

                        <OrderForm
                            customers={customers}
                            products={products}
                            onCreateOrder={handleCreateOrder}
                            onCancel={() => setIsCreateFormOpen(false)}
                            isSubmitting={isCreatingOrder}
                        />

                    )}

                </div>

            )}

            {/* Filters */}

            <section
                className="orders-filters"
                aria-label="Order filters"
            >

                <div className="section-heading">

                    <div>
                        <h2>Filters</h2>

                        <p>
                            Narrow down the orders displayed below.
                        </p>
                    </div>

                    <button
                        className="filter-button"
                        type="button"
                        onClick={handleClearFilters}
                    >
                        Clear filters
                    </button>

                </div>


                <div className="orders-filter-grid">


                    {/* Customer filter */}

                    <div className="form-field">

                        <label htmlFor="order-customer-filter">
                            Customer
                        </label>

                        <select
                            id="order-customer-filter"
                            value={customerFilter}
                            onChange={(event) =>
                                handleCustomerFilterChange(
                                    event.target.value
                                )
                            }
                        >

                            <option value="">
                                All customers
                            </option>

                            {customers.map((customer) => (

                                <option
                                    key={customer.id}
                                    value={customer.id}
                                >
                                    {customer.name}
                                </option>

                            ))}

                        </select>

                    </div>


                    {/* Status filter */}

                    <div className="form-field">

                        <label htmlFor="order-status-filter">
                            Status
                        </label>

                        <select
                            id="order-status-filter"
                            value={statusFilter}
                            onChange={(event) =>
                                handleStatusFilterChange(
                                    event.target.value
                                )
                            }
                        >

                            <option value="All">
                                All statuses
                            </option>

                            <option value="Pending">
                                Pending
                            </option>

                            <option value="Confirmed">
                                Confirmed
                            </option>

                            <option value="Cancelled">
                                Cancelled
                            </option>

                        </select>

                    </div>


                    {/* From date */}

                    <div className="form-field">

                        <label htmlFor="order-from-date">
                            From date
                        </label>

                        <input
                            id="order-from-date"
                            type="date"
                            value={fromDate}
                            max={toDate || undefined}
                            onChange={(event) =>
                                handleFromDateChange(
                                    event.target.value
                                )
                            }
                        />

                    </div>


                    {/* To date */}

                    <div className="form-field">

                        <label htmlFor="order-to-date">
                            To date
                        </label>

                        <input
                            id="order-to-date"
                            type="date"
                            value={toDate}
                            min={fromDate || undefined}
                            onChange={(event) =>
                                handleToDateChange(
                                    event.target.value
                                )
                            }
                        />

                    </div>


                    {/* Sort direction */}

                    <div className="form-field">

                        <label htmlFor="order-sort">
                            Sort by date
                        </label>

                        <select
                            id="order-sort"
                            value={sortDirection}
                            onChange={(event) =>
                                handleSortDirectionChange(
                                    event.target.value
                                )
                            }
                        >

                            <option value="desc">
                                Newest first
                            </option>

                            <option value="asc">
                                Oldest first
                            </option>

                        </select>

                    </div>


                    {/* Page size */}

                    <div className="form-field">

                        <label htmlFor="order-page-size">
                            Orders per page
                        </label>

                        <select
                            id="order-page-size"
                            value={pageSize}
                            onChange={(event) =>
                                handlePageSizeChange(
                                    Number(event.target.value)
                                )
                            }
                        >

                            <option value={10}>10</option>
                            <option value={25}>25</option>
                            <option value={50}>50</option>
                            <option value={100}>100</option>

                        </select>

                    </div>

                </div>

            </section>


            {/* Errors */}

            {customersError !== null && (

                <div className="catalog-error" role="alert">

                    <p>Could not load customer information.</p>
                    <p>{customersError}</p>

                </div>

            )}


            {loadError !== null && (

                <div className="catalog-error" role="alert">

                    <p>Could not load orders.</p>
                    <p>{loadError}</p>

                </div>

            )}


            {/* Orders Content */}

            {isLoading && !hasLoadedOrders ? (

                <p className="catalog-message">
                    Loading orders...
                </p>

            ) : loadError !== null && !hasLoadedOrders ? (

                <div className="empty-state">

                    <h3>Orders could not be loaded</h3>

                    <p>
                        Check your connection and try again.
                    </p>

                </div>

            ) : orders.length === 0 ? (

                <div className="empty-state">

                    <h3>No orders found</h3>

                    <p>
                        There are no orders matching the selected filters.
                    </p>

                </div>

            ) : (

                <section className="orders-section">

                    <div className="orders-section-heading">

                        <div>
                            <h2>Orders</h2>

                            <p>
                                {totalCount} order(s) found.
                            </p>
                        </div>

                        {isLoading && (
                            <span className="refreshing-message">
                                Updating orders...
                            </span>
                        )}

                    </div>


                    <div className="orders-layout">


                        {/* Orders List */}

                        <div className="orders-list">

                            {orders.map((order) => {

                                const customerName =
                                    customers.find(
                                        (customer) =>
                                            customer.id === order.customerId
                                    )?.name ??
                                    `Customer #${order.customerId}`;


                                return (

                                    <OrderCard
                                        key={order.id}
                                        order={order}
                                        customerName={customerName}
                                        isSelected={
                                            selectedOrderId === order.id
                                        }
                                        onSelect={handleSelectOrder}
                                    />

                                );
                            })}

                        </div>


                        {/* Order Details */}

                        <div className="order-details-panel">

                            {selectedOrderId === null ? (

                                <div className="order-details-empty">

                                    <h3>Order details</h3>

                                    <p>
                                        Select an order to view its details.
                                    </p>

                                </div>

                            ) : isDetailsLoading ? (

                                <p className="catalog-message">
                                    Loading order details...
                                </p>

                            ) : detailsError !== null ? (

                                <div className="catalog-error" role="alert">

                                    <h3>Could not load order details</h3>

                                    <p>{detailsError}</p>

                                </div>

                            ) : selectedOrder !== null ? (

                                <div className="order-details">

                                    <div className="order-details-header">

                                        <div>
                                            <p className="eyebrow">
                                                ORDER DETAILS
                                            </p>

                                            <h2>
                                                Order #{selectedOrder.id}
                                            </h2>
                                        </div>

                                        <span
                                            className={`order-status ${
                                                selectedOrder.status.toLowerCase()
                                            }`}
                                        >
                                            {selectedOrder.status}
                                        </span>

                                    </div>


                                    <div className="order-details-meta">

                                        <div>
                                            <span>Customer</span>

                                            <strong>
                                                {customers.find(
                                                    (customer) =>
                                                        customer.id ===
                                                        selectedOrder.customerId
                                                )?.name ??
                                                `Customer #${selectedOrder.customerId}`}
                                            </strong>
                                        </div>


                                        <div>
                                            <span>Order date</span>

                                            <strong>
                                                {dateFormatter.format(
                                                    new Date(
                                                        selectedOrder.orderDate
                                                    )
                                                )}
                                            </strong>
                                        </div>


                                        <div>
                                            <span>Customer ID</span>

                                            <strong>
                                                {selectedOrder.customerId}
                                            </strong>
                                        </div>

                                    </div>


                                    <h3>Order items</h3>


                                    <div className="order-detail-items">

                                        {selectedOrder.items.map((item) => (

                                            <div
                                                className="order-detail-item"
                                                key={item.productId}
                                            >

                                                <div className="order-item-info">

                                                    <strong>
                                                        {item.productName}
                                                    </strong>

                                                    <span>
                                                        {item.quantity} ×{" "}
                                                        {currencyFormatter.format(
                                                            item.unitPrice
                                                        )}
                                                    </span>

                                                </div>

                                                <strong>
                                                    {currencyFormatter.format(
                                                        item.lineTotal
                                                    )}
                                                </strong>

                                            </div>

                                        ))}

                                    </div>


                                    <div className="order-details-total">

                                        <span>Total Amount</span>

                                        <strong>
                                            {currencyFormatter.format(
                                                selectedOrder.totalAmount
                                            )}
                                        </strong>

                                    </div>

                                </div>

                            ) : null}

                        </div>

                    </div>


                    {/* Pagination */}

                    <div className="orders-pagination">

                        <span>
                            Page {page} of {Math.max(totalPages, 1)}
                            {" · "}
                            {totalCount} total orders
                        </span>


                        <div>

                            <button
                                className="filter-button"
                                type="button"
                                onClick={() =>
                                    handlePageChange(page - 1)
                                }
                                disabled={page <= 1 || isLoading}
                            >
                                Previous
                            </button>


                            <button
                                className="filter-button"
                                type="button"
                                onClick={() =>
                                    handlePageChange(page + 1)
                                }
                                disabled={
                                    page >= totalPages || isLoading
                                }
                            >
                                Next
                            </button>

                        </div>

                    </div>

                </section>

            )}

        </div>
    );
}

export default OrdersPage;

