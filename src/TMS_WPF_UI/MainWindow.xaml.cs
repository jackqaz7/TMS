using System.Windows;
using TMS_WPF_UI.Helpers;
using TMS_WPF_UI.ViewModel;

namespace TMS_WPF_UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dashboard _dashboard;

        public MainWindow()
        {
            InitializeComponent();

            _dashboard = new Dashboard();

            // DataContext is the object WPF binding expressions read from. In this screen,
            // bindings such as {Binding Positions} and {Binding RefreshPositionsCommand}
            // resolve against the Dashboard view model below.
            DataContext = _dashboard;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Loaded is a good moment for the first API call because the visual tree exists,
            // so bound controls can immediately render the returned collection/status text.
            await _dashboard.LoadPositionsAsync();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.JwtToken = null;
            var loginWindow = new Login();
            loginWindow.Show();
            this.Close();
        }
    }
}
