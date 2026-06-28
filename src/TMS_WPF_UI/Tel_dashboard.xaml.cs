using System.Windows;
using TMS_WPF_UI.Helpers;
using TMS_WPF_UI.ViewModel;

namespace TMS_WPF_UI
{
    /// <summary>
    /// Interaction logic for Tel_dashboard.xaml
    /// </summary>
    public partial class Tel_dashboard : Window
    {
        private readonly Dashboard _dashboard;

        public Tel_dashboard()
        {
            InitializeComponent();

            _dashboard = new Dashboard();

            // This deliberately reuses the same view model as WPF_Dashboard. Only the
            // visual controls change, which makes the WPF vs Telerik comparison fair.
            DataContext = _dashboard;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Telerik's RadBusyIndicator binds to IsLoading, so the loading state is still
            // driven by the same async API call used by the native WPF dashboard.
            await _dashboard.LoadPositionsAsync();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            // Keep logout behavior identical to the native dashboard.
            SessionManager.JwtToken = null;
            var loginWindow = new Login();
            loginWindow.Show();
            Close();
        }

        private void NewTrade_Click(object sender, RoutedEventArgs e)
        {
            var tradeWindow = new CreateTrade();
            tradeWindow.Owner = this;
            tradeWindow.ShowDialog();
        }
    }
}
