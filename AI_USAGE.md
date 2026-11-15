# AI usage

The brief allows AI tools and asks for the prompts and my reasoning. This page covers how I used them, where I changed course, and how I checked the output. The full prompts are at the bottom.

## Tools and how I split the work

| Tool | Used for |
|---|---|
| Claude (chat) | Reading the brief, planning the architecture, weighing trade-offs, reviewing each stage's output, and writing the prompts below |
| Claude Code (in VS Code) | Making the code changes for each stage on its own branch |

I made every commit myself after reviewing the diff and running the build and tests. Nothing was committed by an AI tool.

## How I worked

1. **Understand the brief first.** Before writing code, I worked out what the assessment is really testing. The core is preventing duplicate orders when the same client reference is submitted again. The rest is the status rules, server-side totals and clear error handling.
2. **Plan, then challenge the plan.** I asked for a domain-driven design with idempotency at the centre, with every decision justified and its trade-off stated.
3. **Build in stages, one branch each.** Backend: domain, then infrastructure, then API. Frontend: foundation, then list and detail, then submit. Then docs. This keeps the commit history readable and let me review one layer at a time.
4. **Review each stage before committing.** Claude Code listed the decisions it made at each stage. I accepted, adjusted or rejected them before committing.

## Where I steered or overrode the AI

These were my calls, and they shaped the final design more than anything else.

- **I rejected the first domain model as over-engineered.** The first version had value objects for everything, strongly typed ids, a Result type alongside domain exceptions, status history, a concurrency version column and very heavy comments. It worked and its tests passed, but it was hard to read for a small app. I threw it away before committing it and set the rules myself:
  - back to basics;
  - thin files with one responsibility each;
  - the brief's own language (`PurchaseOrder`, `LineItem`);
  - entities that mirror their tables, with only small calculations (`CalculateTotals()`);
  - one `PurchaseOrderManager` that does the business work and shapes responses;
  - one validator per request.

  I hold this view for larger apps too: it stops entities growing into 800-line classes and makes rules easy to find.
- **One configuration file per table.** The first persistence code mapped every table in one long file. I split it.
- **Error handling in one place.** I asked for middleware that turns exceptions into consistent ProblemDetails responses, instead of each endpoint handling errors.
- **No "who changed it" audit.** The brief asks for timestamps only, and there's no authentication. Tracking users would have meant building auth for no benefit, so the app records `CreatedAt` and `UpdatedAt` only.
- **A small generic repository.** It's my preference. I kept its interface small (no `IQueryable`, no "get all") and put order-specific queries in `PurchaseOrderRepository`.
- **Repo and commit history.** I set up the monorepo myself and committed the plain scaffold first, so each later layer shows up as its own reviewable change.
- **Minimal API** instead of the controllers template.
- **Routed pages instead of a master–detail layout.** I asked what the industry standard is and what the trade-offs are, then chose routed pages as simpler to build, test and demo.
- **Orders are submit-once.** I questioned how users fix a mistake on a line item. Editing isn't in the brief, so I kept orders submit-once: cancel and resubmit with a new reference. I also declined extra features the AI suggested, like a "duplicate as new order" button, to keep scope tight.
- **Customer id.** I questioned whether it was a database id, which users shouldn't type. It's the customer's account code from the sales team's own system, so I had the UI label changed to "Customer account no.".
- **Visual design.** I asked for a simple, non-AI-looking design: three colours with a light and dark toggle. I picked the palette from three options.

## How I checked the output

- **Builds and tests:** I ran `dotnet build`, `dotnet test` and the frontend tests and build myself after every stage.
- **Package warnings:** I fixed build errors and package warnings, keeping everything on .NET 10 LTS and stable packages. A known SQLite advisory that couldn't be fixed safely is documented in SOLUTION.md.
- **Every flow by hand:** I tried each flow in the running app: create, resubmit unchanged, resubmit changed (conflict), invalid input, and allowed and blocked status changes.
- **Reading the code:** I read the code for each stage and asked for a walkthrough wherever something wasn't clear before committing it.

## Prompts

