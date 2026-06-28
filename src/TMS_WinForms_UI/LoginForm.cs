using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreAPI.Models;
using TMS_WPF_UI.Helpers;

namespace TMS_WinForms_UI
{
    public class LoginForm : Form
    {
        private const string CoreApiBaseAddress = "https://localhost:7104/api/";

        private readonly TextBox _usernameTextBox = new();
        private readonly TextBox _passwordTextBox = new();
        private readonly Button _loginButton = new();
        private readonly Label _messageLabel = new();
        private readonly ProgressBar _progressBar = new();

        public LoginForm()
        {
            Text = "TMS Login";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 420;
            Height = 260;
            MinimumSize = new System.Drawing.Size(420, 260);

            BuildLayout();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 2,
                RowCount = 5
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var title = new Label
            {
                Text = "Treasury Management System",
                AutoSize = true,
                Font = new System.Drawing.Font(Font.FontFamily, 12, System.Drawing.FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 18)
            };

            root.Controls.Add(title, 0, 0);
            root.SetColumnSpan(title, 2);

            root.Controls.Add(new Label { Text = "Username", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            _usernameTextBox.Dock = DockStyle.Fill;
            root.Controls.Add(_usernameTextBox, 1, 1);

            root.Controls.Add(new Label { Text = "Password", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            _passwordTextBox.Dock = DockStyle.Fill;
            _passwordTextBox.UseSystemPasswordChar = true;
            root.Controls.Add(_passwordTextBox, 1, 2);

            _loginButton.Text = "Login";
            _loginButton.Width = 90;
            _loginButton.Margin = new Padding(0, 14, 0, 0);
            _loginButton.Click += async (_, _) => await LoginAsync();
            root.Controls.Add(_loginButton, 1, 3);

            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.Visible = false;
            _progressBar.Dock = DockStyle.Fill;
            _progressBar.Margin = new Padding(0, 10, 0, 0);
            root.Controls.Add(_progressBar, 1, 4);

            _messageLabel.ForeColor = System.Drawing.Color.Firebrick;
            _messageLabel.AutoSize = true;
            _messageLabel.Margin = new Padding(0, 10, 0, 0);
            root.Controls.Add(_messageLabel, 0, 4);

            Controls.Add(root);
        }

        private async Task LoginAsync()
        {
            SetBusy(true, "Signing in...");

            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(CoreApiBaseAddress) };
                var loginDto = new LoginDto
                {
                    Username = _usernameTextBox.Text,
                    Password = _passwordTextBox.Text
                };

                // WinForms login calls the CoreAPI login endpoint directly.
                // The JWT is stored in the shared SessionManager for WPF hosted controls.
                var response = await client.PostAsJsonAsync("users/login", loginDto);

                if (!response.IsSuccessStatusCode)
                {
                    SetBusy(false, "Invalid username or password.");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (result == null || string.IsNullOrWhiteSpace(result.Token))
                {
                    SetBusy(false, "Login succeeded, but the API did not return a token.");
                    return;
                }

                SessionManager.JwtToken = result.Token;

                var shell = new MainShellForm();
                shell.FormClosed += (_, _) => Close();
                shell.Show();
                Hide();
            }
            catch (Exception ex)
            {
                SetBusy(false, $"Login failed: {ex.Message}");
            }
        }

        private void SetBusy(bool isBusy, string message)
        {
            _loginButton.Enabled = !isBusy;
            _progressBar.Visible = isBusy;
            _messageLabel.Text = message;
        }

        private sealed class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public DateTime Expiration { get; set; }
        }
    }
}
