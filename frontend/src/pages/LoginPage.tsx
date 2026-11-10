
import { useState } from "react";
import type { FormEvent } from "react";

import {
    Link,
    Navigate,
    useNavigate
} from "react-router";

import { useAuth } from "../auth/useAuth";
import { loginUser } from "../services/authService";

export default function LoginPage() {

    const [email, setEmail] = 
        useState("");
    const [password, setPassword] = 
        useState("");

    const [error, setError] = 
        useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = 
        useState(false);

    const { login, isAuthenticated } = 
        useAuth();

    const navigate = 
        useNavigate();

    async function handleSubmit(
        event: FormEvent<HTMLFormElement>
    ): Promise<void> {

        event.preventDefault();
        setError(null);
        setIsSubmitting(true);
        try {
            const response = await loginUser({
                email,
                password
            });
            login(response.accessToken);
            navigate("/products", {
                replace: true
            });
        } catch (error) {
            if (error instanceof Error) {
                setError(error.message);
            } else {
                setError("Login failed. Please try again.");
            }
        } finally {
            setIsSubmitting(false);
        }
    }
    if (isAuthenticated) {
        return <Navigate to="/products" replace />;
    }

    return (
        <div className="auth-page">

            <section className="auth-card">

                <h1>Sign In</h1>

                <p>
                    Enter your credentials to access
                    Enterprise Order Management.
                </p>

                <form
                    className="auth-form"
                    onSubmit={handleSubmit}
                >

                    <div className="auth-form-group">

                        <label htmlFor="login-email">
                            Email
                        </label>

                        <input
                            id="login-email"
                            type="email"
                            value={email}
                            onChange={(event) =>
                                setEmail(event.target.value)
                            }
                            autoComplete="email"
                            required
                        />

                    </div>

                    <div className="auth-form-group">

                        <label htmlFor="login-password">
                            Password
                        </label>

                        <input
                            id="login-password"
                            type="password"
                            value={password}
                            onChange={(event) =>
                                setPassword(event.target.value)
                            }
                            autoComplete="current-password"
                            required
                        />

                    </div>

                    {error && (
                        <p
                            className="auth-error"
                            role="alert"
                        >
                            {error}
                        </p>
                    )}

                    <button
                        type="submit"
                        disabled={isSubmitting}
                    >
                        {isSubmitting ? "Signing in..." : "Sign In"}
                    </button>

                </form>

                <p className="auth-footer-text">
                    Don't have an account?{" "}
                    <Link to="/register">
                        Create an account
                    </Link>
                </p>

            </section>

        </div>
    );
}