The prompts below are the ones I gave Claude Code. Planning and review happened in conversation with Claude; the key points of that thinking are summarised above.

<details>
<summary>Setup: monorepo scaffold</summary>

```
Context: This is a monorepo for a take-home assessment (Order Intake & Tracking app). The repo root is this folder (OrderManagement). It contains:
- backend/ – .NET 10 solution: backend/OrderManagement.slnx and backend/src/OrderManagement.Api (ASP.NET Core Web API, controllers template)
- frontend/ – empty for now; an Angular app will be added later

Git hasn't been initialised yet. I want a clean commit history, so this first commit should contain only the base scaffold.

Please do the following in the root folder only. Don't change anything inside backend/src.

1. Create a .gitignore using `dotnet new gitignore`. Add Node/Angular entries (node_modules/, dist/, .angular/) and SQLite files (*.db, *.db-shm, *.db-wal).
2. Create a .gitattributes containing `* text=auto`.
3. Create README.md with the title "Order Intake & Tracking" and a one-line description, plus empty "Getting started" and "Project structure" headings.
4. Create SOLUTION.md with the title "Solution design" and empty "Architecture", "Key decisions" and "Trade-offs" headings.
5. Add frontend/.gitkeep so the folder exists in the repo.
6. Run `git init -b main`, then `git add .`, then show me `git status`. Confirm that no bin/, obj/, .vs/ or *.csproj.user files are staged before you continue.
7. Stop after staging. Don't commit; I'll commit myself.
```
</details>

<details>
<summary>Setup: fix target framework and packages</summary>

```
Context: Monorepo for a take-home assessment. The backend is in backend/: a .NET solution (OrderManagement.slnx) with src/OrderManagement.Domain, .Application, .Infrastructure, .Api and tests/OrderManagement.Domain.Tests. It uses central package management (backend/Directory.Packages.props) and shared settings (backend/Directory.Build.props, which targets net10.0). Everything must stay on .NET 10 LTS. The SDK on this machine is .NET 11, so don't let anything drift to net11.0 or to 11.x packages.

Current problems:
- OrderManagement.Api fails to build: AddOpenApi and MapOpenApi aren't found, because the Microsoft.AspNetCore.OpenApi PackageReference was removed.
- Restore warns NU1903: SQLitePCLRaw.lib.e_sqlite3 2.1.11 has a high-severity vulnerability (GHSA-2m69-gcr7-jv3q). It comes in through Microsoft.EntityFrameworkCore.Sqlite.

Please:
1. In OrderManagement.Api.csproj: make sure there's no TargetFramework override (it should inherit net10.0). Make sure it references Microsoft.AspNetCore.OpenApi and Microsoft.EntityFrameworkCore.Design (Design with PrivateAssets="all"). Make sure it has project references to OrderManagement.Application and OrderManagement.Infrastructure. No Version attributes in any csproj; versions belong only in Directory.Packages.props.
2. In Directory.Packages.props: update every package to its latest stable patch within its current major line (10.0.x for Microsoft packages). Use `dotnet list package --outdated --include-transitive --highest-patch` to find them. Add an entry for Microsoft.EntityFrameworkCore.Design if it's missing. No previews, and no major-version upgrades.
3. Fix the NU1903 warning. Prefer a newer EF Core 10.0.x patch that brings in a fixed SQLitePCLRaw. If that isn't enough, check the advisory for a patched 2.1.x version of SQLitePCLRaw.lib.e_sqlite3 and pin it with CentralPackageTransitivePinningEnabled. Don't pin SQLitePCLRaw 3.x, because EF Core 10 is built against 2.1. If the only fix is 3.x, leave the warning and tell me.
4. Don't change any C# code in Domain, Application or Infrastructure, and don't edit Program.cs.
5. Run `dotnet build` and `dotnet test` from backend/ and show me the results.
6. Don't stage or commit anything. Just leave the changes in the working tree and give me a short summary of every file you changed and why.
```
</details>

<details>
<summary>Backend stage 1: domain (feature/domain-design)</summary>

