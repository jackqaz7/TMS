using System.Windows;
using System.Windows.Controls;

namespace TMS_WPF_UI
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();

            // The view model owns login state and commands. The XAML text boxes bind to it.
            DataContext = new ViewModel.Login();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModel.Login vm)
            {
                // PasswordBox.Password is not a normal bindable dependency property for
                // security reasons, so this event manually copies it into the view model.
                vm.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
