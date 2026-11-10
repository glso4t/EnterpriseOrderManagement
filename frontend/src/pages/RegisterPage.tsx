
import { useState } from "react";
import type { FormEvent } from "react";

import { Link, useNavigate } from "react-router";

import { registerUser } from "../services/authService";

export default function RegisterPage() {

    const [email, setEmail] = 
        useState("");
    const [password, setPassword] = 
        useState("");
    const [confirmPassword, setConfirmPassword] = 
        useState("");

    const [error, setError] = 
        useState<string | null>(null);
    const [success, setSuccess] = 
        useState<string | null>(null);

    const [isSubmitting, setIsSubmitting] = 
        useState(false);

    const navigate = 
        useNavigate();

    async function handleSubmit(
        event: FormEvent<HTMLFormElement>
    ): Promise<void> {

        event.preventDefault();
        setError(null);
        setSuccess(null);
        if (password !== confirmPassword) {
            setError("Passwords do not match.");
            return;
        }
        if (password.length < 8) {
            setError("Password must contain at least 8 characters.");
            return;
        }
        setIsSubmitting(true);
        try {
            const response = await registerUser({
                email,
                password
            });
            setSuccess(response.message);
            setEmail("");
            setPassword("");
            setConfirmPassword("");
        } catch (error) {
            if (error instanceof Error) {
                setError(error.message);
            } else {
                setError("Registration failed. Please try again.");
            }
        } finally {
            setIsSubmitting(false);
        }
    }
    

    return (
        <div className="auth-page">

            <section className="auth-card">

                <h1>Create Account</h1>

                <p>
                    Register a new account.
                </p>

                <form
                    className="auth-form"
                    onSubmit={handleSubmit}
                >

                    <div className="auth-form-group">

                        <label htmlFor="register-email">
                            Email
                        </label>

                        <input
                            id="register-email"
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

                        <label htmlFor="register-password">
                            Password
                        </label>

                        <input
                            id="register-password"
                            type="password"
                            value={password}
                            onChange={(event) =>
                                setPassword(event.target.value)
                            }
                            autoComplete="new-password"
                            minLength={8}
                            required
                        />

                    </div>

                    <div className="auth-form-group">

                        <label htmlFor="confirm-password">
                            Confirm Password
                        </label>

                        <input
                            id="confirm-password"
                            type="password"
                            value={confirmPassword}
                            onChange={(event) =>
                                setConfirmPassword(event.target.value)
                            }
                            autoComplete="new-password"
                            minLength={8}
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

                    {success && (
                        <div
                            className="auth-success"
                            role="status"
                        >
                            <p>{success}</p>

                            <button
                                type="button"
                                onClick={() => navigate("/login")}
                            >
                                Go to Sign In
                            </button>
                        </div>
                    )}

                    {!success && (
                        <button
                            type="submit"
                            disabled={isSubmitting}
                        >
                            {isSubmitting ? "Creating account..." : "Register"}
                        </button>
                    )}

                </form>

                <p className="auth-footer-text">
                    Already have an account?{" "}
                    <Link to="/login">
                        Sign In
                    </Link>
                </p>

            </section>

        </div>
    );
}