```
Context: Monorepo for a take-home assessment: "Order Intake & Tracking". A sales team records customer purchase orders and tracks their status. Sales reps sometimes resubmit the same order, so submitting the same client-provided external reference again must not create a duplicate and must behave consistently. Backend: backend/OrderManagement.slnx, .NET 10 LTS, central package management in backend/Directory.Packages.props, shared settings in backend/Directory.Build.props.

We're rebuilding the backend in 3 stages (domain, then infrastructure, then API), each on its own branch. This is stage 1: the domain.

Ground rules:
- Stay on .NET 10 and stable packages: the latest patch within the current major version, no previews. Package versions only in Directory.Packages.props, never in a csproj.
- Keep it simple, no over-engineering. Thin files with one responsibility; one class per file unless two are genuinely coupled. Comments only where they explain why, not what.
- Use the brief's language: PurchaseOrder, LineItem, Customer, external reference, Pending/Confirmed/Fulfilled/Cancelled.

Steps:
1. Run `git status`. If there are uncommitted changes, stash them with the message "old-scaffold" instead of deleting them. Then create the branch feature/domain-design from main.
2. Remove the previous attempt: delete the OrderManagement.Application project, everything inside src/OrderManagement.Domain and src/OrderManagement.Infrastructure (delete the Infrastructure project entirely; stage 2 recreates it), tests/OrderManagement.Domain.Tests, and src/OrderManagement.Api/Infrastructure. Remove the deleted projects from the .slnx and from the Api's project references. Leave Program.cs and the Controllers alone for now.
3. Build the Domain project (it can reference FluentValidation) with this structure:
   - Common/BaseEntity.cs: Guid Id, DateTime CreatedAt, DateTime? UpdatedAt (UTC; the infrastructure sets these later).
   - Common/IRepository.cs: small generic interface: GetByIdAsync, AddAsync, Update, Remove, SaveChangesAsync. No IQueryable, no GetAll.
   - Common/Exceptions/: NotFoundException, ConflictException (carries the existing order id), BusinessRuleException (can carry extra details, e.g. allowed transitions). For validation, use FluentValidation's ValidationException.
   - Common/PagedResult.cs.
   - PurchaseOrders/PurchaseOrder.cs: entity that mirrors its table: ExternalReference, CustomerId, CustomerName, CustomerEmail, Currency, Notes, Status, Subtotal, Total, Fingerprint, LineItems. The only behaviour is CalculateTotals() (line totals, subtotal, total = subtotal).
   - PurchaseOrders/LineItem.cs: Id, PurchaseOrderId, Sku, Name, Quantity, UnitPrice, LineTotal.
   - PurchaseOrders/OrderStatus.cs (enum) and OrderStatusTransitions.cs (static table: Pending → Confirmed|Cancelled, Confirmed → Fulfilled|Cancelled; Fulfilled and Cancelled are final).
   - PurchaseOrders/Models/: CreatePurchaseOrderRequest, LineItemRequest, ChangeOrderStatusRequest, PurchaseOrderResponse (include AllowedTransitions), LineItemResponse, SubmitPurchaseOrderResult (Order + WasCreated).
   - PurchaseOrders/Validators/: one FluentValidation validator per request in its own file. Rules: external reference required, max 50, letters/digits/- _ / . # only; customer id, name and valid email required; currency one of ZAR, USD, EUR, GBP; 1 to 100 line items; SKU and name required; quantity a whole number of at least 1; unit price at least 0 with at most 2 decimals; no duplicate SKU (case-insensitive); notes max 1000.
   - PurchaseOrders/PurchaseOrderFingerprint.cs: small static helper. SHA-256 of the normalised content (customer, currency, notes, line items sorted by SKU, prices formatted without trailing zeros), so a retry matches and a changed order doesn't.
   - PurchaseOrders/IPurchaseOrderRepository.cs: extends IRepository<PurchaseOrder> with GetWithLineItemsAsync, GetByReferenceAsync, ListAsync(status?, page, pageSize) newest first, and TryAddAsync (returns false if the external reference already exists).
   - PurchaseOrders/PurchaseOrderManager.cs: does the business work and shapes responses.
     - SubmitAsync: validate; normalise the reference (trim, upper-case). If the reference exists: same fingerprint → return the existing order with WasCreated=false; different → ConflictException. Otherwise build the order, CalculateTotals and TryAddAsync; if that returns false (lost a race), re-read and apply the same replay/conflict logic.
     - GetAsync (NotFoundException if missing) and ListAsync.
     - ChangeStatusAsync: same status → no-op success; disallowed → BusinessRuleException with a helpful message naming the allowed next statuses; otherwise update and save.
4. Create tests/OrderManagement.Domain.Tests (xUnit) using a simple in-memory fake repository. Cover the validator rules, totals, submit (new, replay, conflict), the full 4×4 status transition matrix, and the helpful messages. Add the projects to the .slnx.
5. Run `dotnet build` and `dotnet test` from backend/. Don't stage or commit anything. Finish with a short summary of the structure and every file you created or removed.
```

