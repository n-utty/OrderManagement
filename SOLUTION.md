# Solution

This document describes what was built and why. For setup and running, see [README.md](README.md).

## Architecture

### Backend

Three projects, with dependencies pointing inwards: Api → Infrastructure → Domain.

| Project | Owns |
| --- | --- |
| **Domain** | Entities (`PurchaseOrder`, `LineItem`), request/response models, FluentValidation validators, the status transition table, the reference normalisation and order fingerprint, repository interfaces, domain exceptions, and `PurchaseOrderManager`, which runs each use case (submit, get, list, change status). No EF Core or ASP.NET dependencies. |
| **Infrastructure** | `AppDbContext`, entity mappings (column lengths come from the validator constants), code-first migrations, a UTC `DateTime` converter, a `SaveChanges` interceptor that stamps `CreatedAt`/`UpdatedAt`, and the repositories. |
| **Api** | Minimal API endpoints (thin: bind, call the manager, map the result to a status code), the exception-to-ProblemDetails middleware, JSON enum-as-string configuration, CORS, OpenAPI with Scalar, and applying migrations at startup. |

Tests mirror the layers: Domain tests run the manager against a fake repository, Infrastructure tests use real SQLite (including a concurrent-submission test), and Api tests go through `WebApplicationFactory`.

### Frontend

| Area | Contents |
| --- | --- |
| `core/` | Theme service (light/dark, saved choice or system preference), an HTTP interceptor that turns every failed call into a typed `ApiError` (status, title, detail, field errors, `existingOrderId`, `allowedTransitions`), the API base URL. |
| `layout/` | App shell (header, nav, theme toggle) and the not-found page. |
| `shared/ui/` | Small presentational components: status badge, loading, empty and error states. |
| `features/purchase-orders/` | Models matching the API DTOs, `PurchaseOrdersApi`, the order form and validators, the line-items editor, and three routed pages: list, detail, create. |

Each page holds its own state in signals: server data through `rxResource`, local UI state in `signal`/`computed`. There is no global store. The list keeps its status filter and page in the URL, so refresh and the back button keep them. Pages pass one-off context to the detail page through router navigation state: the list's filter (for "Back to orders") and the result banner after a submit.

## Duplicate protection - Idempotency

This is the core of the brief: a rep resubmitting an order must not create a second one.

| Mechanism | Detail |
| --- | --- |
| **Idempotency key** | The customer's external reference, normalised (trimmed, upper case) before it is validated, stored and looked up, so `po-1001` and ` PO-1001` are the same order. |
| **Fingerprint** | A SHA-256 hash of the order's content: customer fields, currency, notes and line items. Values are normalised (trimmed; email lower case; SKU upper case; prices without trailing zeros) and lines are sorted by SKU, so line order and formatting don't count as a difference. The reference itself is left out; it is the key, not the content. |
| **Replay or conflict** | If the reference exists and the fingerprint matches, the API returns **200** with the existing order. If it doesn't match, it returns **409** with `existingOrderId`. A new reference returns **201**. |
| **Race between two submits** | A unique index on `ExternalReference` is the final guard. If two identical requests pass the lookup at the same time, the second insert fails on that index; the repository catches only that specific constraint violation, clears the failed insert, re-reads the winning order and applies the same replay-or-conflict rule. Covered by `ConcurrentSubmissionTests`, which fires parallel submissions and asserts one order and consistent responses. |
| **UI** | The submit button is disabled and reads "Submitting…" while the request is in flight; the form fields are disabled too, and a second submit (for example Enter in a field) is ignored. 201 and 200 both open the order with a banner that says which case it was; 409 keeps the form open with the server's message and an "Open existing order" link; a network failure keeps the input and says a retry is safe. |

## Status rules

| From | Allowed to |
| --- | --- |
| Pending | Confirmed, Cancelled |
| Confirmed | Fulfilled, Cancelled |
| Fulfilled | (final) |
| Cancelled | (final) |

- One table (`OrderStatusTransitions`) is the single source of truth. The manager enforces it, and every order response includes `allowedTransitions`, so the UI only offers buttons the server will accept.
- Requesting the status an order already has returns **200** and changes nothing, so double clicks and retries are harmless.
- A disallowed move returns **422** with the current status, the requested status and `allowedTransitions`. The detail page shows the message inline and reloads the order.
- Cancelling asks for an inline confirmation in the UI.

## Validation and errors

