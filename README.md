# Order Intake & Tracking

Sales reps use this app to record customer purchase orders and track them from Pending through to Fulfilled or Cancelled. An order carries the customer's own reference (their PO number), customer details, a currency and one or more line items; the server calculates the totals. Reps sometimes submit the same order twice, so the API treats the external reference as an idempotency key: an identical resubmission returns the original order, and a different order under a reference that is already taken is rejected with a link to the existing one.

The backend is an ASP.NET Core minimal API on .NET 10 with EF Core and SQLite. The frontend is an Angular 22 app.

## Requirements

| Tool | Version |
| --- | --- |
| .NET SDK | 10.0 or later (all projects target `net10.0`) |
| Node.js | `^22.22.3`, `^24.15.0` or `>=26.0.0` (the range Angular 22 supports) |
| npm | 8 or later (ships with Node) |
| `dotnet-ef` (optional) | Only needed to add new migrations: `dotnet tool install --global dotnet-ef` |

## Project structure

```
backend/
  OrderManagement.slnx
  src/
    OrderManagement.Domain/          Entities, request/response models, validators, status rules, PurchaseOrderManager
    OrderManagement.Infrastructure/  EF Core DbContext, SQLite mappings, migrations, repositories
    OrderManagement.Api/             Minimal API endpoints, ProblemDetails middleware, startup
  tests/
    OrderManagement.Domain.Tests/          Manager, fingerprint, status rules and validators
    OrderManagement.Infrastructure.Tests/  Repositories, timestamps, concurrent submissions (SQLite)
    OrderManagement.Api.Tests/             HTTP-level tests through WebApplicationFactory
frontend/
  proxy.conf.mjs                     Dev proxy: /api -> API_URL (default http://localhost:5224)
  src/app/
    core/                            Theme, HTTP error interceptor, API base URL
    layout/                          App shell, not-found page
    shared/ui/                       Status badge, loading, empty and error states
    features/purchase-orders/        Models, API client, form, list, detail and create pages
```

## Run the backend

```bash
cd backend
dotnet run --project src/OrderManagement.Api --launch-profile http
```

- API: http://localhost:5224/api/purchase-orders
- API reference (Scalar): http://localhost:5224/scalar/v1

The SQLite database (`ordermanagement.db` in `src/OrderManagement.Api`) is created and migrated automatically on first run.

From Visual Studio, the **http** profile matches the frontend's default proxy target. If you use another profile, set `API_URL` for the frontend (below).

## Run the frontend

```bash
cd frontend
npm install
npm start
```

Open http://localhost:4200. The dev server proxies `/api` to the backend, so the backend must be running. The target defaults to http://localhost:5224 (the `http` profile). For another launch profile, set `API_URL` before `npm start`:

| Backend launch profile | `API_URL` |
| --- | --- |
| `http` (default) | not needed |
| `https` | `https://localhost:7069` |
| IIS Express | `https://localhost:44323` |

```powershell
$env:API_URL = "https://localhost:44323"; npm start    # PowerShell
```

```bash
API_URL=https://localhost:44323 npm start              # bash
```

## Run the tests

From the repo root:

```bash
cd backend && dotnet test OrderManagement.slnx
```

```bash
cd frontend && npm test -- --watch=false
```

## API overview

All endpoints are under `/api/purchase-orders`. Errors are returned as ProblemDetails (`application/problem+json`).

| Method and path | Purpose | Responses |
| --- | --- | --- |
| `POST /` | Submit an order | **201** created (Location header set), **200** identical resubmission (returns the existing order), **400** validation errors by field, **409** reference already used by a different order (`existingOrderId`) |
| `GET /?status=&page=&pageSize=` | List orders, newest first | **200** paged result. `page` and `pageSize` are clamped (page ≥ 1, pageSize 1–100, default 20) |
| `GET /{id}` | Get one order | **200**, **404** |
| `PUT /{id}/status` | Change status | **200** (also when the order already has that status), **400** missing or unknown status, **404**, **422** transition not allowed (`allowedTransitions`) |

Allowed transitions: Pending → Confirmed or Cancelled; Confirmed → Fulfilled or Cancelled. Fulfilled and Cancelled are final.

## Trying the duplicate protection

1. In the UI, open **New order**, fill in the form with a reference such as `PO-2001` and submit. You land on the new order with "Order created" (**201**).
2. Open **New order** again and submit exactly the same details. You land on the same order with "This order was already submitted – showing the existing order" (**200**). No second order is created; the reference is matched case-insensitively and surrounding spaces are ignored.
3. Submit once more with the same reference but change a line (for example the quantity). The form stays open with "This reference already belongs to a different order" and an **Open existing order** link (**409**).

The same sequence, plus the 400 and 422 cases, is in [`backend/src/OrderManagement.Api/OrderManagement.Api.http`](backend/src/OrderManagement.Api/OrderManagement.Api.http) for Visual Studio, Rider or the VS Code REST Client.

## Troubleshooting

- **Proxy errors (`ECONNREFUSED`/`ECONNRESET`) in the `npm start` console:** the API isn't running, or `API_URL` doesn't match its launch profile (see the table above). `ECONNRESET` usually means `http://` was used for an HTTPS port. Restart `npm start` after changing `API_URL`.
- **`dotnet run` fails with "file is locked by IIS Express Worker Process":** the API is already running from Visual Studio. Stop it there first.