Follow-up after reviewing stage 1:

```
Before I commit: add max-length rules to the validators and expose the limits as constants so stage 2 can reuse them for column sizes: SKU 64, item name 200, customer id 50, customer name 200, customer email 254 (external reference 50 and notes 1000 already exist). Add tests for each. Also confirm the validator and PurchaseOrderManager normalise the external reference the same way (trim, then upper-case). Run dotnet test. Don't stage or commit.
```
</details>

<details>
<summary>Backend stage 2: persistence (feature/infrastructure-persistence)</summary>

```
Context: Same monorepo and ground rules as before (.NET 10, stable packages only, versions only in Directory.Packages.props, thin single-responsibility files, minimal comments). Stage 1 is merged: OrderManagement.Domain has PurchaseOrder, LineItem, BaseEntity, IRepository<T>, IPurchaseOrderRepository and PurchaseOrderManager. This is stage 2: persistence with EF Core code-first on SQLite.

Steps:
1. Make sure main is up to date, then create the branch feature/infrastructure-persistence from it.
2. Create src/OrderManagement.Infrastructure (references Domain; uses Microsoft.EntityFrameworkCore.Sqlite) and add it to the .slnx:
   - Persistence/AppDbContext.cs: DbSets for PurchaseOrders and LineItems. Apply configurations from the assembly. Store DateTimes as UTC and read them back marked as UTC.
   - Persistence/Configurations/PurchaseOrderConfiguration.cs and LineItemConfiguration.cs: one table per file. Unique index on ExternalReference (this guarantees no duplicates under concurrency); indexes on CreatedAt and Status; Status stored as a string; sensible max lengths; money precision (18,2); LineItems → PurchaseOrder one-to-many with cascade delete.
   - Persistence/Interceptors/TimestampInterceptor.cs: sets CreatedAt on add and UpdatedAt on modify, for any BaseEntity. Uses TimeProvider.
   - Repositories/Repository.cs: generic implementation of IRepository<T>.
   - Repositories/PurchaseOrderRepository.cs: inherits Repository<PurchaseOrder>. GetWithLineItemsAsync and GetByReferenceAsync include line items. ListAsync: newest first (CreatedAt desc, then Id), optional status filter, paging, total count. TryAddAsync catches the SQLite unique-constraint violation on ExternalReference only, clears the change tracker and returns false.
   - DependencyInjection.cs: AddInfrastructure(connectionString) registers the DbContext, interceptor, TimeProvider and repositories, plus a helper that applies migrations at startup.
3. Create tests/OrderManagement.Infrastructure.Tests (xUnit) using SQLite in-memory mode (a real database, so the unique index is enforced). Cover:
   - the timestamps get set;
   - listing is newest first and the status filter works;
   - TryAddAsync returns false for a duplicate reference;
   - firing several concurrent SubmitAsync calls with the same request through PurchaseOrderManager creates exactly one order, and every call gets that same order back.
4. Don't create migrations yet; they come in the API stage.
5. Run `dotnet build` and `dotnet test` from backend/. Don't stage or commit anything. Summarise every file you created and the schema (tables, columns, indexes).
```
</details>

