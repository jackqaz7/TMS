using System.Windows;
using System.Windows.Controls;
using TMS_WPF_UI.ViewModel;

namespace TMS_WPF_UI
{
    public partial class DashboardControl : UserControl
    {
        private readonly Dashboard _dashboard;
        private bool _hasLoadedPositions;

        public DashboardControl()
        {
            InitializeComponent();

            _dashboard = new Dashboard();

            // A WPF UserControl can use the same MVVM binding pattern as a WPF Window.
            // WinForms only hosts this control; the WPF binding still resolves here.
            DataContext = _dashboard;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_hasLoadedPositions)
            {
                return;
            }

            _hasLoadedPositions = true;
            await _dashboard.LoadPositionsAsync();
        }

        private void NewTrade_Click(object sender, RoutedEventArgs e)
        {
            var tradeWindow = new CreateTrade();
            tradeWindow.ShowDialog();
        }

        private void TelerikDashboard_Click(object sender, RoutedEventArgs e)
        {
            var telerikDashboard = new Tel_dashboard();
            telerikDashboard.Show();
        }
    }
}
