using System.Drawing;
using System.Windows.Forms;
using ChildhoodGame.Core;
using ChildhoodGame.Core.Strategy.TicTacToe;
using ChildhoodGame.Core.Strategy.Snake;

namespace ChildhoodGame.Runner;

internal sealed class GameLauncherForm : Form
{
    private static readonly Color SurfaceColor = Color.FromArgb(246, 243, 238);
    private static readonly Color PanelColor = Color.White;
    private static readonly Color AccentColor = Color.FromArgb(32, 96, 147);
    private static readonly Color SuccessColor = Color.FromArgb(45, 125, 84);
    private static readonly Color ErrorColor = Color.FromArgb(166, 63, 44);
    private static readonly Color SubtleTextColor = Color.FromArgb(86, 94, 107);

    private readonly IGameLoader loader;

    private readonly TextBox gameFolderTextBox;
    private readonly TextBox saveStateTextBox;
    private readonly TextBox loadStateTextBox;
    private readonly Button browseGameButton;
    private readonly Button browseSaveStateButton;
    private readonly Button browseLoadStateButton;
    private readonly Button validateButton;
    private readonly Button launchButton;
    private readonly Button stopButton;
    private readonly TextBox logTextBox;
    private readonly Label statusValueLabel;

    private IGameRuntime? runtime;
    private bool isBusy;

    public GameLauncherForm()
        : this(new DosGameLoader())
    {
    }

    internal GameLauncherForm(IGameLoader loader)
    {
        this.loader = loader;

        Text = "ChildhoodGame Launcher";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(840, 620);
        Size = new Size(900, 680);
        BackColor = SurfaceColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        AllowDrop = true;

        DragEnter += HandleDragEnter;
        DragDrop += HandleDragDrop;
        FormClosing += HandleFormClosing;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = SurfaceColor
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        Controls.Add(root);

        var headerPanel = BuildHeaderPanel();
        root.Controls.Add(headerPanel, 0, 0);

        var selectionPanel = BuildSelectionPanel();
        root.Controls.Add(selectionPanel, 0, 1);

        var actionsPanel = BuildActionsPanel();
        root.Controls.Add(actionsPanel, 0, 2);

        var logPanel = BuildLogPanel();
        root.Controls.Add(logPanel, 0, 3);

        gameFolderTextBox = CreateInputTextBox("Select or paste the game folder path");
        saveStateTextBox = CreateInputTextBox("Optional output file for emulator save-state");
        loadStateTextBox = CreateInputTextBox("Optional existing save-state to load on startup");

        browseGameButton = CreateActionButton("Browse", AccentColor, Color.White);
        browseSaveStateButton = CreateActionButton("Save As", Color.FromArgb(219, 232, 247), AccentColor);
        browseLoadStateButton = CreateActionButton("Browse", Color.FromArgb(219, 232, 247), AccentColor);
        validateButton = CreateActionButton("Validate", Color.FromArgb(226, 231, 237), AccentColor);
        launchButton = CreateActionButton("Launch Game", AccentColor, Color.White);
        stopButton = CreateActionButton("Stop Runtime", Color.FromArgb(248, 224, 218), ErrorColor);
        logTextBox = CreateLogTextBox();
        statusValueLabel = new Label();

        ConfigureSelectionPanel(selectionPanel);
        ConfigureActionsPanel(actionsPanel);
        ConfigureLogPanel(logPanel);

        AcceptButton = launchButton;

        browseGameButton.Click += HandleBrowseGameClick;
        browseSaveStateButton.Click += HandleBrowseSaveStateClick;
        browseLoadStateButton.Click += HandleBrowseLoadStateClick;
        validateButton.Click += HandleValidateClick;
        launchButton.Click += HandleLaunchClick;
        stopButton.Click += HandleStopClick;

        UpdateStatus("Idle", AccentColor);
        AppendLog("INFO", "Choose your DOS game folder to begin.");
        AppendLog("INFO", "You can drag and drop a folder onto this window.");
        UpdateControlState();
    }

