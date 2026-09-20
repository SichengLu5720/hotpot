using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Harness.Reusable;
using DevConsole = Harness.Reusable.DeveloperConsole;

namespace HarnessModelDashboard;

internal static class KitExport
{
    public static Button Button(string module, Control owner, Func<string>? parameterConfiguration = null)
    {
        var button = new Button { Text = "导出源码包", AutoSize = true, Height = 32 };
        button.Click += (_, _) => {
            using var dialog = new SaveFileDialog { Filter = "源码压缩包|*.zip", FileName = "Harness-" + module + ".zip", OverwritePrompt = true };
            if (dialog.ShowDialog(owner) != DialogResult.OK) return;
            try {
                var config = parameterConfiguration?.Invoke();
                var assembly = Assembly.GetExecutingAssembly();
                using var memory = new MemoryStream();
                using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
                {
                    if (config != null) {
                        using var configWriter = new StreamWriter(zip.CreateEntry("Unity/DeveloperConsoleParameters.json").Open(), new UTF8Encoding(false));
                        configWriter.Write(config);
                    }
                    foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.StartsWith("kit/")))
                    {
                        var path = resource[4..].Replace('\\', '/');
                        bool include = path.EndsWith(".md") || path == "provenance.json" ||
                            (module == "Leaderboard" ? path.Contains("Leaderboard") || path.StartsWith("WeChat/") :
                                path.Contains("DeveloperConsole") || path.Contains("UnityParameterStore"));
                        if (!include) continue;
                        using var input = assembly.GetManifestResourceStream(resource)!;
                        using var output = zip.CreateEntry(path).Open(); input.CopyTo(output);
                    }
                }
                // Build the entire archive before replacing the chosen destination.
                File.WriteAllBytes(dialog.FileName, memory.ToArray());
                MessageBox.Show(owner, "源码包已导出，按 README.md 接入即可。", "导出完成");
            } catch (Exception e) { MessageBox.Show(owner, e.Message, "导出失败"); }
        };
        return button;
    }
    public static Button Guide(Control owner)
    {
        var button = new Button { Text = "接入说明", AutoSize = true };
        button.Click += (_, _) => {
            using var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("kit/README.md")!;
            using var reader = new StreamReader(input);
            using var form = new Form { Text = "可复用组件 · 接入说明", Size = new Size(800, 650), StartPosition = FormStartPosition.CenterParent };
            form.Controls.Add(new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both,
                Font = new Font("Microsoft YaHei UI", 10), Text = reader.ReadToEnd().Replace("\n", Environment.NewLine) });
            form.ShowDialog(owner);
        };
        return button;
    }
    public static TableLayoutPanel Layout(Control owner, string title, string subtitle)
    {
        owner.Dock = DockStyle.Fill; owner.Padding = new Padding(18);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Microsoft YaHei UI", 13, FontStyle.Bold) }, 0, 0);
        layout.Controls.Add(new Label { Text = subtitle, Dock = DockStyle.Fill, ForeColor = Color.DimGray }, 0, 1);
        owner.Controls.Add(layout); return layout;
    }
}

internal sealed class ExampleProvider : ILeaderboardProvider
{
    public int Mode;
    public Task<IReadOnlyList<ScoreEntry>> LoadAsync(CancellationToken cancellation)
    {
        if (Mode == 2) throw new IOException("示例：数据源不可用。切换到正常榜单后重试。");
        if (Mode == 3) return Task.Delay(15000, cancellation).ContinueWith<IReadOnlyList<ScoreEntry>>(
            _ => Array.Empty<ScoreEntry>(), cancellation);
        var entries = new List<ScoreEntry> { new ScoreEntry("self", "我（示例）", 12, 105) };
        if (Mode == 0) for (int i = 0; i < 30; i++)
            entries.Add(new ScoreEntry("friend-" + i.ToString("D2"), "示例玩家 " + (i + 1), Math.Max(0, 20 - i / 2), 100 + i));
        return Task.FromResult<IReadOnlyList<ScoreEntry>>(entries);
    }
}

