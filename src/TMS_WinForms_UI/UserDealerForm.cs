using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreAPI.Models;
using TMS_WPF_UI.Helpers;

namespace TMS_WinForms_UI
{
    public class UserDealerForm : UserControl
    {
        private const string CoreApiBaseAddress = "https://localhost:7104/api/";

        private readonly DataGridView _usersGrid = new();
        private readonly TextBox _idTextBox = new();
        private readonly TextBox _usernameTextBox = new();
        private readonly TextBox _passwordTextBox = new();
        private readonly ComboBox _roleComboBox = new();
        private readonly Label _messageLabel = new();
        private readonly Button _loadButton = new();
        private readonly Button _addButton = new();
        private readonly Button _updateButton = new();
        private readonly Button _deleteButton = new();
        private readonly Button _clearButton = new();

        public UserDealerForm()
        {
            Dock = DockStyle.Fill;
            BuildLayout();
            _ = LoadUsersAsync();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 2,
                RowCount = 3
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var title = new Label
            {
                Text = "Users and Dealers",
                AutoSize = true,
                Font = new System.Drawing.Font(Font.FontFamily, 16, System.Drawing.FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 16)
            };

            root.Controls.Add(title, 0, 0);
            root.SetColumnSpan(title, 2);

            ConfigureGrid();
            root.Controls.Add(_usersGrid, 0, 1);

            root.Controls.Add(BuildEditorPanel(), 1, 1);

            _messageLabel.AutoSize = true;
            _messageLabel.ForeColor = System.Drawing.Color.Firebrick;
            _messageLabel.Margin = new Padding(0, 12, 0, 0);
            root.Controls.Add(_messageLabel, 0, 2);
            root.SetColumnSpan(_messageLabel, 2);

            Controls.Add(root);
        }

        private void ConfigureGrid()
        {
            _usersGrid.Dock = DockStyle.Fill;
            _usersGrid.AutoGenerateColumns = false;
            _usersGrid.AllowUserToAddRows = false;
            _usersGrid.AllowUserToDeleteRows = false;
            _usersGrid.ReadOnly = true;
            _usersGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _usersGrid.MultiSelect = false;
            _usersGrid.RowHeadersVisible = false;

            _usersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Id",
                DataPropertyName = nameof(UserAdminResponse.Id),
                Width = 60
            });

