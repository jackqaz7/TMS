using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreAPI.Models;
using TMS_WPF_UI.Helpers;

namespace TMS_WinForms_UI
{
    public class ReconciliationForm : UserControl
    {
        private const string CoreApiBaseAddress = "https://localhost:7104/api/";

        private readonly DateTimePicker _fromDatePicker = new();
        private readonly DateTimePicker _toDatePicker = new();
        private readonly NumericUpDown _toleranceInput = new();
        private readonly NumericUpDown _batchSizeInput = new();
        private readonly NumericUpDown _parallelismInput = new();
        private readonly Button _runButton = new();
        private readonly Button _cancelButton = new();
        private readonly Button _loadSampleScenarioButton = new();
        private readonly ProgressBar _progressBar = new();
        private readonly Label _summaryLabel = new();
        private readonly DataGridView _ledgerGrid = new();
        private readonly DataGridView _resultsGrid = new();
        private readonly BindingList<LedgerEntryRow> _ledgerRows = new();
        private readonly BindingList<ReconciliationResultRow> _resultRows = new();
        private CancellationTokenSource? _runCancellation;

        public ReconciliationForm()
        {
            Dock = DockStyle.Fill;
            BuildLayout();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 1,
                RowCount = 5
            };

            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var title = new Label
            {
                Text = "Reconciliation",
                AutoSize = true,
                Font = new System.Drawing.Font(Font.FontFamily, 16, System.Drawing.FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6)
            };
            root.Controls.Add(title, 0, 0);

            root.Controls.Add(BuildSettingsPanel(), 0, 1);

            ConfigureLedgerGrid();
            root.Controls.Add(BuildGroupBox("Ledger entries to reconcile against trades", _ledgerGrid), 0, 2);

            ConfigureResultsGrid();
            root.Controls.Add(BuildGroupBox("Reconciliation results", _resultsGrid), 0, 3);

            _summaryLabel.AutoSize = true;
            _summaryLabel.ForeColor = System.Drawing.Color.DimGray;
            _summaryLabel.Margin = new Padding(0, 12, 0, 0);
            _summaryLabel.Text = "Enter ledger entries, then run reconciliation.";
            root.Controls.Add(_summaryLabel, 0, 4);

            Controls.Add(root);
        }

        private Control BuildSettingsPanel()
        {
            var groupBox = new GroupBox
            {
                Text = "Run settings",
                Dock = DockStyle.Top,
                Height = 86,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 12)
            };

