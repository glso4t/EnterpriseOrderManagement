import { useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router";

import { useAuth } from "../auth/useAuth";

export default function LoginPage() {

    const [token, setToken] = 
        useState("");
    const [error, setError] = 
        useState<string | null>(null);

    const { login } = useAuth();
    const navigate = useNavigate();

    function handleLogin(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        try {
            login(token.trim());
            navigate("/products", { replace: true });
        } catch {
            setError("The token is invalid or expired.");
        }
    }

    return (
        <main className="page-content">
            <section>
                <h1>Development Login</h1>

                <p>
                    Paste a development JWT to access the application.
                </p>

                <form onSubmit={handleLogin}>
                    <label htmlFor="jwt-token">
                        JWT Token
                    </label>

                    <textarea
                        id="jwt-token"
                        value={token}
                        onChange={(event) => setToken(event.target.value)}
                        rows={5}
                        required
                    />

                    {error && (
                        <p role="alert">
                            {error}
                        </p>
                    )}

                    <button type="submit">
                        Sign In
                    </button>
                </form>
            </section>
        </main>
    );
}