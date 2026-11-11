interface SuccessMessageProps {
    message: string;
}

export default function SuccessMessage({
    message
}: SuccessMessageProps) {
    return (
        <p
            className="form-success"
            role="status"
            aria-live="polite"
            aria-atomic="true"
        >
            {message}
        </p>
    );
}