            var settings = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true,
                Padding = new Padding(0, 2, 0, 0)
            };

            _fromDatePicker.Value = DateTime.Today.AddDays(-7);
            _toDatePicker.Value = DateTime.Today;

            ConfigureNumeric(_toleranceInput, 0, 1000000, 2, 0.01m);
            ConfigureNumeric(_batchSizeInput, 1, 500, 0, 100);
            ConfigureNumeric(_parallelismInput, 1, 16, 0, 4);

            _runButton.Text = "Run";
            _runButton.Width = 92;
            _runButton.Height = 30;
            _runButton.Margin = new Padding(8, 17, 4, 0);
            _runButton.Click += async (_, _) => await RunReconciliationAsync();

            _cancelButton.Text = "Cancel";
            _cancelButton.Width = 92;
            _cancelButton.Height = 30;
            _cancelButton.Margin = new Padding(4, 17, 4, 0);
            _cancelButton.Enabled = false;
            _cancelButton.Click += (_, _) => _runCancellation?.Cancel();

            _loadSampleScenarioButton.Text = "Load Sample";
            _loadSampleScenarioButton.Width = 116;
            _loadSampleScenarioButton.Height = 30;
            _loadSampleScenarioButton.Margin = new Padding(4, 17, 4, 0);
            _loadSampleScenarioButton.Click += (_, _) => LoadSampleScenario();

            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.Width = 140;
            _progressBar.Height = 22;
            _progressBar.Margin = new Padding(8, 21, 0, 0);
            _progressBar.MarqueeAnimationSpeed = 0;

            settings.Controls.Add(CreateLabeledControl("From", _fromDatePicker));
            settings.Controls.Add(CreateLabeledControl("To", _toDatePicker));
            settings.Controls.Add(CreateLabeledControl("Tolerance", _toleranceInput));
            settings.Controls.Add(CreateLabeledControl("Batch size", _batchSizeInput));
            settings.Controls.Add(CreateLabeledControl("Parallelism", _parallelismInput));
            settings.Controls.Add(_runButton);
            settings.Controls.Add(_cancelButton);
            settings.Controls.Add(_loadSampleScenarioButton);
            settings.Controls.Add(_progressBar);

            groupBox.Controls.Add(settings);
            return groupBox;
        }

        private static void ConfigureNumeric(
            NumericUpDown input,
            decimal minimum,
            decimal maximum,
            int decimalPlaces,
            decimal value)
        {
            input.Minimum = minimum;
            input.Maximum = maximum;
            input.DecimalPlaces = decimalPlaces;
            input.Value = value;
            input.Width = 96;
        }

        private static Control CreateLabeledControl(string labelText, Control control)
        {
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Margin = new Padding(0, 0, 14, 0)
            };

            panel.Controls.Add(new Label { Text = labelText, AutoSize = true, ForeColor = System.Drawing.Color.DimGray });
            panel.Controls.Add(control);

            return panel;
        }

        private static Control BuildGroupBox(string title, Control child)
        {
            var groupBox = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 12)
            };

            child.Dock = DockStyle.Fill;
            groupBox.Controls.Add(child);

            return groupBox;
        }

        private void ConfigureLedgerGrid()
        {
            _ledgerGrid.Dock = DockStyle.Fill;
            _ledgerGrid.BackgroundColor = System.Drawing.Color.White;
            _ledgerGrid.BorderStyle = BorderStyle.None;
            _ledgerGrid.AutoGenerateColumns = false;
            _ledgerGrid.AllowUserToAddRows = true;
            _ledgerGrid.AllowUserToDeleteRows = true;
            _ledgerGrid.ReadOnly = false;
            _ledgerGrid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            _ledgerGrid.RowHeadersVisible = false;
            _ledgerGrid.AllowUserToResizeRows = false;
            _ledgerGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _ledgerGrid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            _ledgerGrid.DataSource = _ledgerRows;

            _ledgerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "External Reference",
                DataPropertyName = nameof(LedgerEntryRow.ExternalReference),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 160
            });

            _ledgerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Currency",
                DataPropertyName = nameof(LedgerEntryRow.Currency),
                Width = 90
            });

            _ledgerGrid.Columns.Add(new DataGridViewComboBoxColumn
            {
                HeaderText = "Side",
                DataPropertyName = nameof(LedgerEntryRow.Side),
                DataSource = new[] { "BUY", "SELL" },
                Width = 90
            });

            _ledgerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Amount",
                DataPropertyName = nameof(LedgerEntryRow.Amount),
                Width = 110
            });

            _ledgerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Settlement Date",
                DataPropertyName = nameof(LedgerEntryRow.SettlementDate),
                Width = 140
            });
        }

        private void ConfigureResultsGrid()
        {
            _resultsGrid.Dock = DockStyle.Fill;
            _resultsGrid.BackgroundColor = System.Drawing.Color.White;
            _resultsGrid.BorderStyle = BorderStyle.None;
            _resultsGrid.AutoGenerateColumns = false;
            _resultsGrid.AllowUserToAddRows = false;
            _resultsGrid.AllowUserToDeleteRows = false;
            _resultsGrid.ReadOnly = true;
            _resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _resultsGrid.RowHeadersVisible = false;
            _resultsGrid.AllowUserToResizeRows = false;
            _resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            _resultsGrid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            _resultsGrid.DataSource = _resultRows;

            AddResultColumn("Currency", nameof(ReconciliationResultRow.Currency), 80);
            AddResultColumn("Settlement Date", nameof(ReconciliationResultRow.SettlementDate), 120);
            AddResultColumn("Matched", nameof(ReconciliationResultRow.IsMatched), 80);
            AddResultColumn("Expected Buy", nameof(ReconciliationResultRow.ExpectedBuyAmount), 110);
            AddResultColumn("Actual Buy", nameof(ReconciliationResultRow.ActualBuyAmount), 110);
            AddResultColumn("Buy Break", nameof(ReconciliationResultRow.BuyBreakAmount), 110);
            AddResultColumn("Expected Sell", nameof(ReconciliationResultRow.ExpectedSellAmount), 110);
            AddResultColumn("Actual Sell", nameof(ReconciliationResultRow.ActualSellAmount), 110);
            AddResultColumn("Sell Break", nameof(ReconciliationResultRow.SellBreakAmount), 110);
            AddResultColumn("Trades", nameof(ReconciliationResultRow.TradeCount), 70);
            AddResultColumn("Ledger", nameof(ReconciliationResultRow.LedgerEntryCount), 70);
            AddResultColumn("Worker Thread", nameof(ReconciliationResultRow.WorkerThreadId), 100);
        }

        private void AddResultColumn(string headerText, string propertyName, int width)
        {
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = headerText,
                DataPropertyName = propertyName,
                Width = width
            });
        }

        private async Task RunReconciliationAsync()
        {
            if (_fromDatePicker.Value.Date > _toDatePicker.Value.Date)
            {
                SetSummary("From date cannot be after to date.", true);
                return;
            }

            var ledgerEntries = BuildLedgerEntries();

            if (ledgerEntries.Count == 0)
            {
                SetSummary("Add at least one ledger entry before running reconciliation.", true);
                return;
            }

            var request = new ReconciliationBatchRequest
            {
                FromSettlementDate = _fromDatePicker.Value.Date,
                ToSettlementDate = _toDatePicker.Value.Date,
                Tolerance = _toleranceInput.Value,
                BatchSize = (int)_batchSizeInput.Value,
                MaxDegreeOfParallelism = (int)_parallelismInput.Value,
                LedgerEntries = ledgerEntries
            };

            _runCancellation?.Dispose();
            _runCancellation = new CancellationTokenSource();

            SetBusy(true);
            SetSummary("Reconciliation is running in the background...", false);

            try
            {
                using var client = CreateAuthorizedClient();

                // This await keeps the WinForms UI thread free while CoreAPI does the
                // async database read and parallel batch reconciliation on the server.
                var response = await client.PostAsJsonAsync(
                    "reconciliation/batches",
                    request,
                    _runCancellation.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(_runCancellation.Token);
                    SetSummary($"{(int)response.StatusCode} {response.ReasonPhrase}: {body}", true);
                    return;
                }

                var batchResult = await response.Content
                    .ReadFromJsonAsync<ReconciliationBatchResponse>(cancellationToken: _runCancellation.Token);

                if (batchResult == null)
                {
                    SetSummary("Reconciliation returned no result.", true);
                    return;
                }

                ShowResults(batchResult);
            }
            catch (OperationCanceledException)
            {
                SetSummary("Reconciliation cancelled.", true);
            }
            catch (Exception ex)
            {
                SetSummary($"Reconciliation failed: {ex.Message}", true);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private List<ReconciliationLedgerEntry> BuildLedgerEntries()
        {
            var entries = new List<ReconciliationLedgerEntry>();

            foreach (var row in _ledgerRows)
            {
                if (string.IsNullOrWhiteSpace(row.Currency) || string.IsNullOrWhiteSpace(row.Side))
                {
                    continue;
                }

                entries.Add(new ReconciliationLedgerEntry
                {
                    ExternalReference = row.ExternalReference.Trim(),
                    Currency = row.Currency.Trim().ToUpperInvariant(),
                    Side = row.Side.Trim().ToUpperInvariant(),
                    Amount = row.Amount,
                    SettlementDate = row.SettlementDate.Date
                });
            }

            return entries;
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(CoreApiBaseAddress) };
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);

            return client;
        }

        private void ShowResults(ReconciliationBatchResponse batchResult)
        {
            _resultRows.Clear();

            foreach (var result in batchResult.Results)
            {
                _resultRows.Add(new ReconciliationResultRow
                {
                    Currency = result.Currency,
                    SettlementDate = result.SettlementDate.ToString("yyyy-MM-dd"),
                    IsMatched = result.IsMatched,
                    ExpectedBuyAmount = result.ExpectedBuyAmount,
                    ActualBuyAmount = result.ActualBuyAmount,
                    BuyBreakAmount = result.BuyBreakAmount,
                    ExpectedSellAmount = result.ExpectedSellAmount,
                    ActualSellAmount = result.ActualSellAmount,
                    SellBreakAmount = result.SellBreakAmount,
                    TradeCount = result.TradeCount,
                    LedgerEntryCount = result.LedgerEntryCount,
                    WorkerThreadId = result.WorkerThreadId
                });
            }

            SetSummary(
                $"Batch {batchResult.BatchId}: {batchResult.MatchedGroupCount} matched, " +
                $"{batchResult.BreakGroupCount} break(s), {batchResult.ElapsedMilliseconds} ms, " +
                $"server worker thread(s): {string.Join(", ", batchResult.WorkerThreadIds)}.",
                batchResult.BreakGroupCount > 0);
        }

        private void LoadSampleScenario()
        {
            _ledgerRows.Clear();
            _fromDatePicker.Value = new DateTime(2026, 06, 26);
            _toDatePicker.Value = new DateTime(2026, 06, 26);
            _toleranceInput.Value = 0.01m;
            _batchSizeInput.Value = 2;
            _parallelismInput.Value = 4;

            // These ledger rows line up with Docs/Sql/SeedReconciliationData.sql.
            // They give you one clean match, one amount break, one tolerance match,
            // and one ledger-only break to make the result grid easy to understand.
            _ledgerRows.Add(new LedgerEntryRow
            {
                ExternalReference = "LED-USD-BUY-001",
                Currency = "USD",
                Side = "BUY",
                Amount = 1000m,
                SettlementDate = new DateTime(2026, 06, 26)
            });

            _ledgerRows.Add(new LedgerEntryRow
            {
                ExternalReference = "LED-USD-SELL-001",
                Currency = "USD",
                Side = "SELL",
                Amount = 250m,
                SettlementDate = new DateTime(2026, 06, 26)
            });

            _ledgerRows.Add(new LedgerEntryRow
            {
                ExternalReference = "LED-EUR-BUY-001",
                Currency = "EUR",
                Side = "BUY",
                Amount = 475m,
                SettlementDate = new DateTime(2026, 06, 26)
            });

            _ledgerRows.Add(new LedgerEntryRow
            {
                ExternalReference = "LED-GBP-SELL-001",
                Currency = "GBP",
                Side = "SELL",
                Amount = 300.005m,
                SettlementDate = new DateTime(2026, 06, 26)
            });

            _ledgerRows.Add(new LedgerEntryRow
            {
                ExternalReference = "LED-JPY-BUY-001",
                Currency = "JPY",
                Side = "BUY",
                Amount = 10000m,
                SettlementDate = new DateTime(2026, 06, 26)
            });

            SetSummary("Loaded sample ledger rows. Run Docs/Sql/SeedReconciliationData.sql first for matching trade data.", false);
        }

        private void SetBusy(bool isBusy)
        {
            SuspendLayout();

            _runButton.Enabled = !isBusy;
            _cancelButton.Enabled = isBusy;
            _loadSampleScenarioButton.Enabled = !isBusy;
            // Keep the progress bar visible so the Run settings panel does not
            // resize/reflow when a reconciliation starts or finishes.
            _progressBar.MarqueeAnimationSpeed = isBusy ? 30 : 0;
            _ledgerGrid.Enabled = !isBusy;

            ResumeLayout();
        }

        private void SetSummary(string message, bool isError)
        {
            _summaryLabel.ForeColor = isError
                ? System.Drawing.Color.Firebrick
                : System.Drawing.Color.DarkGreen;
            _summaryLabel.Text = message;
        }

        public sealed class LedgerEntryRow
        {
            public string ExternalReference { get; set; } = string.Empty;
            public string Currency { get; set; } = string.Empty;
            public string Side { get; set; } = "BUY";
            public decimal Amount { get; set; }
            public DateTime SettlementDate { get; set; } = DateTime.Today;
        }

        public sealed class ReconciliationResultRow
        {
            public string Currency { get; set; } = string.Empty;
            public string SettlementDate { get; set; } = string.Empty;
            public bool IsMatched { get; set; }
            public decimal ExpectedBuyAmount { get; set; }
            public decimal ActualBuyAmount { get; set; }
            public decimal BuyBreakAmount { get; set; }
            public decimal ExpectedSellAmount { get; set; }
            public decimal ActualSellAmount { get; set; }
            public decimal SellBreakAmount { get; set; }
            public int TradeCount { get; set; }
            public int LedgerEntryCount { get; set; }
            public int WorkerThreadId { get; set; }
        }
    }
}
