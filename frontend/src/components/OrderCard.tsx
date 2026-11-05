import type { Order } from "../types";

interface OrderCardProps {
    order: Order;
    customerName: string;
    isSelected: boolean;
    onSelect: (orderId: number) => void;
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


export default function OrderCard({
    order,
    customerName,
    isSelected,
    onSelect
}: OrderCardProps) {

    const formattedDate = dateFormatter.format(
        new Date(order.orderDate)
    );
    const statusClass = order.status.toLowerCase();
    return (

        <button
            type="button"
            className={
                isSelected
                    ? "order-card selected"
                    : "order-card"
            }
            onClick={() => onSelect(order.id)}
            aria-pressed={isSelected}
        >

            <div className="order-card-header">

                <strong>
                    Order #{order.id}
                </strong>

                <span
                    className={`order-status ${statusClass}`}
                >
                    {order.status}
                </span>

            </div>


            <div className="order-card-customer">

                <span className="order-customer-label">
                    Customer
                </span>

                <strong>{customerName}</strong>

            </div>


            <div className="order-card-footer">

                <span>
                    {formattedDate}
                </span>

                <strong>
                    {currencyFormatter.format(
                        order.totalAmount
                    )}
                </strong>

            </div>


            <p className="order-card-item-count">
                {order.items.length} product line(s)
            </p>

        </button>

    );
}