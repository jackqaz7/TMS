using CoreAPI.Models;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TMS_WPF_UI.Helpers;

namespace TMS_WPF_UI.ViewModel
{
    public class Login : INotifyPropertyChanged
    {
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _message = string.Empty;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ICommand LoginCommand { get; }

        public Login()
        {
            LoginCommand = new RelayCommand(async _ => await LoginAsync());
        }

        private async Task LoginAsync()
        {
            using var client = new HttpClient { BaseAddress = new Uri("https://localhost:7104/api/") };
            var loginDto = new LoginDto { Username = Username, Password = Password };

            var response = await client.PostAsJsonAsync("users/login", loginDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (result == null || string.IsNullOrWhiteSpace(result.Token))
                {
                    Message = "Login succeeded, but the API did not return a token.";
                    return;
                }

                // Store the token once after login. Other WPF screens use it as the Bearer
                // token when calling protected API endpoints such as /api/treasury/positions.
                SessionManager.JwtToken = result.Token;
                Message = "Login successful!";

                var home = new MainWindow();
                home.Show();
                Application.Current.Windows[0]?.Close();
            }
            else
            {
                Message = "Invalid username or password.";
            }
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}
