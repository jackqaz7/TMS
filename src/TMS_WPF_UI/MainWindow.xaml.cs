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

            // DataContext is the binding source for this window. XAML expressions such as
            // {Binding Positions} and {Binding RefreshPositionsCommand} resolve here.
            DataContext = _dashboard;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Window_Loaded is an event handler, so async void is acceptable here. For normal
            // methods prefer async Task, as used by Dashboard.LoadPositionsAsync.
            await _dashboard.LoadPositionsAsync();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            // Clearing the token removes the client-side login session. The next protected
            // API call will fail unless the user logs in again and receives a new JWT.
            SessionManager.JwtToken = null;
            var loginWindow = new Login();
            loginWindow.Show();
            this.Close();
        }
    }
}