            _usersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Username",
                DataPropertyName = nameof(UserAdminResponse.Username),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            _usersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Role",
                DataPropertyName = nameof(UserAdminResponse.Role),
                Width = 140
            });

            _usersGrid.SelectionChanged += (_, _) => PopulateEditorFromSelection();
        }

        private Control BuildEditorPanel()
        {
            var editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 0, 0, 0),
                ColumnCount = 1,
                RowCount = 11
            };

            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            editor.Controls.Add(new Label { Text = "Id", AutoSize = true });
            _idTextBox.ReadOnly = true;
            _idTextBox.Dock = DockStyle.Top;
            editor.Controls.Add(_idTextBox);

            editor.Controls.Add(new Label { Text = "Username", AutoSize = true, Margin = new Padding(0, 12, 0, 0) });
            _usernameTextBox.Dock = DockStyle.Top;
            editor.Controls.Add(_usernameTextBox);

            editor.Controls.Add(new Label { Text = "Password", AutoSize = true, Margin = new Padding(0, 12, 0, 0) });
            _passwordTextBox.Dock = DockStyle.Top;
            _passwordTextBox.UseSystemPasswordChar = true;
            editor.Controls.Add(_passwordTextBox);

            editor.Controls.Add(new Label { Text = "Role", AutoSize = true, Margin = new Padding(0, 12, 0, 0) });
            _roleComboBox.Dock = DockStyle.Top;
            _roleComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            _roleComboBox.Items.AddRange(new object[] { "Admin", "Dealer", "Viewer" });
            editor.Controls.Add(_roleComboBox);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 16, 0, 0)
            };

            ConfigureButton(_loadButton, "Load", async () => await LoadUsersAsync());
            ConfigureButton(_addButton, "Add", async () => await AddUserAsync());
            ConfigureButton(_updateButton, "Update", async () => await UpdateUserAsync());
            ConfigureButton(_deleteButton, "Delete", async () => await DeleteUserAsync());
            _clearButton.Text = "Clear";
            _clearButton.Width = 72;
            _clearButton.Click += (_, _) => ClearEditor();

            buttonPanel.Controls.Add(_loadButton);
            buttonPanel.Controls.Add(_addButton);
            buttonPanel.Controls.Add(_updateButton);
            buttonPanel.Controls.Add(_deleteButton);
            buttonPanel.Controls.Add(_clearButton);
            editor.Controls.Add(buttonPanel);

            var note = new Label
            {
                Text = "Leave password blank when updating to keep the current password.",
                AutoSize = true,
                ForeColor = System.Drawing.Color.DimGray,
                Margin = new Padding(0, 12, 0, 0)
            };
            editor.Controls.Add(note);

            return editor;
        }

        private static void ConfigureButton(Button button, string text, Func<Task> action)
        {
            button.Text = text;
            button.Width = 72;
            button.Click += async (_, _) => await action();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var users = await client.GetFromJsonAsync<List<UserAdminResponse>>("users")
                    ?? new List<UserAdminResponse>();

                _usersGrid.DataSource = users;
                SetMessage($"Loaded {users.Count} user(s).", false);
            }
            catch (Exception ex)
            {
                SetMessage($"Load failed: {ex.Message}", true);
            }
        }

        private async Task AddUserAsync()
        {
            var request = new CreateUserAdminRequest
            {
                Username = _usernameTextBox.Text.Trim(),
                Password = _passwordTextBox.Text,
                Role = (_roleComboBox.Text ?? string.Empty).Trim()
            };

            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                SetMessage("Username, password, and role are required.", true);
                return;
            }

            await SendAndReloadAsync(client => client.PostAsJsonAsync("users", request), "User added.");
        }

        private async Task UpdateUserAsync()
        {
            if (!int.TryParse(_idTextBox.Text, out var id))
            {
                SetMessage("Select a user to update.", true);
                return;
            }

            var request = new UpdateUserAdminRequest
            {
                Username = _usernameTextBox.Text.Trim(),
                Password = string.IsNullOrWhiteSpace(_passwordTextBox.Text) ? null : _passwordTextBox.Text,
                Role = (_roleComboBox.Text ?? string.Empty).Trim()
            };

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Role))
            {
                SetMessage("Username and role are required.", true);
                return;
            }

            await SendAndReloadAsync(client => client.PutAsJsonAsync($"users/{id}", request), "User updated.");
        }

        private async Task DeleteUserAsync()
        {
            if (!int.TryParse(_idTextBox.Text, out var id))
            {
                SetMessage("Select a user to delete.", true);
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete user '{_usernameTextBox.Text}'?",
                "Confirm delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            await SendAndReloadAsync(client => client.DeleteAsync($"users/{id}"), "User deleted.");
        }

        private async Task SendAndReloadAsync(
            Func<HttpClient, Task<HttpResponseMessage>> sendAsync,
            string successMessage)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await sendAsync(client);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    SetMessage($"{(int)response.StatusCode} {response.ReasonPhrase}: {body}", true);
                    return;
                }

                ClearEditor();
                await LoadUsersAsync();
                SetMessage(successMessage, false);
            }
            catch (Exception ex)
            {
                SetMessage($"Save failed: {ex.Message}", true);
            }
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(CoreApiBaseAddress) };

            // All user admin endpoints are protected. LoginForm stores the JWT here
            // after CoreAPI authenticates the user.
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);

            return client;
        }

        private void PopulateEditorFromSelection()
        {
            if (_usersGrid.SelectedRows.Count == 0 ||
                _usersGrid.SelectedRows[0].DataBoundItem is not UserAdminResponse user)
            {
                return;
            }

            _idTextBox.Text = user.Id.ToString();
            _usernameTextBox.Text = user.Username;
            _passwordTextBox.Text = string.Empty;
            _roleComboBox.Text = user.Role;
            SetMessage("Selected user. Enter a password only if you want to change it.", false);
        }

        private void ClearEditor()
        {
            _idTextBox.Clear();
            _usernameTextBox.Clear();
            _passwordTextBox.Clear();
            _roleComboBox.Text = string.Empty;
        }

        private void SetMessage(string message, bool isError)
        {
            _messageLabel.ForeColor = isError
                ? System.Drawing.Color.Firebrick
                : System.Drawing.Color.DarkGreen;
            _messageLabel.Text = message;
        }
    }
}