<details>
<summary>Backend stage 3: API (feature/api-endpoints)</summary>

```
Context: Same monorepo and ground rules as before. Stages 1 and 2 are merged: the Domain project has PurchaseOrderManager with validators and custom exceptions; Infrastructure has AppDbContext, repositories and AddInfrastructure. This is stage 3: a minimal API on top.

Steps:
1. Make sure main is up to date, then create the branch feature/api-endpoints from it.
2. In src/OrderManagement.Api (references Domain and Infrastructure):
   - Delete the Controllers folder and WeatherForecast.cs. Use minimal APIs only.
   - Endpoints/PurchaseOrderEndpoints.cs: MapGroup("/api/purchase-orders"), with:
     - POST: 201 Created with a Location header for a new order; 200 OK with the existing order on a replay.
     - GET with ?status=&page=&pageSize=: newest first.
     - GET /{id}
     - PUT /{id}/status
     Endpoints stay thin: call PurchaseOrderManager and return the result.
   - Middleware/ExceptionHandlingMiddleware.cs: map exceptions to ProblemDetails:
     - ValidationException → 400, with field errors grouped by property;
     - NotFoundException → 404;
     - ConflictException → 409, including existingOrderId;
     - BusinessRuleException → 422, including allowedTransitions;
     - anything else → 500, with no internal details.
   - Program.cs: AddInfrastructure with a connection string from appsettings ("Data Source=ordermanagement.db"); register PurchaseOrderManager and the validators; serialise enums as strings; CORS for http://localhost:4200; OpenAPI plus the Scalar API reference UI in Development; apply migrations at startup.
   - Create the initial EF Core migration with `dotnet ef migrations add InitialCreate`, with Infrastructure as the project and Api as the startup project, output to Persistence/Migrations. Add Microsoft.EntityFrameworkCore.Design to the Api if it's missing.
   - Rewrite OrderManagement.Api.http with sample requests: create, replay (same body), conflict (same reference, different body), invalid input, list, get, a valid status change and an invalid one.
3. Create tests/OrderManagement.Api.Tests (xUnit, WebApplicationFactory, SQLite in-memory). Cover:
   - POST twice → 201 then 200 with the same id;
   - same reference with a different body → 409;
   - invalid body → 400 with field errors;
   - invalid status change → 422.
4. Run `dotnet build` and `dotnet test` from backend/, then start the API briefly to confirm it runs and the database is created. Don't stage or commit anything. Summarise every file you changed and list the endpoints with their response codes.
```
</details>

<details>
<summary>Frontend stage 1: foundation (feature/frontend-foundation)</summary>

