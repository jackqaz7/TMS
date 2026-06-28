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
        private readonly ToolStripStatusLabel _statusLabel = new();
        private readonly ToolStripProgressBar _statusProgressBar = new();
        private readonly Panel _loginPanel = new();

        public LoginForm()
        {
            Text = "TMS Login";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new System.Drawing.Size(820, 500);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            AcceptButton = _loginButton;

            BuildLayout();
        }

        private void BuildLayout()
        {
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(36),
                BackColor = System.Drawing.Color.White
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var title = new Label
            {
                Text = "Treasury Management System",
                AutoSize = true,
                Font = new System.Drawing.Font(Font.FontFamily, 18, System.Drawing.FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };

            root.Controls.Add(title, 0, 0);
            root.SetColumnSpan(title, 2);

            var subtitle = new Label
            {
                Text = "Sign in with your CoreAPI user account.",
                AutoSize = true,
                Font = new System.Drawing.Font(Font.FontFamily, 10),
                ForeColor = System.Drawing.Color.DimGray,
                Margin = new Padding(0, 0, 0, 24)
            };
            root.Controls.Add(subtitle, 0, 1);
            root.SetColumnSpan(subtitle, 2);

            root.Controls.Add(new Label { Text = "Username", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            _usernameTextBox.Dock = DockStyle.Fill;
            _usernameTextBox.Font = new System.Drawing.Font(Font.FontFamily, 11);
            _usernameTextBox.Margin = new Padding(0, 0, 0, 14);
            root.Controls.Add(_usernameTextBox, 1, 2);

            root.Controls.Add(new Label { Text = "Password", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            _passwordTextBox.Dock = DockStyle.Fill;
            _passwordTextBox.Font = new System.Drawing.Font(Font.FontFamily, 11);
            _passwordTextBox.UseSystemPasswordChar = true;
            root.Controls.Add(_passwordTextBox, 1, 3);

            _loginButton.Text = "Login";
            _loginButton.Width = 132;
            _loginButton.Height = 38;
            _loginButton.Font = new System.Drawing.Font(Font.FontFamily, 10);
            _loginButton.Margin = new Padding(0, 24, 0, 0);
            _loginButton.Click += async (_, _) => await LoginAsync();
            root.Controls.Add(_loginButton, 1, 4);

            var statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                SizingGrip = false
            };

            _statusLabel.Text = "Ready";
            _statusLabel.Spring = true;
            _statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            _statusProgressBar.Style = ProgressBarStyle.Marquee;
            _statusProgressBar.Visible = false;
            _statusProgressBar.Width = 140;

            statusStrip.Items.Add(_statusLabel);
            statusStrip.Items.Add(_statusProgressBar);

            _loginPanel.Width = 700;
            _loginPanel.Height = 290;
            _loginPanel.Controls.Add(root);

            // Keep the login controls visually grouped instead of letting the
            // TableLayoutPanel stretch tiny default controls across a large window.
            contentPanel.Controls.Add(_loginPanel);
            contentPanel.Resize += (_, _) => CenterLoginPanel(contentPanel);
            CenterLoginPanel(contentPanel);

            Controls.Add(contentPanel);
            Controls.Add(statusStrip);
        }

        private void CenterLoginPanel(Control parent)
        {
            var x = Math.Max(parent.Padding.Left, (parent.ClientSize.Width - _loginPanel.Width) / 2);
            var y = Math.Max(parent.Padding.Top, (parent.ClientSize.Height - _loginPanel.Height) / 2);
            _loginPanel.Location = new System.Drawing.Point(x, y);
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(_usernameTextBox.Text) ||
                string.IsNullOrWhiteSpace(_passwordTextBox.Text))
            {
                SetBusy(false, "Username and password are required.");
                return;
            }

            SetBusy(true, "Signing in...");

            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(CoreApiBaseAddress) };
                var loginDto = new LoginDto
                {
                    Username = _usernameTextBox.Text,
                    Password = _passwordTextBox.Text
                };

                // async/await + REST concept: this HTTP call goes to CoreAPI without
                // blocking the WinForms UI thread. CoreAPI returns a JWT on success.
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

                // Simple session state concept: the JWT is stored once and reused by
                // WinForms screens and hosted WPF controls for authenticated API calls.
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
            _usernameTextBox.Enabled = !isBusy;
            _passwordTextBox.Enabled = !isBusy;
            _statusProgressBar.Visible = isBusy;
            _statusLabel.ForeColor = isBusy ? System.Drawing.Color.DarkSlateGray : System.Drawing.Color.Firebrick;
            _statusLabel.Text = message;
        }

        private sealed class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public DateTime Expiration { get; set; }
        }
    }
}
