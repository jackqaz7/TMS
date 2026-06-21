# Trade Capture Slice

This slice adds the first treasury workflow behind the existing authenticated API.

## What changed

- `Trade` is the persistence entity stored in SQL Server through `TmsDbContext`.
- `CreateTradeRequest` is the input contract for the API. It has validation attributes so ASP.NET Core can reject invalid requests before business logic runs.
- `TradeResponse` is the output contract. Keeping it separate from `Trade` avoids leaking database shape directly to clients.
- `PositionSummaryDto` is a read model that groups trades by currency and calculates buy, sell, net notional, weighted average rate, and trade count.

## Endpoints

- `POST /api/treasury/trades` captures a trade.
- `GET /api/treasury/trades` returns captured trades.
- `GET /api/treasury/trades/{id}` returns one trade.
- `GET /api/treasury/positions` returns a currency-level position summary.

## Learning focus

This is the small version of a trade engine:

1. Accept a trade command.
2. Validate input.
3. Persist the trade.
4. Project trades into positions.

Later slices can publish a trade event to Kafka, cache positions in Redis, write an audit event, and show positions in WPF or React.

## Local database setup

Run Docs/Sql/CreateTrades.sql against the database configured by ConnectionStrings:TMS before calling the trade endpoints.

