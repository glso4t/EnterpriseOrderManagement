# EnterpriseOrderManagement

A distributed Enterprise Order Management System built with a multi-layered ASP.NET Core backend and a React frontend. Asynchronous notifications (Outbox, RabbitMQ, Node.js service) are being added incrementally.

## Structure

- `EnterpriseOrderManagement.Api` - ASP.NET Core 10 REST API (EF Core, SQL Server, Identity, JWT)
- `EnterpriseOrderManagement.Tests` - xUnit unit and integration tests
- `frontend` - React 19 + TypeScript + Vite

## Backend

Run from the solution folder:

    dotnet build
    dotnet test

## Frontend

Copy `frontend/.env.example` to `frontend/.env.local` and set `VITE_API_BASE_URL`. Then, from `frontend`:

    npm install
    npm run dev
    npm run lint
    npm run build
    npm run runtime:info