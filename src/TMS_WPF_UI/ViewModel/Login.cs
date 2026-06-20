using CoreAPI.Models;
using System;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TMS_WPF_UI.Helpers;


namespace TMS_WPF_UI.ViewModel
{
    public class Login : INotifyPropertyChanged
    {
        private string _username;
        private string _password;
        private string _Message;

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
            get => _Message;
            set { _Message = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
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
                Message = "Login successful!";
                SessionManager.JwtToken = result.Token;
                var home = new MainWindow();
                home.Show();
                Application.Current.Windows[0].Close(); // close Login window

                //Properties.Settings.Default.JwtToken = result.Token;
                //Properties.Settings.Default.Save();
            }
            else
            {
                Message = "Invalid username or password.";
            }
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
}
