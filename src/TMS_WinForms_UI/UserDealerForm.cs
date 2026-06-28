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
            root.Controls.Add(BuildGroupBox("Existing users", _usersGrid, new Padding(0, 0, 16, 0)), 0, 1);

            root.Controls.Add(BuildGroupBox("User details", BuildEditorPanel(), Padding.Empty), 1, 1);

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
            _usersGrid.BackgroundColor = System.Drawing.Color.White;
            _usersGrid.BorderStyle = BorderStyle.None;
            _usersGrid.AutoGenerateColumns = false;
            _usersGrid.AllowUserToAddRows = false;
            _usersGrid.AllowUserToDeleteRows = false;
            _usersGrid.ReadOnly = true;
            _usersGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _usersGrid.MultiSelect = false;
            _usersGrid.RowHeadersVisible = false;
            _usersGrid.AllowUserToResizeRows = false;
            _usersGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _usersGrid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);

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
                Padding = new Padding(8),
                ColumnCount = 2,
                RowCount = 6
            };

            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            editor.Controls.Add(CreateFieldLabel("Id"), 0, 0);
            _idTextBox.ReadOnly = true;
            _idTextBox.Dock = DockStyle.Fill;
            editor.Controls.Add(_idTextBox, 1, 0);

            editor.Controls.Add(CreateFieldLabel("Username"), 0, 1);
            _usernameTextBox.Dock = DockStyle.Fill;
            _usernameTextBox.Margin = new Padding(0, 8, 0, 0);
            editor.Controls.Add(_usernameTextBox, 1, 1);

            editor.Controls.Add(CreateFieldLabel("Password"), 0, 2);
            _passwordTextBox.Dock = DockStyle.Fill;
            _passwordTextBox.Margin = new Padding(0, 8, 0, 0);
            _passwordTextBox.UseSystemPasswordChar = true;
            editor.Controls.Add(_passwordTextBox, 1, 2);

            editor.Controls.Add(CreateFieldLabel("Role"), 0, 3);
            _roleComboBox.Dock = DockStyle.Fill;
            _roleComboBox.Margin = new Padding(0, 8, 0, 0);
            _roleComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _roleComboBox.Items.AddRange(new object[] { "Admin", "Dealer", "Viewer" });
            editor.Controls.Add(_roleComboBox, 1, 3);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true,
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
            editor.Controls.Add(buttonPanel, 1, 4);

            var note = new Label
            {
                Text = "Leave password blank when updating to keep the current password.",
                Dock = DockStyle.Top,
                ForeColor = System.Drawing.Color.DimGray,
                Margin = new Padding(0, 12, 0, 0)
            };
            editor.Controls.Add(note, 1, 5);

            return editor;
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 10, 12, 0)
            };
        }

        private static Control BuildGroupBox(string title, Control child, Padding margin)
        {
            var groupBox = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Margin = margin
            };

            child.Dock = DockStyle.Fill;
            groupBox.Controls.Add(child);

            return groupBox;
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
