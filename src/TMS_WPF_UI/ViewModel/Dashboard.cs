using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TMS_WPF_UI.Helpers;

namespace TMS_WPF_UI.ViewModel
{
    public class Dashboard : INotifyPropertyChanged
    {
        private const string TreasuryApiBaseAddress = "https://localhost:7104/api/";
        private string _statusMessage = "Positions not loaded yet.";
        private bool _isLoading;

        // ObservableCollection notifies WPF when rows are added/removed. That is why the
        // DataGrid refreshes after we clear and repopulate this collection from the API.
        public ObservableCollection<PositionSummary> Positions { get; } = new();

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // ICommand lets XAML buttons call view-model logic without putting business/API
        // code in the code-behind file.
        public ICommand RefreshPositionsCommand { get; }

        public Dashboard()
        {
            // The second lambda disables the Refresh command while a load is already running.
            RefreshPositionsCommand = new RelayCommand(async _ => await LoadPositionsAsync(), _ => !IsLoading);
        }

        public async Task LoadPositionsAsync()
        {
            if (string.IsNullOrWhiteSpace(SessionManager.JwtToken))
            {
                StatusMessage = "Login token is missing. Please login again.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading positions...";

            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(TreasuryApiBaseAddress) };

                // The API protects /api/treasury/* with [Authorize]. This header sends the
                // JWT received during login so the API can authenticate this WPF request.
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);

                // GetFromJsonAsync performs the HTTP GET and deserializes JSON into our
                // PositionSummary DTO. await keeps the WPF UI thread responsive.
                var positions = await client.GetFromJsonAsync<PositionSummary[]>("treasury/positions")
                    ?? Array.Empty<PositionSummary>();

                Positions.Clear();

                foreach (var position in positions)
                {
                    Positions.Add(position);
                }

                StatusMessage = positions.Length == 0
                    ? "No positions found. Capture a trade first."
                    : $"Loaded {positions.Length} currency position(s).";
            }
            catch (HttpRequestException ex)
            {
                StatusMessage = $"Could not reach Treasury API: {ex.Message}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load positions: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // PropertyChanged tells WPF to re-read a bound property, such as StatusMessage.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // This class mirrors the JSON returned by GET /api/treasury/positions.
    // Property names match the API response so System.Text.Json can bind automatically.
    public class PositionSummary
    {
        public string Currency { get; set; } = string.Empty;
        public decimal BuyNotional { get; set; }
        public decimal SellNotional { get; set; }
        public decimal NetNotional { get; set; }
        public decimal WeightedAverageRate { get; set; }
        public int TradeCount { get; set; }
    }
}
