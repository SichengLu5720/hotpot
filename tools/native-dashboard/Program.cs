using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;

namespace HarnessModelDashboard;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new DashboardForm());
    }
}

internal sealed class DashboardForm : Form
{
    private const string InheritModel = "继承宿主";
    private static readonly (string Key, string Name)[] Agents =
    [
        ("feature_designer", "Feature Designer"),
        ("design_art_agent", "Design-Art"),
        ("code_builder", "Code Builder"),
        ("qa_reporter", "QA Reporter")
    ];

    private static readonly string[] SuggestedModels =
    [
        InheritModel,
        "gpt-6-astra",
        "gpt-5.6-sol",
        "gpt-5.6-terra",
        "gpt-5.6-luna",
        "gpt-5.5"
    ];

    private static readonly string[] Efforts =
        ["none", "minimal", "low", "medium", "high", "xhigh", "max", "ultra"];

    private readonly Dictionary<string, (ComboBox Model, ComboBox Effort)> _editors = [];
    private readonly Label _status = new();
    private readonly Button _saveButton = new();
    private readonly Button _refreshButton = new();
    private readonly string _repoRoot = "";
    private readonly string _python = "";
    private string _etag = "";

    public DashboardForm()
    {
        Text = "Harness 开发看板";
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
        BackColor = Color.FromArgb(247, 248, 250);
        ForeColor = Color.FromArgb(32, 35, 42);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        ClientSize = new Size(800, 560);
        MinimumSize = Size;

        try
        {
            _repoRoot = FindRepositoryRoot();
            _python = FindPython(_repoRoot);
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "Harness 模型配置", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Shown += (_, _) => Close();
            return;
        }

        BuildUi();
        BuildPages();
        Shown += async (_, _) => await ReloadAsync();
    }