```
Context: Monorepo for a take-home assessment: "Order Intake & Tracking". Sales reps record customer purchase orders and track their status. The backend is done: backend/ has an ASP.NET Core minimal API at /api/purchase-orders (see backend/src/OrderManagement.Api/Endpoints and the request/response models in backend/src/OrderManagement.Domain/PurchaseOrders/Models). It returns ProblemDetails for errors: 400 with field errors, 404, 409 with existingOrderId, 422 with allowedTransitions. The frontend goes in frontend/ (currently only a .gitkeep). We're building it in 3 stages, each on its own branch. This is stage 1: the foundation.

Ground rules:
- Latest stable Angular (18+), TypeScript 5+. Standalone components, signals, inject(), the new control flow (@if/@for), OnPush.
- No component library (no Angular Material or Bootstrap). Plain CSS with design tokens.
- Keep it simple: thin files, one responsibility each, minimal comments (only where they explain why).
- Design must look like a well-made internal tool, not AI-generated: no gradients, shadows, glows, glassmorphism, emoji, sparkle icons, hero banners or pill-shaped everything. 1px borders, 4–6px radius, generous spacing, system font stack, tabular numbers for money, monospace for order references.

Steps:
1. Create the branch feature/frontend-foundation from main. Delete frontend/.gitkeep, then create the app non-interactively in frontend/ (app name order-management-web, routing on, CSS, no SSR, skip git so there's no nested repo).
2. Structure:
   - src/styles/tokens.css: the theme as CSS variables under [data-theme="light"] and [data-theme="dark"]. Three colours only:
     - light: background #F8F8F6, ink #1C2230, accent #1F4E8C
     - dark: background #12151C, ink #E4E6EB, accent #7FA6DB
     Derive surface, border, muted text and accent tint from these three with color-mix(); don't add new hues. The one exception is a muted error red (light #A33A3A, dark #E08A8A), used only for errors.
   - src/styles/base.css: resets, typography, form controls, buttons (primary = accent fill, secondary = bordered), tables, and a visible focus outline in the accent colour.
   - app/core/theme/theme.service.ts: a signal for the current theme. Starts from localStorage if set, otherwise prefers-color-scheme. Sets data-theme on <html> and saves the choice. Add a tiny inline script in index.html that sets data-theme before Angular boots, so there's no flash.
   - app/layout/app-shell: header with the app name "Order Management", an "Orders" nav link, and a sun/moon theme toggle (inline SVG icon, aria-label, no emoji). Content area max-width ~1100px.
   - app/core/http/api-error.ts and error.interceptor.ts: turn HttpErrorResponse/ProblemDetails into a typed ApiError (status, title, detail, fieldErrors, existingOrderId, allowedTransitions), plus a friendly message for network errors.
   - app/features/purchase-orders/models/: TypeScript types that match the backend DTOs exactly (status as a string union).
   - app/features/purchase-orders/data-access/purchase-orders.api.ts:
     - list(status?, page, pageSize)
     - get(id)
     - submit(request): returns { order, created }, where created is true for 201 and false for 200
     - changeStatus(id, status)
   - Routes: '' → /orders; /orders shows a simple placeholder for now; ** → a small not-found page.
3. Add proxy.conf.json, wired into ng serve, that sends /api to the API's http URL (read it from backend/src/OrderManagement.Api/Properties/launchSettings.json). The API base URL in the app is just '/api'.
4. Add a unit test for ThemeService and one for the error interceptor, using the project's default test runner.
5. Run the build and the tests. Don't stage or commit anything. Summarise the structure and every file you created.
```
</details>

<details>
<summary>Frontend stage 2: list and detail (feature/frontend-orders-list)</summary>

```
Context: Same monorepo and ground rules as frontend stage 1: Angular standalone with signals, no component library, design tokens in src/styles/tokens.css, simple non-AI look, thin files. Stage 1 is merged: theme, app shell, error interceptor, models and PurchaseOrdersApi exist. This is stage 2: the list and detail pages. Read the backend models and endpoints if you need to check field names.

Steps:
1. Make sure main is up to date, then create the branch feature/frontend-orders-list from it.
2. Shared UI in app/shared/ui/: a status-badge component (text plus a subtle tint using only the theme colours; no new colours per status) and small loading, empty-state and error-message components.
3. /orders: features/purchase-orders/pages/order-list
   - A table, newest first (the API already sorts), with columns: reference (monospace), customer name, number of items, total (currency code plus amount, right-aligned, tabular numbers), status badge, created date.
   - A status filter (All plus the four statuses) and paging (previous/next, with "Showing 1–20 of 57"). Keep the filter and page in the URL query params so the back button and refresh keep them.
   - The whole row links to the detail page. A "New order" button goes to /orders/new (that page comes in stage 3).
   - Loading, empty (an invitation to create the first order) and error states.
4. /orders/:id: features/purchase-orders/pages/order-detail
   - Header: reference and status badge, created and updated dates.
   - Customer section (id, name, email), currency and notes.
   - Line items table (SKU, name, quantity, unit price, line total), plus subtotal and total, showing the server's values only.
   - Status change: offer only the order's allowedTransitions, as buttons. Cancelling needs a simple inline confirm step (no browser dialogs). Show "This order is in a final status" when there are none. Show a 422 message from the server inline. Update the view from the response.
   - A "Back to orders" link that returns to the list with its previous filter and page.
   - 404 state: "Order not found", with a link back.
5. Page components stay thin: they call PurchaseOrdersApi and hold state in signals. No global store.
6. Add tests for the status badge and for the list page (rows render, filter updates the query params) and the detail page (only allowed transitions are shown), using HttpTestingController.
7. Run the build and the tests, and check both pages against the running API. Don't stage or commit anything. Summarise the files you changed.
```
</details>