- **Server:** each request type has a FluentValidation validator (lengths, the reference character set, email, the currency allow-list, 1–100 lines, quantity ≥ 1, unit price from 0 to 1,000,000.00 with at most 2 decimals, no duplicate SKUs). The price cap keeps line totals well inside `decimal` and the `(18,2)` column, so an extreme value is a 400 instead of an overflow. The manager calls them before doing any work.
- **Exceptions to responses:** the domain throws `NotFoundException`, `ConflictException` (carries `existingOrderId`) and `BusinessRuleException` (carries structured details). One middleware maps these, validation failures and unreadable requests to ProblemDetails: 400 (with errors grouped by field), 404, 409, 422. Anything else becomes a 500 with no internal detail; the exception is logged.
- **Client:** the form's validators mirror the server's rules and limits for immediate feedback. Errors show under each field once it has been touched or a submit was attempted. Server field errors from a 400 (for example `LineItems[0].Quantity`) are mapped back onto the matching controls; anything without a matching field goes into the summary at the top. The server remains the authority.
- **Totals** are calculated only on the server. The create form shows a live estimate, labelled "Estimate – final totals are calculated by the server".

## Key decisions and trade-offs

| Decision | Why | Trade-off |
| --- | --- | --- |
| Service-centric `PurchaseOrderManager` instead of a rich domain model | The behaviour is a handful of use cases around one aggregate; one class that reads top to bottom is easier to review than logic spread across entity methods. | Invariants are enforced in the manager, not by the entity itself. A larger domain would move them onto the entity. |
| SQLite with EF Core code-first migrations, applied at startup | Zero setup for reviewers: no server to install, and the database file is created on first run. | Single-writer database; migrating at startup isn't how you'd deploy to production. |
| Timestamps only (`CreatedAt`, `UpdatedAt`), no user audit | The brief asks for timestamps, and there is no authentication to attribute changes to. | No record of who changed an order or a status history. |
| Small generic repository (`IRepository<T>`) plus a specific `IPurchaseOrderRepository` | Keeps EF Core out of the Domain and makes the manager easy to test with a fake. The generic interface deliberately exposes no `IQueryable`, so queries stay in the specific repository. | A thin layer over `DbContext`, which is already a unit of work. |
| Minimal API | Four endpoints; each handler is a few lines. | Less convention than controllers once the API grows. |
| Routed pages (list → detail) instead of master–detail | Every order has a shareable URL, and the back button and refresh behave as expected. | More navigation than a split view when working through many orders. |
| Signals and `rxResource` per page instead of NgRx | State is page-local and small; signals keep it next to the page that uses it. | No central store or devtools history if state starts being shared across pages. |
| No component library | Small surface (tables, forms, badges); plain CSS with design tokens keeps the bundle small and the look consistent across light and dark themes. | Accessibility and edge cases of controls are our own responsibility. |
| Orders are submit-once | Matches how a received PO works and keeps duplicate protection simple. A mistake is corrected by cancelling the order and submitting with a new reference. | No editing; see "With more time". |
| "Customer account no." is an external code, not a customer table | It is the account code from the sales team's own system; this app doesn't own customer data. | No lookup or validation against a customer list, and name/email are entered per order. |
| Currency allow-list (ZAR, USD, EUR, GBP) | Rejects typos and unsupported currencies at the edge. | Adding a currency means a code change. |
| Paging values clamped, not rejected | A list view should always show something; `page=0` or `pageSize=1000` become 1 and 100. | Callers aren't told their value was adjusted. |

## Known issues

- **Concurrent status changes are last-write-wins.** Each change is validated against the state it read, but there is no concurrency token, so two simultaneous changes on the same order (for example Confirm and Cancel) can both succeed, and the later write sticks.
- **HTTPS redirection warning on the `http` profile.** `UseHttpsRedirection` logs "Failed to determine the https port for redirect" because that profile has no HTTPS port. It is harmless locally and kept so the `https` profile and deployed environments redirect as they should.
- **Email validation differs slightly.** The client uses Angular's email validator, which is stricter than FluentValidation's default (an `@` that is not the first or last character). A borderline address can be rejected in the browser that the server would accept.
- **CORS allows only `http://localhost:4200`.** Local development goes through the dev proxy, so CORS isn't involved; a deployed frontend needs its origin added under `Cors:AllowedOrigins`.

The backend builds with no warnings, and `dotnet list package --vulnerable --include-transitive` reports no vulnerable packages.

## With more time

- Editing orders while Pending, with optimistic concurrency (a row version, returned as an ETag and checked on update). The same token would close the status-change race above.
- Authentication, and a "who changed what" audit trail with a status history.
- A customer master list with a picker instead of free-text account number, name and email.
- A master–detail layout for working through many orders.
- An `Idempotency-Key` header for endpoints that don't have a natural key like the external reference.
- SQL Server or PostgreSQL, with migrations run as a deployment step.
- A CI pipeline running both test suites and the builds on every pull request.
- End-to-end tests (for example Playwright) covering the submit outcomes against the real API.
- A Docker Compose file to start the API and frontend together.
