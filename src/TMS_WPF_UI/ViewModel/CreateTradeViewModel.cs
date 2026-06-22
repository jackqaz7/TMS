using CoreAPI.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TMS_WPF_UI.Helpers;

namespace TMS_WPF_UI.ViewModel
{
    public class CreateTradeViewModel : INotifyPropertyChanged
    {
        private const string TreasuryApiBaseAddress = "https://localhost:7104/api/";
        private string _message = string.Empty;
        private bool _isSaving;

        public ObservableCollection<string> TradeTypes { get; } = new() { "FX_SPOT", "FX_FORWARD", "FX_SWAP" };
        public ObservableCollection<string> Sides { get; } = new() { "BUY", "SELL" };
        public ObservableCollection<string> Currencies { get; } = new() { "USD", "INR", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD", "SGD", "AED" };

        public CreateTradeRequest Trade { get; private set; } = CreateDefaultTrade();

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set { _isSaving = value; OnPropertyChanged(); }
        }

        public bool IsForwardOrSwap => Trade.TradeType == "FX_FORWARD" || Trade.TradeType == "FX_SWAP";
        public bool IsSwap => Trade.TradeType == "FX_SWAP";

        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler? RequestClose;
        public event PropertyChangedEventHandler? PropertyChanged;

        public CreateTradeViewModel()
        {
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsSaving);
            ResetCommand = new RelayCommand(_ => Reset());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty));
        }

        public void OnTradeTypeChanged()
        {
            OnPropertyChanged(nameof(IsForwardOrSwap));
            OnPropertyChanged(nameof(IsSwap));
        }

        private async Task SaveAsync()
        {
            var validationMessage = Validate();

            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                Message = validationMessage;
                return;
            }

            if (string.IsNullOrWhiteSpace(SessionManager.JwtToken))
            {
                Message = "Login token is missing. Please login again.";
                return;
            }

            IsSaving = true;
            Message = "Saving trade...";

            try
            {
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
                Message = $"Trade save failed: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        private string Validate()
        {
            if (string.IsNullOrWhiteSpace(Trade.TradeReference)) return "Trade reference is required.";
            if (string.IsNullOrWhiteSpace(Trade.Counterparty)) return "Counterparty is required.";
            if (Trade.Currency1.Length != 3 || Trade.Currency2.Length != 3) return "Both currencies are required.";
            if (Trade.Currency1 == Trade.Currency2) return "Currency 1 and Currency 2 cannot be same.";
            if (Trade.Amount1 <= 0) return "Amount 1 must be greater than zero.";
            if (Trade.Amount2 <= 0) return "Amount 2 must be greater than zero.";
            if (Trade.FxRateUsed <= 0) return "FX rate must be greater than zero.";
            if (Trade.SettlementDate < Trade.TradeDate) return "Settlement date cannot be before trade date.";

            if (Trade.TradeType == "FX_SWAP")
            {
                if (Trade.NearLegDate == null || Trade.FarLegDate == null) return "FX swap requires near and far leg dates.";
                if (Trade.NearLegRate == null || Trade.FarLegRate == null) return "FX swap requires near and far leg rates.";
            }

            return string.Empty;
        }

        private void Reset()
        {
            Trade = CreateDefaultTrade();
            Message = string.Empty;
            OnPropertyChanged(nameof(Trade));
            OnTradeTypeChanged();
        }

        private static CreateTradeRequest CreateDefaultTrade()
        {
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