    private void BuildPages()
    {
        var modelPage = new TabPage("模型配置") { BackColor = BackColor, AutoScroll = true };
        foreach (var control in Controls.Cast<Control>().ToArray()) modelPage.Controls.Add(control);
        // Reflow after reparenting into a tab, using its actual client dimensions.
        void LayoutModelPage()
        {
            int pageWidth = modelPage.ClientSize.Width;
            var group = modelPage.Controls.OfType<GroupBox>().First();
            group.Width = Math.Max(720, pageWidth - group.Left - 20);
            var table = group.Controls.OfType<TableLayoutPanel>().First();
            table.Width = group.ClientSize.Width - table.Left - 14;
            int footerY = Math.Max(group.Bottom + 18, modelPage.ClientSize.Height - 48);
            _saveButton.Location = new Point(pageWidth - _saveButton.Width - 20, footerY);
            _refreshButton.Location = new Point(_saveButton.Left - _refreshButton.Width - 8, footerY);
            _status.Location = new Point(24, footerY + 5);
            _status.Width = Math.Max(180, _refreshButton.Left - _status.Left - 16);
        }
        modelPage.ClientSizeChanged += (_, _) => LayoutModelPage();
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(18, 9) };
        var install = new TabPage("组件接入") { BackColor = BackColor };
        var consolePage = new DeveloperConsolePage(_repoRoot);
        var delivery = new ImportPage(_repoRoot, consolePage.Editor);
        install.Controls.Add(delivery);
        tabs.TabPages.Add(install);
        FormClosing += (_, e) => {
            if (delivery.Running) {
                e.Cancel = true;
                MessageBox.Show(this, "Codex 正在执行接入任务。可最小化窗口；若需关闭，请先在组件接入页停止任务。", "任务执行中");
            }
        };
        tabs.TabPages.Add(modelPage);
        var leaderboard = new TabPage("排行榜") { BackColor = BackColor };
        leaderboard.Controls.Add(new LeaderboardPage(_repoRoot));
        tabs.TabPages.Add(leaderboard);
        var console = new TabPage("开发者控制台") { BackColor = BackColor };
        console.Controls.Add(consolePage);
        tabs.TabPages.Add(console);
        delivery.EditParametersRequested += () => tabs.SelectedTab = console;
        Controls.Add(tabs);
    }

    private void BuildUi()
    {
        var accent = new Panel
        {
            Dock = DockStyle.Top,
            Height = 4,
            BackColor = Color.FromArgb(38, 132, 214)
        };
        Controls.Add(accent);

        var title = new Label
        {
            Text = "Subagent 模型设置",
            Font = new Font("Microsoft YaHei UI", 13.5F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 20)
        };
        Controls.Add(title);

        var subtitle = new Label
        {
            Text = "保存后仅影响新派发的 Subagent",
            ForeColor = Color.FromArgb(104, 110, 120),
            AutoSize = true,
            Location = new Point(26, 52)
        };
        Controls.Add(subtitle);

        var group = new GroupBox
        {
            Text = "模型配置",
            Location = new Point(20, 82),
            Size = new Size(720, 230),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(group);

        var table = new TableLayoutPanel
        {
            Location = new Point(14, 26),
            Size = new Size(692, 188),
            ColumnCount = 3,
            RowCount = 5,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            BackColor = Color.White,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
        for (var row = 1; row < 5; row++)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        group.Controls.Add(table);

        AddHeader(table, "Subagent", 0);
        AddHeader(table, "模型", 1);
        AddHeader(table, "强度", 2);

        for (var index = 0; index < Agents.Length; index++)
        {
            var agent = Agents[index];
            var name = new Label
            {
                Text = agent.Name,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };
            table.Controls.Add(name, 0, index + 1);

            var model = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(7, 3, 7, 3)
            };
            model.Items.AddRange(SuggestedModels);
            table.Controls.Add(model, 1, index + 1);

            var effort = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(7, 3, 7, 3)
            };
            effort.Items.AddRange(Efforts);
            table.Controls.Add(effort, 2, index + 1);
            _editors[agent.Key] = (model, effort);
        }

        _status.AutoEllipsis = true;
        _status.ForeColor = Color.FromArgb(92, 99, 109);
        _status.Location = new Point(24, 336);
        _status.Size = new Size(430, 25);
        _status.TextAlign = ContentAlignment.MiddleLeft;
        Controls.Add(_status);

        _refreshButton.Text = "刷新";
        _refreshButton.Location = new Point(548, 331);
        _refreshButton.Size = new Size(84, 32);
        _refreshButton.Click += async (_, _) => await ReloadAsync();
        Controls.Add(_refreshButton);

        _saveButton.Text = "保存应用";
        _saveButton.Location = new Point(640, 331);
        _saveButton.Size = new Size(100, 32);
        _saveButton.BackColor = Color.FromArgb(38, 132, 214);
        _saveButton.ForeColor = Color.White;
        _saveButton.FlatStyle = FlatStyle.Flat;
        _saveButton.FlatAppearance.BorderSize = 0;
        _saveButton.Click += async (_, _) => await SaveAsync();
        Controls.Add(_saveButton);
    }

    private static void AddHeader(TableLayoutPanel table, string text, int column)
    {
        table.Controls.Add(new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            BackColor = Color.FromArgb(240, 242, 245),
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
        }, column, 0);
    }

    private async Task ReloadAsync()
    {
        SetBusy(true, "正在读取配置…");
        try
        {
            using var json = JsonDocument.Parse(await RunHarnessAsync("models", "show"));
            var root = json.RootElement;
            _etag = root.GetProperty("etag").GetString() ?? "";
            foreach (var row in root.GetProperty("rows").EnumerateArray())
            {
                var key = row.GetProperty("role").GetString()!;
                if (!_editors.TryGetValue(key, out var editor))
                    continue;
                var target = row.GetProperty("target");
                var model = target.GetProperty("model");
                var modelText = model.ValueKind == JsonValueKind.Null ? InheritModel : model.GetString()!;
                if (!editor.Model.Items.Contains(modelText))
                    editor.Model.Items.Add(modelText);
                editor.Model.Text = modelText;
                editor.Effort.SelectedItem = target.GetProperty("effort").GetString() ?? "medium";
            }
            _status.ForeColor = Color.FromArgb(49, 126, 78);
            _status.Text = "配置已同步";
        }
        catch (Exception error)
        {
            ShowError(error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SaveAsync()
    {
        SetBusy(true, "正在保存…");
        var temporary = Path.Combine(Path.GetTempPath(), $"harness-models-{Guid.NewGuid():N}.json");
        try
        {
            var agents = new Dictionary<string, object>();
            foreach (var agent in Agents)
            {
                var editor = _editors[agent.Key];
                var model = editor.Model.Text.Trim();
                if (model == InheritModel)
                    model = "";
                var effort = editor.Effort.SelectedItem?.ToString() ?? "medium";
                agents[agent.Key] = new { model, effort };
            }
            var payload = JsonSerializer.Serialize(new { agents });
            await File.WriteAllTextAsync(temporary, payload, new UTF8Encoding(false));
            await RunHarnessAsync("models", "update-agents", "--input", temporary, "--etag", _etag);
            await ReloadAsync();
            _status.ForeColor = Color.FromArgb(49, 126, 78);
            _status.Text = "已保存；新配置将在下次派发时生效";
        }
        catch (Exception error)
        {
            ShowError(error);
        }
        finally
        {
            try { File.Delete(temporary); } catch { }
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy, string? message = null)
    {
        UseWaitCursor = busy;
        _saveButton.Enabled = !busy;
        _refreshButton.Enabled = !busy;
        foreach (var editor in _editors.Values)
        {
            editor.Model.Enabled = !busy;
            editor.Effort.Enabled = !busy;
        }
        if (message is not null)
        {
            _status.ForeColor = Color.FromArgb(92, 99, 109);
            _status.Text = message;
        }
    }

    private void ShowError(Exception error)
    {
        _status.ForeColor = Color.FromArgb(183, 52, 52);
        _status.Text = "操作失败";
        MessageBox.Show(error.Message, "Harness 模型配置", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private async Task<string> RunHarnessAsync(params string[] arguments)
    {
        var start = new ProcessStartInfo
        {
            FileName = _python,
            WorkingDirectory = _repoRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        start.ArgumentList.Add(Path.Combine(_repoRoot, "tools", "harness.py"));
        start.Environment["PYTHONIOENCODING"] = "utf-8";
        start.ArgumentList.Add("--repo");
        start.ArgumentList.Add(_repoRoot);
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        using var process = Process.Start(start) ?? throw new InvalidOperationException("无法启动 Harness 后端");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
        {
            var message = error.Trim();
            try
            {
                using var json = JsonDocument.Parse(message);
                message = json.RootElement.GetProperty("error").GetString() ?? message;
            }
            catch { }
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "Harness 操作失败" : message);
        }
        return output;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; current is not null && depth < 8; depth++, current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "models.toml")) &&
                File.Exists(Path.Combine(current.FullName, "tools", "harness.py")))
                return current.FullName;
        }
        throw new InvalidOperationException("找不到 Harness 项目目录。请把程序放在项目根目录或其子目录中。");
    }

    private static string FindPython(string repoRoot)
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("HARNESS_PYTHON"),
            Path.Combine(repoRoot, ".venv", "Scripts", "python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cache", "codex-runtimes", "codex-primary-runtime", "dependencies", "python", "python.exe"),
            "python.exe"
        };
        foreach (var candidate in candidates.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            try
            {
                var start = new ProcessStartInfo
                {
                    FileName = candidate!,
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = Process.Start(start);
                if (process is null)
                    continue;
                process.WaitForExit(3000);
                if (process.ExitCode == 0)
                    return candidate!;
            }
            catch { }
        }
        throw new InvalidOperationException("找不到 Python 3.11+。请安装 Python，或设置 HARNESS_PYTHON 环境变量。");
    }
}
