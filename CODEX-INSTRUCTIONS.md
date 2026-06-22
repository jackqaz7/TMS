# TMS Codex Instructions

## Default Behavior
- Keep explanations brief unless I ask for more detail.
- Do not build, compile, run, start servers, or execute tests unless I explicitly ask.
- When writing/changing code, add useful technical comments that help me learn while debugging.
- Prefer simple, learning-friendly implementation over over-engineering.
- If I ask a conceptual question, answer briefly first and expand only if asked.
- If I ask for code only, provide code/instructions and do not edit the repo unless I explicitly ask.

## Project Context
- Repo path: `C:\TMS`
- Project goal: build a small Treasury Management System to learn modern architecture by implementing it step by step.
- Current direction: modular monolith first, selected microservices later.
- Primary workflow: WPF desktop app first, React web app later after APIs are stable.

## Architecture / Tech Stack To Keep In Mind

### Presentation
- C# WPF desktop dashboard first.
- React TypeScript web UI later.

### Security
- JWT authentication currently.
- OAuth2 / Azure AD / Entra ID later.
- HTTPS and rate limiting later through API gateway/hardening.

### API Layer
- ASP.NET Core REST APIs currently.
- Swagger / OpenAPI for documentation and testing.
- GraphQL / HotChocolate later only if dashboard/read queries benefit from it.

### Core Application Style
- Start as a modular monolith.
- Keep clear modules for Trades, Positions, FX Rates, Users/Roles, Validation.
- Use services for shared business rules, especially rules needed by WPF and future React.
- API validation is authoritative; WPF/React validation is only for user experience.

### Microservices Later
- Trade Engine: C++ / multithreaded, only if needed for performance-heavy trade processing.
- Forecast Service: C# cash-flow forecasting.
- Audit Service: event log writer.
- AI Risk Summariser: OpenAI / Ollama.

### Messaging And Cache
- Apache Kafka later for trade event streams.
- Redis later for FX-rate cache, positions cache, and possibly session/cache scenarios.

### Data
- SQL Server first for trades, users, ledger, FX rates, and core TMS data.
- PostgreSQL later for audit/events if useful.

### Observability
- Serilog + Seq first for structured logging.
- Prometheus and Grafana later for metrics, dashboards, and alerts.

### Cloud / Infra
- Docker later for containerization.
- Kubernetes / Minikube later for learning orchestration.
- Azure preferred first: Azure App Service / AKS / Entra ID.
- AWS ECS / EC2 optional only for portability or comparison.
- GitHub Actions later for CI/CD.

## Coding Preferences
- Follow existing project patterns before introducing new abstractions.
- Keep changes scoped to the requested feature.
- Do not remove user changes unless I explicitly ask.
- Prefer readable code over clever code.
- Add comments around architecture decisions, async/API calls, EF Core queries, validation, and design patterns.
- Avoid large refactors unless required for the feature.

## Current Practical Priorities
- Finish Create Trade end-to-end.
- Save trades to DB through API.
- Keep WPF validation for UX and API validation for real business rules.
- Use FX rates stored in DB for auto-calculation.
- Refresh positions from saved trades.
- Add React only after the backend API is stable.