<details>
<summary>Frontend stage 3: submit (feature/frontend-order-submit)</summary>

```
Context: Same monorepo and ground rules as the earlier frontend stages. Stages 1 and 2 are merged: the theme, shell, list and detail pages work. This is stage 3: submitting a new purchase order. Key requirement from the brief: reps sometimes resubmit the same order. The backend returns 201 for a new order, 200 with the existing order when the same external reference is resubmitted with the same content, and 409 (with existingOrderId) when that reference already belongs to a different order. The UI must make each outcome clear.

Steps:
1. Make sure main is up to date, then create the branch feature/frontend-order-submit from it.
2. /orders/new: features/purchase-orders/pages/order-create, with a typed reactive form:
   - External reference, customer id, customer name, customer email, currency (select: ZAR, USD, EUR, GBP), notes.
   - Line items as a FormArray, with SKU, name, quantity and unit price per row. "Add line" and "Remove" buttons; at least one row.
   - Client-side validators that mirror the backend's rules and limits. Read them from backend/src/OrderManagement.Domain/PurchaseOrders/Validators: required fields, max lengths, allowed reference characters, valid email, quantity a whole number of at least 1, price at least 0 with at most 2 decimals, no duplicate SKUs. Show each error under its field after the user touches it or tries to submit.
   - A live estimate of line totals and the subtotal, clearly labelled "Estimate – final totals are calculated by the server".
   Split the line-items editor into its own component if the form file gets long.
3. Submitting:
   - Disable the submit button and show "Submitting…" while the request is in flight, so double clicks can't send twice.
   - 201 → go to the order's detail page with a banner "Order created".
   - 200 → go to the detail page with a banner "This order was already submitted – showing the existing order".
   - 409 → stay on the form and show the server's message with an "Open existing order" link (using existingOrderId).
   - 400 → map the server's field errors onto the matching form controls (including line items like LineItems[0].Quantity), with a short summary at the top.
   - Network error → keep everything the user entered and show "Couldn't reach the server. Your order wasn't lost – try again." Retrying with the same reference is safe.
   Pass the banner to the detail page through router navigation state. Stage 2's detail page shows it once.
4. Add tests: the validators (including duplicate SKU and decimals), and each submit outcome (201, 200, 409, 400) using HttpTestingController.
5. Run the build and the tests, and try all the outcomes against the running API. Don't stage or commit anything. Summarise the files you changed.
```
</details>

<details>
<summary>Docs and label fix (docs/readme-and-solution)</summary>

```
Context: Monorepo for a take-home assessment: "Order Intake & Tracking". backend/ has a .NET 10 minimal API (Domain, Infrastructure, Api projects and their tests; SQLite with EF Core migrations applied at startup). frontend/ has an Angular app (list, detail and create pages, light/dark theme, dev proxy to the API). Everything is built and working. This is the final stage: a small UI label fix and the documentation reviewers will read first.

Steps:
1. Make sure main is up to date, then create the branch docs/readme-and-solution from it.
2. Label fix (frontend only; don't change the backend or API field names): in the create form and the detail page, label the customer id field "Customer account no." with the placeholder "ACC-1042". It's the customer's account code from the sales team's own system, not a database id. Update any tests that check the label.
3. README.md at the repo root: overview, requirements (exact .NET SDK and Node.js versions read from the repo), project structure, how to run the backend and frontend, how to run the tests, an API overview table with response codes, a short "Trying the duplicate protection" section, and brief troubleshooting.
4. SOLUTION.md at the repo root: architecture, duplicate protection, status rules, validation and errors, a key decisions and trade-offs table, known issues, and what I'd do with more time.
5. Use plain, direct language. No marketing tone, no emoji, no AI attribution anywhere.
6. Run the frontend tests and build to confirm the label change. Don't stage or commit anything. Summarise the files you changed.
```
</details>
