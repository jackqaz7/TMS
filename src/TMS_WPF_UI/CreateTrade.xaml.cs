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
    }
}
