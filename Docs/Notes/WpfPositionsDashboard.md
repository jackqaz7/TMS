# WPF Positions Dashboard Slice

This slice connects the WPF desktop dashboard to the secured treasury positions API.

## What changed

- `MainWindow.xaml` now contains a real positions dashboard with a `DataGrid`.
- `Dashboard` view model calls `GET /api/treasury/positions`.
- `SessionManager.JwtToken` is sent as a Bearer token so WPF can call `[Authorize]` API endpoints.
- `ObservableCollection<PositionSummary>` stores the rows shown by the grid.
- `RefreshPositionsCommand` lets the UI reload data without putting API logic inside XAML code-behind.

## Important WPF concepts used

### DataContext

`DataContext` tells WPF what object binding expressions should read from. In `MainWindow.xaml.cs`, the dashboard view model becomes the screen's binding source:

```csharp
DataContext = _dashboard;
```

After this, XAML bindings such as `{Binding Positions}` and `{Binding RefreshPositionsCommand}` resolve against the `Dashboard` class.

### ObservableCollection

`ObservableCollection<T>` raises collection change notifications when rows are added or removed. That is why the `DataGrid` updates after the API response is copied into `Positions`.

### ICommand

Buttons in WPF usually bind to `ICommand`. `RelayCommand` is a small adapter that lets the view model expose normal C# methods as bindable UI commands.

### JWT bearer token

The API uses `[Authorize]`, so the WPF client must attach the JWT received during login:

```csharp
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
```

That line is the desktop-client equivalent of sending an `Authorization: Bearer ...` header from Postman or an HTTP file.

### Async API loading

The dashboard loads positions with `await`. This keeps the UI responsive while the HTTP request is in progress. `IsLoading` and `StatusMessage` are UI state fields that explain what the screen is doing.

## Current data path

```text
SQL Server -> EF Core -> ASP.NET Core REST -> JWT -> WPF ViewModel -> DataGrid
```

This completes the first visible end-to-end treasury workflow.
