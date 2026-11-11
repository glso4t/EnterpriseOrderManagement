
import "./PageFeedback.css";

type PageFeedbackVariant = "loading" | "error" | "empty";

interface PageFeedbackProps {
    variant: PageFeedbackVariant;   //Ποια κατάσταση εμφανίζουμε
    title: string;
    message: string;
    actionLabel?: string;   //Προαιρετικό κείμενο κουμπιού
    onAction?: () => void;  //Προαιρετική συνάρτηση που εκτελείται στο click
}

function PageFeedback({
    variant,
    title,
    message,
    actionLabel,
    onAction
}: PageFeedbackProps) {

    //Χρησιμοποιούμε semantic HTML accessibility roles, ώστε οι υποστηρικτικές τεχνολογίες, 
    // όπως screen readers, να μπορούν να αντιληφθούν καλύτερα το μήνυμα.
    const role = variant === "error" ? "alert" : "status";

    return (
        <section
            className={`page-feedback page-feedback--${variant}`}
            role={role}
            aria-atomic="true"
        >
            <div className="page-feedback__content">

                {variant === "loading" && ( //Το spinner εμφανίζεται μόνο όταν το variant είναι "loading"
                    <span
                        className="page-feedback__spinner"
                        aria-hidden="true"
                    />
                )}

                <div>
                    <h2>{title}</h2>
                    <p>{message}</p>
                </div>

            </div>
            
            {actionLabel && onAction && variant !== "loading" && (  //Το κουμπί εμφανίζεται μόνο όταν έχουμε δώσει και κείμενο και συνάρτηση. 
                <button                                             //Έτσι μπορούμε, για παράδειγμα, να προσφέρουμε Retry σε περίπτωση αποτυχίας.
                    type="button"
                    className="page-feedback__button"
                    onClick={onAction}
                >
                    {actionLabel}
                </button>
            )}
        </section>
    );
}

export default PageFeedback;
