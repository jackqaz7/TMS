using System.Windows;
using TMS_WPF_UI.ViewModel;

namespace TMS_WPF_UI
{
    public partial class CreateTrade : Window
    {
        private readonly CreateTradeViewModel _viewModel;

        public CreateTrade()
        {
            InitializeComponent();

            _viewModel = new CreateTradeViewModel();
            _viewModel.RequestClose += (_, _) => Close();
            DataContext = _viewModel;
        }

        private void TradeType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _viewModel.OnTradeTypeChanged();
        }

        private async void FxInput_Changed(object sender, RoutedEventArgs e)
        {
            // The DTO bound to the form is a simple object, so we trigger FX lookup
            // from the view when important inputs change. The actual calculation logic
            // remains in the ViewModel to keep the code testable and MVVM-friendly.
            await _viewModel.RecalculateFxAmountAsync();
        }
    }
}
