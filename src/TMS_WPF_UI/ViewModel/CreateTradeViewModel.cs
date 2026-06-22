using CoreAPI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TMS_WPF_UI.Helpers;

namespace TMS_WPF_UI.ViewModel
{
    // ViewModel in MVVM:
    // - The XAML view owns only UI layout.
    // - This ViewModel owns screen state, commands, validation, and API calls.
    // - The model object CreateTradeRequest is the DTO sent to the ASP.NET Core API.
    public class CreateTradeViewModel : INotifyPropertyChanged
    {
        // For now the API URL is hardcoded so the learning flow is easy to follow.
        // Later this should move to appsettings.json or a typed API client service.
        private const string TreasuryApiBaseAddress = "https://localhost:7104/api/";

        private string _message = string.Empty;
        private bool _isSaving;

        // ObservableCollection is useful for WPF binding because the UI can observe
        // changes to the list. These lists are static today, but later they should be
        // loaded from TradeTypes and Currencies tables through API endpoints.
        public ObservableCollection<string> TradeTypes { get; } = new() { "FX_SPOT", "FX_FORWARD", "FX_SWAP" };
        public ObservableCollection<string> Sides { get; } = new() { "BUY", "SELL" };
        public ObservableCollection<string> Currencies { get; } = new() { "USD", "INR", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD", "SGD", "AED" };

        // Trade is the form backing object. XAML fields bind to properties like
        // Trade.TradeReference, Trade.Amount1, Trade.Currency1, etc.
        public CreateTradeRequest Trade { get; private set; } = CreateDefaultTrade();

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        // Used to disable Save while the HTTP call is running. This avoids duplicate
        // submits if the user clicks Save multiple times quickly.
        public bool IsSaving
        {
            get => _isSaving;
            set { _isSaving = value; OnPropertyChanged(); }
        }

        // These computed properties drive conditional visibility in XAML.
        // FX_SPOT shows only basic fields; FX_FORWARD and FX_SWAP show leg fields.
        public bool IsForwardOrSwap => Trade.TradeType == "FX_FORWARD" || Trade.TradeType == "FX_SWAP";
        public bool IsSwap => Trade.TradeType == "FX_SWAP";

        // Commands are the MVVM replacement for button click event handlers.
        // Buttons in XAML bind to these ICommand properties.
        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CancelCommand { get; }

        // The ViewModel does not directly call Window.Close(). Instead it raises an
        // event and the view decides how to close itself. That keeps UI details out of
        // the ViewModel.
        public event EventHandler? RequestClose;
        public event PropertyChangedEventHandler? PropertyChanged;

        public CreateTradeViewModel()
        {
            // RelayCommand adapts a normal C# delegate into WPF's ICommand interface.
            // The async lambda lets the UI thread stay responsive while the API call runs.
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsSaving);
            ResetCommand = new RelayCommand(_ => Reset());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty));
        }

        public void OnTradeTypeChanged()
        {
            // Changing trade type affects which fields are visible. WPF must be told
            // that these computed properties changed even though they do not have setters.
            OnPropertyChanged(nameof(IsForwardOrSwap));
            OnPropertyChanged(nameof(IsSwap));
        }


        public async Task RecalculateFxAmountAsync()
        {
            // This method is called by the form when Amount1, Currency1, or Currency2 changes.
            // It asks the API for the latest stored FX rate and then calculates Amount2.
            // The calculation stays in WPF for fast UX, while the source rate still comes
            // from the backend/database so WPF and future React use the same rate source.
            var fromCurrency = Trade.Currency1?.Trim().ToUpperInvariant() ?? string.Empty;
            var toCurrency = Trade.Currency2?.Trim().ToUpperInvariant() ?? string.Empty;

            if (!IsValidCurrency(fromCurrency) || !IsValidCurrency(toCurrency) || fromCurrency == toCurrency)
            {
                return;
            }

            if (Trade.Amount1 <= 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SessionManager.JwtToken))
            {
                Message = "Login token is missing. Please login again.";
                return;
            }

            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(TreasuryApiBaseAddress) };
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);

                var url = $"treasury/fx-rates/latest?fromCurrency={Uri.EscapeDataString(fromCurrency)}&toCurrency={Uri.EscapeDataString(toCurrency)}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Message = $"FX rate lookup failed: {(int)response.StatusCode} {response.ReasonPhrase}";
                    return;
                }

                var fxRate = await response.Content.ReadFromJsonAsync<FxRateResponse>();

                if (fxRate == null)
                {
                    Message = "FX rate lookup returned no data.";
                    return;
                }

                Trade.FxRateUsed = fxRate.Rate;
                Trade.RateDate = fxRate.RateDate.Date;
                Trade.Amount2 = decimal.Round(Trade.Amount1 * fxRate.Rate, 2);

                // CreateTradeRequest is a simple DTO and does not raise property changed
                // events itself. Raising Trade tells WPF to refresh bindings like
                // Trade.FxRateUsed, Trade.RateDate, and Trade.Amount2.
                OnPropertyChanged(nameof(Trade));

                Message = fxRate.IsCrossRate
                    ? $"FX cross rate loaded: {fromCurrency}/{toCurrency} = {fxRate.Rate}"
                    : $"FX rate loaded: {fromCurrency}/{toCurrency} = {fxRate.Rate}";
            }
            catch (Exception ex)
            {
                Message = $"FX rate lookup failed: {ex.Message}";
            }
        }
        private async Task SaveAsync()
        {
            // UX validation runs before the API call so the user gets instant feedback.
            // The API must still validate again because WPF is only one possible client.
            var validationMessage = Validate();

            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                Message = validationMessage;
                return;
            }

            // Every treasury API call is protected by JWT. The token was created during
            // login and stored in SessionManager for this simple desktop application.
            if (string.IsNullOrWhiteSpace(SessionManager.JwtToken))
            {
                Message = "Login token is missing. Please login again.";
                return;
            }

            IsSaving = true;
            Message = "Saving trade...";

            try
            {
                // HttpClient sends the DTO as JSON to POST /api/treasury/trades.
                // The Bearer token is added to the Authorization header so the API can
                // identify and authorize the current user.
                using var client = new HttpClient { BaseAddress = new Uri(TreasuryApiBaseAddress) };
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);

                var response = await client.PostAsJsonAsync("treasury/trades", Trade);

                if (response.IsSuccessStatusCode)
                {
                    Message = "Trade saved.";
                    RequestClose?.Invoke(this, EventArgs.Empty);
                    return;
                }

                Message = $"Trade save failed: {(int)response.StatusCode} {response.ReasonPhrase}";
            }
            catch (Exception ex)
            {
                // This catches client-side failures such as API not running, certificate
                // issues, DNS/network errors, or JSON serialization problems.
                Message = $"Trade save failed: {ex.Message}";
            }
            finally
            {
                // finally always runs, whether the save succeeds, fails, or throws.
                IsSaving = false;
            }
        }

        private string Validate()
        {
            var errors = new List<string>();

            // Normalize user input before validation and before sending to API.
            // Example: " usd " becomes "USD" so comparison and storage are consistent.
            Trade.TradeReference = Trade.TradeReference?.Trim() ?? string.Empty;
            Trade.Counterparty = Trade.Counterparty?.Trim() ?? string.Empty;
            Trade.CounterpartyBankAccount = Trade.CounterpartyBankAccount?.Trim();
            Trade.Currency1 = Trade.Currency1?.Trim().ToUpperInvariant() ?? string.Empty;
            Trade.Currency2 = Trade.Currency2?.Trim().ToUpperInvariant() ?? string.Empty;
            Trade.TradeType = Trade.TradeType?.Trim().ToUpperInvariant() ?? string.Empty;
            Trade.Side = Trade.Side?.Trim().ToUpperInvariant() ?? string.Empty;
            Trade.Comments = Trade.Comments?.Trim();

            // Text lengths match the SQL table column sizes. This prevents avoidable
            // database errors and gives the user a readable message before Save.
            ValidateRequiredText(errors, Trade.TradeReference, "Trade reference", 40);
            ValidateRequiredText(errors, Trade.Counterparty, "Counterparty", 120);
            ValidateOptionalText(errors, Trade.CounterpartyBankAccount, "Bank account", 50);
            ValidateOptionalText(errors, Trade.Comments, "Comments", 500);

            // These checks protect against invalid values if the bound ComboBox is ever
            // changed, populated incorrectly, or edited through a future UI path.
            if (!TradeTypes.Contains(Trade.TradeType))
            {
                errors.Add("Trade type is invalid.");
            }

            if (!Sides.Contains(Trade.Side))
            {
                errors.Add("Side must be BUY or SELL.");
            }

            // Currency code shape is checked here. Later the API can also verify that
            // each code exists in dbo.Currencies and is active.
            if (!IsValidCurrency(Trade.Currency1) || !IsValidCurrency(Trade.Currency2))
            {
                errors.Add("Both currencies are required.");
            }
            else if (Trade.Currency1 == Trade.Currency2)
            {
                errors.Add("Currency 1 and Currency 2 cannot be same.");
            }

            // Treasury amounts and rates must be positive. Fees can be zero, but not negative.
            if (Trade.Amount1 <= 0) errors.Add("Amount 1 must be greater than zero.");
            if (Trade.Amount2 <= 0) errors.Add("Amount 2 must be greater than zero.");
            if (Trade.FxRateUsed <= 0) errors.Add("FX rate must be greater than zero.");
            if (Trade.Fees < 0) errors.Add("Fees cannot be negative.");

            if (Trade.RateDate == default) errors.Add("Rate date is required.");
            if (Trade.TradeDate == default) errors.Add("Trade date is required.");
            if (Trade.SettlementDate == default) errors.Add("Settlement date is required.");

            if (Trade.SettlementDate.Date < Trade.TradeDate.Date)
            {
                errors.Add("Settlement date cannot be before trade date.");
            }

            // FX_FORWARD needs near-leg information. FX_SWAP needs both near and far legs.
            if (Trade.TradeType == "FX_FORWARD")
            {
                ValidateForwardLeg(errors);
            }

            if (Trade.TradeType == "FX_SWAP")
            {
                ValidateForwardLeg(errors);
                ValidateSwapLeg(errors);
            }

            return errors.Count == 0
                ? string.Empty
                : "Please fix:" + Environment.NewLine + "- " + string.Join(Environment.NewLine + "- ", errors);
        }

        private void ValidateForwardLeg(List<string> errors)
        {
            // The near leg represents the first settlement leg for forwards/swaps.
            if (Trade.NearLegDate == null) errors.Add("Near leg date is required.");
            if (Trade.NearLegRate == null || Trade.NearLegRate <= 0) errors.Add("Near leg rate must be greater than zero.");
            if (Trade.NearLegAmount1 is <= 0) errors.Add("Near leg amount 1 must be greater than zero.");
            if (Trade.NearLegAmount2 is <= 0) errors.Add("Near leg amount 2 must be greater than zero.");

            if (Trade.NearLegDate != null && Trade.NearLegDate.Value.Date < Trade.TradeDate.Date)
            {
                errors.Add("Near leg date cannot be before trade date.");
            }
        }

        private void ValidateSwapLeg(List<string> errors)
        {
            // The far leg is required only for FX swaps. It must happen after the near leg.
            if (Trade.FarLegDate == null) errors.Add("Far leg date is required.");
            if (Trade.FarLegRate == null || Trade.FarLegRate <= 0) errors.Add("Far leg rate must be greater than zero.");
            if (Trade.FarLegAmount1 is <= 0) errors.Add("Far leg amount 1 must be greater than zero.");
            if (Trade.FarLegAmount2 is <= 0) errors.Add("Far leg amount 2 must be greater than zero.");

            if (Trade.NearLegDate != null && Trade.FarLegDate != null &&
                Trade.FarLegDate.Value.Date <= Trade.NearLegDate.Value.Date)
            {
                errors.Add("Far leg date must be after near leg date.");
            }
        }

        private static bool IsValidCurrency(string currency)
        {
            // ISO currency codes are three letters, for example USD, INR, EUR.
            return currency.Length == 3 && currency.All(char.IsLetter);
        }

        private static void ValidateRequiredText(List<string> errors, string value, string fieldName, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{fieldName} is required.");
                return;
            }

            ValidateOptionalText(errors, value, fieldName, maxLength);
        }

        private static void ValidateOptionalText(List<string> errors, string? value, string fieldName, int maxLength)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
            {
                errors.Add($"{fieldName} cannot be more than {maxLength} characters.");
            }
        }

        private void Reset()
        {
            // Reset creates a brand-new DTO so old values do not accidentally remain.
            Trade = CreateDefaultTrade();
            Message = string.Empty;
            OnPropertyChanged(nameof(Trade));
            OnTradeTypeChanged();
        }

        private static CreateTradeRequest CreateDefaultTrade()
        {
            // Defaults make the form usable immediately and represent the most common
            // trade type in our current learning flow.
            return new CreateTradeRequest
            {
                TradeType = "FX_SPOT",
                Side = "BUY",
                Currency1 = "USD",
                Currency2 = "INR",
                TradeDate = DateTime.Today,
                SettlementDate = DateTime.Today.AddDays(2),
                RateDate = DateTime.Today
            };
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // This notifies WPF binding that a property changed so the screen refreshes.
            // CallerMemberName fills in the property name automatically in most calls.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

