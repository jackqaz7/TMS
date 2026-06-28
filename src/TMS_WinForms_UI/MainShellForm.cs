using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using TMS_WPF_UI;
using TMS_WPF_UI.Helpers;

namespace TMS_WinForms_UI
{
    public class MainShellForm : Form
    {
        private const string CoreApiBaseAddress = "https://localhost:7104/api/";

        private readonly Panel _contentPanel = new();
        private readonly Label _statusLabel = new();
        private readonly Button _logButton = new();
        private ElementHost? _currentHost;

        public MainShellForm()
        {
            Text = "TMS Hybrid Shell";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 1120;
            Height = 720;
            MinimumSize = new System.Drawing.Size(980, 620);

            BuildLayout();
            ShowDashboard();
        }

        private void BuildLayout()
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = System.Drawing.Color.FromArgb(179, 229, 252),
                Padding = new Padding(16)
            };

            var title = new Label
            {
                Text = "TMS",
                Dock = DockStyle.Top,
                Height = 48,
                Font = new System.Drawing.Font(Font.FontFamily, 18, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White
            };

            var dashboardButton = new Button
            {
                Text = "Positions Dashboard",
                Dock = DockStyle.Top,
                Height = 36
            };
            dashboardButton.Click += (_, _) => ShowDashboard();

            var usersButton = new Button
            {
                Text = "Users and Dealers",
                Dock = DockStyle.Top,
                Height = 36
            };
            usersButton.Click += (_, _) => ShowUsersAndDealers();

            var reconciliationButton = new Button
            {
                Text = "Reconciliation",
                Dock = DockStyle.Top,
                Height = 36
            };
            reconciliationButton.Click += (_, _) => ShowReconciliation();

            _logButton.Text = "Log";
            _logButton.Dock = DockStyle.Top;
            _logButton.Height = 36;
            _logButton.Click += async (_, _) => await GenerateAuditLogEventsAsync();

            _statusLabel.Dock = DockStyle.Bottom;
            _statusLabel.Height = 72;
            _statusLabel.ForeColor = System.Drawing.Color.DarkSlateGray;
            _statusLabel.Text = "Ready";

            var logoutButton = new Button
            {
                Text = "Logout",
                Dock = DockStyle.Bottom,
                Height = 36
            };
            logoutButton.Click += (_, _) => Logout();

            sidebar.Controls.Add(logoutButton);
            sidebar.Controls.Add(_statusLabel);
            sidebar.Controls.Add(_logButton);
            sidebar.Controls.Add(reconciliationButton);
            sidebar.Controls.Add(usersButton);
            sidebar.Controls.Add(dashboardButton);
            sidebar.Controls.Add(title);

            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = System.Drawing.Color.White;

            Controls.Add(_contentPanel);
            Controls.Add(sidebar);
        }

        private void ShowDashboard()
        {
            _currentHost?.Dispose();
            _contentPanel.Controls.Clear();

            // ElementHost is the interop bridge: WinForms owns the shell, while WPF
            // still renders this DashboardControl and uses its normal MVVM binding.
            _currentHost = new ElementHost
            {
                Dock = DockStyle.Fill,
                Child = new DashboardControl()
            };

            _contentPanel.Controls.Add(_currentHost);
        }

        private void ShowUsersAndDealers()
        {
            _currentHost?.Dispose();
            _currentHost = null;
            _contentPanel.Controls.Clear();

            // This screen is plain WinForms because it is a simple CRUD/admin form.
            // It still goes through CoreAPI so API validation remains authoritative.
            _contentPanel.Controls.Add(new UserDealerForm());
        }

        private void ShowReconciliation()
        {
            _currentHost?.Dispose();
            _currentHost = null;
            _contentPanel.Controls.Clear();

            // Reconciliation is an operational WinForms screen. The UI uses async/await
            // while CoreAPI performs database reads and parallel batch processing.
            _contentPanel.Controls.Add(new ReconciliationForm());
        }

        private async Task GenerateAuditLogEventsAsync()
        {
            _logButton.Enabled = false;
            _statusLabel.Text = "Queueing 50 log events...";

            try
            {
                using var client = CreateAuthorizedClient();

                // One click produces 50 audit events on the API. If the bounded
                // Channel<T> queue fills, this call slows down instead of dropping work.
                var response = await client.PostAsync("audit-log-test/button-click", content: null);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _statusLabel.ForeColor = System.Drawing.Color.Firebrick;
                    _statusLabel.Text = $"{(int)response.StatusCode}: {body}";
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<AuditLogButtonResponse>();

                _statusLabel.ForeColor = System.Drawing.Color.DarkGreen;
                _statusLabel.Text = result == null
                    ? "Queued 50 log events."
                    : $"Queued {result.EventsQueued} events in {result.EnqueueElapsedMilliseconds} ms.";
            }
            catch (Exception ex)
            {
                _statusLabel.ForeColor = System.Drawing.Color.Firebrick;
                _statusLabel.Text = $"Log failed: {ex.Message}";
            }
            finally
            {
                _logButton.Enabled = true;
            }
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = new HttpClient { BaseAddress = new System.Uri(CoreApiBaseAddress) };
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);

            return client;
        }

        private void Logout()
        {
            SessionManager.JwtToken = null;
            var login = new LoginForm();
            login.Show();
            Close();
        }

        private sealed class AuditLogButtonResponse
        {
            public int EventsQueued { get; set; }
            public long EnqueueElapsedMilliseconds { get; set; }
        }
    }
}