    private Panel BuildHeaderPanel()
    {
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = AccentColor,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(20, 18, 20, 18)
        };

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = "ChildhoodGame Launcher",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold, GraphicsUnit.Point),
            Location = new Point(0, 0)
        };

        var subtitleLabel = new Label
        {
            AutoSize = true,
            Text = "Pick a game folder, validate its files, and launch without command-line paths.",
            ForeColor = Color.FromArgb(225, 234, 244),
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            Location = new Point(2, 48)
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        return headerPanel;
    }

    private Panel BuildSelectionPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = PanelColor,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 12)
        };
    }

    private Panel BuildActionsPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = PanelColor,
            Padding = new Padding(18, 14, 18, 20),
            Margin = new Padding(0, 0, 0, 12)
        };
    }

    private Panel BuildLogPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            Padding = new Padding(18)
        };
    }

    private void ConfigureSelectionPanel(Control selectionPanel)
    {
        var selectionTitleLabel = new Label
        {
            AutoSize = true,
            Text = "Game Selection",
            Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(31, 37, 45),
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 12)
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 4
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));

        AddInputRow(grid, 0, "Game folder", gameFolderTextBox, browseGameButton);
        AddInputRow(grid, 1, "Save state", saveStateTextBox, browseSaveStateButton);
        AddInputRow(grid, 2, "Load state", loadStateTextBox, browseLoadStateButton);

        var helperLabel = new Label
        {
            AutoSize = true,
            Text = "The folder must contain one .exe file and one .json file.",
            ForeColor = SubtleTextColor,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Margin = new Padding(0, 10, 0, 0)
        };

        grid.Controls.Add(helperLabel, 0, 3);
        grid.SetColumnSpan(helperLabel, 3);

        selectionPanel.Controls.Add(grid);
        selectionPanel.Controls.Add(selectionTitleLabel);
    }

    private void ConfigureActionsPanel(Control actionsPanel)
    {
        var statusLabel = new Label
        {
            AutoSize = true,
            Text = "Status",
            ForeColor = SubtleTextColor,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Location = new Point(18, 16)
        };

        statusValueLabel.AutoSize = true;
        statusValueLabel.Text = "Idle";
        statusValueLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
        statusValueLabel.Location = new Point(18, 34);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Location = new Point(0, 0),
            Padding = new Padding(0, 0, 0, 8),
            Margin = new Padding(0)
        };

        buttonsPanel.Controls.Add(validateButton);
        buttonsPanel.Controls.Add(launchButton);
        buttonsPanel.Controls.Add(stopButton);

        actionsPanel.Controls.Add(buttonsPanel);
        actionsPanel.Controls.Add(statusLabel);
        actionsPanel.Controls.Add(statusValueLabel);
    }

    private void ConfigureLogPanel(Control logPanel)
    {
        var titleLabel = new Label
        {
            AutoSize = true,
            Text = "Activity Log",
            Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(31, 37, 45),
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 10)
        };

        logPanel.Controls.Add(logTextBox);
        logPanel.Controls.Add(titleLabel);
    }

    private void AddInputRow(TableLayoutPanel grid, int rowIndex, string labelText, Control inputControl, Control actionControl)
    {
        var label = new Label
        {
            AutoSize = true,
            Text = labelText,
            ForeColor = SubtleTextColor,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Margin = new Padding(0, 9, 10, 0)
        };

        inputControl.Dock = DockStyle.Fill;
        inputControl.Margin = new Padding(0, 0, 12, 10);

        actionControl.Dock = DockStyle.Fill;
        actionControl.Margin = new Padding(0, 0, 0, 10);

        grid.Controls.Add(label, 0, rowIndex);
        grid.Controls.Add(inputControl, 1, rowIndex);
        grid.Controls.Add(actionControl, 2, rowIndex);
    }

    private static TextBox CreateInputTextBox(string placeholderText)
    {
        return new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = placeholderText,
            Height = 32
        };
    }

    private static TextBox CreateLogTextBox()
    {
        return new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            WordWrap = false,
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(252, 252, 252),
            Font = new Font("Cascadia Code", 9F, FontStyle.Regular, GraphicsUnit.Point)
        };
    }

    private static Button CreateActionButton(string text, Color backColor, Color foreColor)
    {
        var borderColor = foreColor == Color.White ? ControlPaint.Dark(backColor) : foreColor;
        return new BorderedButton(borderColor)
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Text = text,
            Padding = new Padding(16, 8, 16, 8),
            BackColor = backColor,
            ForeColor = foreColor,
            Margin = new Padding(0, 0, 10, 8),
            UseVisualStyleBackColor = false
        };
    }

    private void HandleBrowseGameClick(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the target game folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        ApplyDialogStartDirectory(dialog, gameFolderTextBox.Text);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        gameFolderTextBox.Text = dialog.SelectedPath;
        AppendLog("INFO", $"Selected game folder '{dialog.SelectedPath}'.");
    }

    private void HandleBrowseSaveStateClick(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Choose a save-state file",
            Filter = "Save-state files|*.sav;*.state|All files|*.*",
            OverwritePrompt = false
        };

        ApplyDialogStartDirectory(dialog, saveStateTextBox.Text);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        saveStateTextBox.Text = dialog.FileName;
        AppendLog("INFO", $"Save-state path set to '{dialog.FileName}'.");
    }

    private void HandleBrowseLoadStateClick(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select an existing load-state file",
            Filter = "Save-state files|*.sav;*.state|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        ApplyDialogStartDirectory(dialog, loadStateTextBox.Text);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        loadStateTextBox.Text = dialog.FileName;
        AppendLog("INFO", $"Load-state path set to '{dialog.FileName}'.");
    }

    private async void HandleValidateClick(object? sender, EventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            ValidateSelection();
            await Task.CompletedTask;
        }, "Validating...", AccentColor);
    }

    private async void HandleLaunchClick(object? sender, EventArgs e)
    {
        if (runtime is not null && runtime.IsRunning)
        {
            AppendLog("INFO", "Runtime is already running.");
            return;
        }

        await RunBusyAsync(async () =>
        {
            var loadResult = ValidateSelection();
            if (loadResult?.GamePackage is null)
            {
                return;
            }

            runtime = new DosGameRuntime();
            var inputController = new RuntimeInputController(runtime);

            try
            {
                AppendLog("INFO", $"Launching DOS runtime for '{loadResult.GamePackage.GameExecutablePath}'.");
                await runtime.StartAsync(loadResult.GamePackage);

                if (ShouldRunPaidBetaAutomation(loadResult.GamePackage)
                    && runtime is IWindowCaptureRuntime captureRuntime)
                {
                    AppendLog("INFO", "Starting PAIDBETA screen-aware automation.");
                    var automation = new PaidBetaDosAutomation();
                    var result = await automation.RunAsync(
                        runtime,
                        inputController,
                        captureRuntime,
                        loadResult.GamePackage,
                        new PaidBetaAutomationOptions(),
                        message => AppendLog("INFO", message));

                    AppendLog(result.IsWin ? "INFO" : "ERROR", $"PAIDBETA automation result: {result.Summary}");
                }
                else if (ShouldRunSnakeAutomation(loadResult.GamePackage)
                    && runtime is IWindowCaptureRuntime captureRuntimeSnake)
                {
                    AppendLog("INFO", "Starting Snake screen-aware automation.");
                    var startupInput = loadResult.GamePackage.RuntimeConfig.StartupInput;
                    if (startupInput is not null && startupInput.Length > 0)
                    {
                        foreach (var command in startupInput)
                        {
                            await inputController.SendCommandAsync(command);
                            AppendLog("INFO", $"Startup input sent: {command}");
                        }
                    }
                    await Task.Delay(500);
                    
                    var automation = new SnakeDosAutomation();
                    var result = await automation.RunAsync(
                        runtime,
                        inputController,
                        captureRuntimeSnake,
                        loadResult.GamePackage,
                        new SnakeAutomationOptions(),
                        message => AppendLog("INFO", message));

                    AppendLog(result.IsWin ? "INFO" : "ERROR", $"Snake automation result: {result.Summary}");
                }
                else
                {
                    var startupInput = loadResult.GamePackage.RuntimeConfig.StartupInput;
                    if (startupInput is not null && startupInput.Length > 0)
                    {
                        foreach (var command in startupInput)
                        {
                            await inputController.SendCommandAsync(command);
                            AppendLog("INFO", $"Startup input sent: {command}");
                        }
                    }
                }

                UpdateStatus("Runtime running", SuccessColor);
                AppendLog("INFO", "Runtime started successfully.");
            }
            catch (Exception ex)
            {
                UpdateStatus("Launch failed", ErrorColor);
                AppendLog("ERROR", $"Runtime crash detected: {ex.Message}");
                AppendLog("ERROR", ex.ToString());
                await DisposeRuntimeAsync();
            }
        }, "Launching...", AccentColor);
    }

    private static bool ShouldRunPaidBetaAutomation(GamePackage gamePackage) =>
        Path.GetFileName(gamePackage.GameExecutablePath)
            .Equals("PAIDBETA.EXE", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldRunSnakeAutomation(GamePackage gamePackage) =>
        Path.GetFileName(gamePackage.GameExecutablePath)
            .Equals("SNAKE.EXE", StringComparison.OrdinalIgnoreCase);

    private async void HandleStopClick(object? sender, EventArgs e)
    {
        if (runtime is null)
        {
            AppendLog("INFO", "No active runtime to stop.");
            return;
        }

        await RunBusyAsync(async () =>
        {
            try
            {
                if (runtime is not null && runtime.IsRunning)
                {
                    await runtime.StopAsync();
                    AppendLog("INFO", "Runtime stopped cleanly.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Stop failed", ErrorColor);
                AppendLog("ERROR", $"Failed to stop runtime: {ex.Message}");
                AppendLog("ERROR", ex.ToString());
            }
            finally
            {
                await DisposeRuntimeAsync();
                UpdateStatus("Stopped", AccentColor);
            }
        }, "Stopping...", AccentColor);
    }

    private void HandleDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void HandleDragDrop(object? sender, DragEventArgs e)
    {
        var droppedFiles = e.Data?.GetData(DataFormats.FileDrop) as string[];
        if (droppedFiles is null || droppedFiles.Length == 0)
        {
            return;
        }

        var gameFolder = ResolveGameFolder(droppedFiles[0]);
        if (string.IsNullOrWhiteSpace(gameFolder))
        {
            AppendLog("ERROR", $"Dropped path could not be resolved to a folder: '{droppedFiles[0]}'.");
            return;
        }

        gameFolderTextBox.Text = gameFolder;
        AppendLog("INFO", $"Dropped game folder '{gameFolder}'.");
    }

    private void HandleFormClosing(object? sender, FormClosingEventArgs e)
    {
        var activeRuntime = runtime;
        runtime = null;

        if (activeRuntime is null)
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                if (activeRuntime.IsRunning)
                {
                    await activeRuntime.StopAsync();
                }

                await activeRuntime.DisposeAsync();
            }
            catch
            {
            }
        }).GetAwaiter().GetResult();
    }

    private GameLoadResult? ValidateSelection()
    {
        var selectedPath = gameFolderTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            UpdateStatus("Select a folder", ErrorColor);
            AppendLog("ERROR", "Choose a game folder before validating or launching.");
            return null;
        }

        var resolvedGameFolder = ResolveGameFolder(selectedPath);
        gameFolderTextBox.Text = resolvedGameFolder;

        var launchOptions = new GameLaunchOptions(
            resolvedGameFolder,
            false,
            NormalizeOptionalPath(saveStateTextBox.Text),
            NormalizeOptionalPath(loadStateTextBox.Text));

        var loadResult = loader.Load(launchOptions);
        if (!loadResult.IsValid || loadResult.GamePackage is null)
        {
            UpdateStatus("Validation failed", ErrorColor);
            AppendLog("ERROR", "Game launch validation failed.");
            foreach (var error in loadResult.Errors)
            {
                AppendLog("ERROR", error);
            }

            return null;
        }

        UpdateStatus("Validated", SuccessColor);
        AppendLog("INFO", $"Validated game package at '{loadResult.GamePackage.GameRootPath}'.");
        return loadResult;
    }

    private async Task RunBusyAsync(Func<Task> action, string statusText, Color statusColor)
    {
        if (isBusy)
        {
            return;
        }

        isBusy = true;
        UpdateStatus(statusText, statusColor);
        UpdateControlState();

        try
        {
            await action();
        }
        finally
        {
            isBusy = false;
            UpdateControlState();
        }
    }

    private async Task DisposeRuntimeAsync()
    {
        if (runtime is null)
        {
            return;
        }

        await runtime.DisposeAsync();
        runtime = null;
    }

    private void UpdateControlState()
    {
        var runtimeIsActive = runtime is not null && runtime.IsRunning;

        gameFolderTextBox.Enabled = !isBusy && !runtimeIsActive;
        saveStateTextBox.Enabled = !isBusy && !runtimeIsActive;
        loadStateTextBox.Enabled = !isBusy && !runtimeIsActive;
        browseGameButton.Enabled = !isBusy && !runtimeIsActive;
        browseSaveStateButton.Enabled = !isBusy && !runtimeIsActive;
        browseLoadStateButton.Enabled = !isBusy && !runtimeIsActive;
        validateButton.Enabled = !isBusy && !runtimeIsActive;
        launchButton.Enabled = !isBusy && !runtimeIsActive;
        stopButton.Enabled = !isBusy && runtimeIsActive;
    }

    private void UpdateStatus(string text, Color color)
    {
        statusValueLabel.Text = text;
        statusValueLabel.ForeColor = color;
    }

    private void AppendLog(string level, string message)
    {
        var entry = $"{DateTimeOffset.UtcNow:O} [{level}] {message}{Environment.NewLine}";
        logTextBox.AppendText(entry);
    }

    private static string NormalizeOptionalPath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? null! : Path.GetFullPath(path.Trim());
    }

    private static string ResolveGameFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            if (Directory.Exists(path))
            {
                return Path.GetFullPath(path);
            }

            if (File.Exists(path))
            {
                return Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;
            }

            return Path.GetFullPath(path);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void ApplyDialogStartDirectory(FolderBrowserDialog dialog, string existingPath)
    {
        var startDirectory = ResolveGameFolder(existingPath);
        if (!string.IsNullOrWhiteSpace(startDirectory) && Directory.Exists(startDirectory))
        {
            dialog.SelectedPath = startDirectory;
        }
    }

    private static void ApplyDialogStartDirectory(FileDialog dialog, string existingPath)
    {
        if (string.IsNullOrWhiteSpace(existingPath))
        {
            return;
        }

        try
        {
            var startDirectory = existingPath;
            if (File.Exists(existingPath))
            {
                startDirectory = Path.GetDirectoryName(existingPath) ?? existingPath;
            }

            if (Directory.Exists(startDirectory))
            {
                dialog.InitialDirectory = Path.GetFullPath(startDirectory);
            }
        }
        catch
        {
        }
    }

    private sealed class BorderedButton : Button
    {
        private readonly Color borderColor;
        private bool isHovered;
        private bool isPressed;

        public BorderedButton(Color borderColor)
        {
            this.borderColor = borderColor;

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            MinimumSize = new Size(0, 48);

            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isPressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var backgroundColor = Enabled ? BackColor : SystemColors.Control;
            if (Enabled && isPressed)
            {
                backgroundColor = ControlPaint.Dark(backgroundColor);
            }
            else if (Enabled && isHovered)
            {
                backgroundColor = ControlPaint.Light(backgroundColor);
            }

            using (var brush = new SolidBrush(backgroundColor))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            var textColor = Enabled ? ForeColor : SystemColors.GrayText;
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                ClientRectangle,
                textColor,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis);

            DrawButtonBorder(e.Graphics, Enabled ? borderColor : SystemColors.ControlDark);

            if (Focused && ShowFocusCues)
            {
                var focusBounds = Rectangle.Inflate(ClientRectangle, -4, -4);
                ControlPaint.DrawFocusRectangle(e.Graphics, focusBounds, textColor, backgroundColor);
            }
        }

        private void DrawButtonBorder(Graphics graphics, Color color)
        {
            var width = ClientSize.Width;
            var height = ClientSize.Height;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            using var pen = new Pen(color);
            graphics.DrawLine(pen, 0, 0, width - 1, 0);
            graphics.DrawLine(pen, 0, 0, 0, height - 1);
            graphics.DrawLine(pen, width - 1, 0, width - 1, height - 1);
            graphics.DrawLine(pen, 0, height - 1, width - 1, height - 1);

            if (height > 2)
            {
                graphics.DrawLine(pen, 0, height - 2, width - 1, height - 2);
            }
        }
    }
}
