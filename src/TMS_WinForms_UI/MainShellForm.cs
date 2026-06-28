using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Microsoft.AspNetCore.SignalR.Client;
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
        private HubConnection? _notificationsConnection;
        private ElementHost? _currentHost;

        public MainShellForm()
        {
            // Shell pattern: this WinForms form owns the app window, navigation, and
            // lifetime. Individual screens are swapped into _contentPanel.
            Text = "TMS Hybrid Shell";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 1120;
            Height = 720;
            MinimumSize = new System.Drawing.Size(980, 620);

            BuildLayout();
            ShowDashboard();
            Shown += async (_, _) => await StartNotificationsAsync();
            FormClosed += async (_, _) => await StopNotificationsAsync();
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
            _statusLabel.AutoEllipsis = true;
            _statusLabel.ForeColor = System.Drawing.Color.DarkSlateGray;
            _statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            // still renders this DashboardControl and uses its normal MVVM data binding.
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
            SetShellStatus("Queueing 50 log events...", false);

            try
            {
                using var client = CreateAuthorizedClient();

                // One click produces 50 audit events on the API. If the bounded
                // Channel<T> queue fills, this call slows down instead of dropping work.
                var response = await client.PostAsync("audit-log-test/button-click", content: null);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    SetShellStatus($"{(int)response.StatusCode}: {body}", true);
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<AuditLogButtonResponse>();

                SetShellStatus(result == null
                    ? "Queued 50 log events."
                    : $"Queued {result.EventsQueued} events in {result.EnqueueElapsedMilliseconds} ms.",
                    false);
            }
            catch (Exception ex)
            {
                SetShellStatus($"Log failed: {ex.Message}", true);
            }
            finally
            {
                _logButton.Enabled = true;
            }
        }

        private async Task StartNotificationsAsync()
        {
            if (_notificationsConnection != null)
            {
                return;
            }

            _notificationsConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7104/hubs/notifications", options =>
                {
                    // SignalR concept: the hub keeps a live connection open for server
                    // notifications. It uses the same JWT auth as normal CoreAPI calls.
                    options.AccessTokenProvider = () => Task.FromResult<string?>(SessionManager.JwtToken);
                })
                .WithAutomaticReconnect()
                .Build();

            _notificationsConnection.On<ReconciliationCompletedNotification>(
                "ReconciliationCompleted",
                notification =>
                {
                    SetShellStatus(
                        $"Reconciliation completed: {notification.MatchedGroupCount} matched, " +
                        $"{notification.BreakGroupCount} break(s), {notification.ElapsedMilliseconds} ms.",
                        notification.BreakGroupCount > 0);
                });

            _notificationsConnection.Reconnecting += _ =>
            {
                SetShellStatus("Notification connection reconnecting...", true);
                return Task.CompletedTask;
            };

            _notificationsConnection.Reconnected += _ =>
            {
                SetShellStatus("Notification connection restored.", false);
                return Task.CompletedTask;
            };

            _notificationsConnection.Closed += _ =>
            {
                SetShellStatus("Notification connection closed.", true);
                return Task.CompletedTask;
            };

            try
            {
                await _notificationsConnection.StartAsync();
                SetShellStatus("Notifications connected.", false);
            }
            catch (Exception ex)
            {
                SetShellStatus($"Notifications unavailable: {ex.Message}", true);
                await _notificationsConnection.DisposeAsync();
                _notificationsConnection = null;
            }
        }

        private async Task StopNotificationsAsync()
        {
            if (_notificationsConnection == null)
            {
                return;
            }

            await _notificationsConnection.DisposeAsync();
            _notificationsConnection = null;
        }

        private void SetShellStatus(string message, bool isWarning)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetShellStatus(message, isWarning)));
                return;
            }

            _statusLabel.ForeColor = isWarning
                ? System.Drawing.Color.Firebrick
                : System.Drawing.Color.DarkGreen;
            _statusLabel.Text = message;
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

        private sealed class ReconciliationCompletedNotification
        {
            public Guid BatchId { get; set; }
            public int MatchedGroupCount { get; set; }
            public int BreakGroupCount { get; set; }
            public long ElapsedMilliseconds { get; set; }
        }
    }
}
