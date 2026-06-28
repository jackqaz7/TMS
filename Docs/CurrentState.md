# TMS Current State

Last updated: 2026-06-28

## Read Order For Codex

Before making code changes, read:

1. `CODEX-INSTRUCTIONS.md`
2. `Docs/CurrentState.md`
3. The specific files related to the requested change

Do not build, run, test, or start services unless explicitly asked.

## Frontend Direction

The main desktop frontend is now WinForms.

- `src/TMS_WinForms_UI` owns the application shell.
- `LoginForm` logs in through CoreAPI and stores the JWT in `SessionManager`.
- `MainShellForm` owns navigation.
- `TMS_WPF_UI` is no longer a standalone WPF app.
- `TMS_WPF_UI` is a WPF component library used by WinForms.
- WinForms hosts WPF components through `ElementHost`.

Current shell flow:

```text
LoginForm -> CoreAPI /api/users/login -> JWT -> MainShellForm
MainShellForm -> ElementHost -> DashboardControl
```

## Current UI Screens

WinForms:

- `LoginForm`
- `MainShellForm`
- `UserDealerForm`
- `ReconciliationForm`

WPF components/windows:

- `DashboardControl`
- `CreateTrade`
- `Tel_dashboard`

Removed old standalone WPF app path:

- `App.xaml`
- `Login.xaml`
- `ViewModel/Login.cs`
- `WPF_Dashboard.xaml`

## Backend Direction

CoreAPI remains the main API layer in the modular monolith.

Current API areas:

- Users/authentication through `UsersController`
- Treasury trades/positions/fx rates through `TreasuryController`
- Reconciliation batches through `ReconciliationController`
- Audit events are sent to `AuditService` through `IAuditClient`

## Authentication And Users

Authentication uses `TMSAuth.dbo.AppUsers`.

Current behavior:

- `LoginForm` posts username/password to `POST /api/users/login`.
- CoreAPI returns a JWT.
- JWT is stored in `TMS_WPF_UI.Helpers.SessionManager.JwtToken`.
- The role is included as a JWT role claim.

User administration:

- `UserDealerForm` manages users and roles.
- It calls authenticated CRUD endpoints on `UsersController`.
- User CRUD audit events are written as:
  - `UserCreated`
  - `UserUpdated`
  - `UserDeleted`

Known issue:

- User CRUD currently uses `[Authorize]`, but should become admin-only with role-based authorization.
- Password storage is still learning-mode/plain comparison through `PasswordHash`; replace with real hashing later.

## Reconciliation

Backend:

- `ReconciliationBatchService` loads trade snapshots async from EF Core.
- It processes grouped reconciliation batches with `Parallel.ForEachAsync`.
- It uses bounded `MaxDegreeOfParallelism`.
- It returns matched/break groups and worker thread ids.

API:

```text
POST /api/reconciliation/batches
```

WinForms:

- `ReconciliationForm` lets the user enter ledger entries.
- The form calls the reconciliation API with `async/await`.
- The UI stays responsive while the backend processes reconciliation.
- Results are shown in a WinForms `DataGridView`.

Sample data:

- `Docs/Sql/SeedReconciliationData.sql`
- `Docs/Sql/Seed100Trades.sql`

## SQL/Data

Primary databases:

- `TMSLive` for trades, FX rates, reconciliation data
- `TMSAuth` for users/authentication

SQL scripts live in:

```text
Docs/Sql
```

Current seed scripts:

- `SeedReconciliationData.sql`
- `Seed100Trades.sql`

## Known Issues To Fix Soon

1. Add role-based authorization to user admin endpoints.
2. Add null request guards in user and reconciliation controllers.
3. Change login query to async EF Core query.
4. Replace learning-mode password storage with real password hashing.
5. Move hardcoded API base URLs into shared configuration.
6. Consider adding a shared client/helper for authenticated CoreAPI calls from WinForms/WPF.

## Working Style

Keep changes simple and learning-friendly.

Preferred workflow:

1. Read instructions/current state.
2. Scan relevant files.
3. Make the smallest useful change.
4. Add comments around architecture, async/API calls, EF Core, validation, and design patterns.
5. Do not run build/test unless explicitly asked.