internal sealed class LeaderboardPage : UserControl
{
    private readonly ExampleProvider provider = new();
    private readonly LeaderboardController controller;
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly Label self = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.FromArgb(226, 239, 232) };
    private readonly Label status = new() { AutoSize = true, Padding = new Padding(0, 7, 0, 0) };
    public LeaderboardPage(string repo)
    {
        controller = new LeaderboardController(provider, "self", TimeSpan.FromSeconds(3));
        var layout = KitExport.Layout(this, "可复用排行榜", "TASK-008 · 当前为本地示例数据\n成绩降序 → 首次达成时间升序；本人排名固定在底部。");
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var modes = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        modes.Items.AddRange(new object[] { "正常榜单", "暂无好友", "加载失败", "加载超时（3 秒）" }); modes.SelectedIndex = 0;
        modes.SelectedIndexChanged += async (_, _) => { provider.Mode = modes.SelectedIndex; await controller.RefreshAsync(); };
        var refresh = new Button { Text = "刷新 / 重试", AutoSize = true };
        refresh.Click += async (_, _) => await controller.RefreshAsync();
        actions.Controls.AddRange(new Control[] { modes, refresh, status }); content.Controls.Add(actions, 0, 0);
        grid.Columns.Add("rank", "名次"); grid.Columns.Add("name", "玩家"); grid.Columns.Add("score", "成绩");
        foreach (DataGridViewColumn column in grid.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;
        grid.RowTemplate.Height = 30;
        content.Controls.Add(grid, 0, 1); content.Controls.Add(self, 0, 2); layout.Controls.Add(content, 0, 2);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        footer.Controls.Add(KitExport.Button("Leaderboard", this)); footer.Controls.Add(KitExport.Guide(this)); layout.Controls.Add(footer, 0, 3);
        controller.Changed += Render;
        Load += async (_, _) => await controller.RefreshAsync();
    }
    private void Render()
    {
        if (IsDisposed) return;
        grid.Rows.Clear();
        status.Text = controller.State switch {
            LeaderboardState.Loading => "加载中…", LeaderboardState.Failure => "加载失败，可重试",
            LeaderboardState.Empty => "暂无好友记录", _ => "已加载 · 示例数据" };
        foreach (var row in controller.Current?.Rows ?? Array.Empty<RankedEntry>())
        {
            int index = grid.Rows.Add(row.Rank, row.Entry.Name, row.Entry.Score);
            if (row.IsSelf) grid.Rows[index].DefaultCellStyle.BackColor = Color.FromArgb(226, 239, 232);
        }
        var own = controller.Current?.Self;
        self.Text = controller.State == LeaderboardState.Failure ? "  " + controller.Error :
            own == null ? "  我的排名：暂无可靠记录" : $"  我（示例）    第 {own.Rank} 名    成绩 {own.Entry.Score}";
    }
    protected override void Dispose(bool disposing) { if (disposing) { controller.Changed -= Render; controller.Dispose(); } base.Dispose(disposing); }
}

internal sealed class JsonParameterStore : IParameterStore
{
    private readonly string path;
    public JsonParameterStore(string path) { this.path = path; }
    public IDictionary<string, double> Load() => File.Exists(path) ?
        JsonSerializer.Deserialize<Dictionary<string, double>>(File.ReadAllText(path)) ?? new() : new();
    public void Save(IDictionary<string, double> values)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, JsonSerializer.Serialize(values)); File.Move(temp, path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

internal sealed class DeveloperConsolePage : UserControl
{
    public CustomParameterEditor Editor { get; }
    public DeveloperConsolePage(string repo)
    {
        var layout = KitExport.Layout(this, "自定义开发调试参数",
            "填写想调试的参数，例如圆球大小、特效速度，以及默认值和范围。\n导入时 Codex 会查找项目中的真实对象与字段并完成绑定。");
        Editor = new CustomParameterEditor(repo);
        layout.Controls.Add(Editor, 0, 2);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        footer.Controls.Add(KitExport.Button("DeveloperConsole", this, () => {
            var values = Editor.Read(); Editor.Save(values); return CustomParameterEditor.Configuration(values);
        }));
        footer.Controls.Add(KitExport.Guide(this));
        if (Editor.LoadWarning.Length > 0)
            footer.Controls.Add(new Label { Text = Editor.LoadWarning, AutoSize = true, ForeColor = Color.Firebrick });
        layout.Controls.Add(footer, 0, 3);
    }
}
