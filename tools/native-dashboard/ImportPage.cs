using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace HarnessModelDashboard;

internal sealed class ImportPage : UserControl
{
    private readonly TextBox target = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly CheckBox leaderboard = new() { Text = "排行榜 · 排序 / 本人栏 / 重试 / 微信开放域", AutoSize = true };
    private readonly CheckBox console = new() { Text = "开发者控制台 · 自定义参数", AutoSize = true };
    private readonly CustomParameterEditor parameterEditor;
    public event Action? EditParametersRequested;
    private readonly TextBox requirements = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical,
        PlaceholderText = "补充接入要求（可选）：例如排行榜入口放在首页右侧，成绩使用累计积分。" };
    private readonly RichTextBox log = new() { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
    private readonly Label status = new() { Dock = DockStyle.Fill, Text = "勾选组件后，自动生成任务并由 Codex 接入目标项目。", AutoEllipsis = true };
    private readonly Button start = new() { Text = "导入 Codex 并自动交付", AutoSize = true };
    private readonly Button stop = new() { Text = "停止任务", AutoSize = true, Enabled = false };
    private readonly Button pick = new() { Text = "选择项目", AutoSize = true };
    private readonly Button report = new() { Text = "打开交付目录", AutoSize = true, Enabled = false };
    private Process? process;
    private bool canceled;
    private string? jobDirectory;
    private readonly string settingsPath;
    public bool Running { get; private set; }

    public ImportPage(string root, CustomParameterEditor parameterEditor)
    {
        this.parameterEditor = parameterEditor;
        Dock = DockStyle.Fill; Padding = new Padding(18);
        settingsPath = Path.Combine(root, ".harness", "import-project.txt");
        target.Text = File.Exists(settingsPath) ? File.ReadAllText(settingsPath).Trim() : root;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8 };
        foreach (int height in new[] { 36, 38, 34, 34, 65, 30 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.Controls.Add(new Label { Text = "勾选组件，自动接入项目", AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 13, FontStyle.Bold) }, 0, 0);
        var location = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        location.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); location.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        location.Controls.Add(target, 0, 0); location.Controls.Add(pick, 1, 0); layout.Controls.Add(location, 0, 1);
        layout.Controls.Add(leaderboard, 0, 2);
        var consoleOptions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var editParameters = new Button { Text = "填写调试参数…", AutoSize = true };
        editParameters.Click += (_, _) => EditParametersRequested?.Invoke();
        consoleOptions.Controls.AddRange(new Control[] { console, editParameters });
        layout.Controls.Add(consoleOptions, 0, 3);
        layout.Controls.Add(requirements, 0, 4); layout.Controls.Add(status, 0, 5); layout.Controls.Add(log, 0, 6);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.AddRange(new Control[] { start, stop, report }); layout.Controls.Add(buttons, 0, 7); Controls.Add(layout);
        pick.Click += (_, _) => {
            using var dialog = new FolderBrowserDialog { Description = "选择要接入组件的项目根目录", InitialDirectory = target.Text, UseDescriptionForTitle = true };
            if (dialog.ShowDialog(this) == DialogResult.OK) target.Text = dialog.SelectedPath;
        };
        start.Click += async (_, _) => await StartAsync();
        stop.Click += (_, _) => {
            try { canceled = true; if (process is { HasExited: false }) process.Kill(true); }
            catch (Exception e) { Append(e.Message); }
        };
        report.Click += (_, _) => { if (jobDirectory != null) Process.Start(new ProcessStartInfo(jobDirectory) { UseShellExecute = true }); };
        Append("选择已有游戏项目，勾选组件，然后点击“导入 Codex 并自动交付”。\n" +
            "程序会导入源码、生成每个组件的 Task 请求单并启动本机 Codex，执行适配、绑定和交付。\n" +
            "使用当前 Codex 登录及宿主模型；缺少引擎、账号或必要产品信息时会显示阻塞原因。\n" +
            "运行状态在此页显示；CLI 会话是否出现在 Codex 桌面任务列表取决于宿主支持。\n");
    }

    private void Busy(bool busy)
    {
        Running = busy; start.Enabled = pick.Enabled = leaderboard.Enabled = console.Enabled = requirements.Enabled = !busy;
        stop.Enabled = busy;
        parameterEditor.Enabled = !busy;
    }
    private void Append(string text)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => Append(text)); return; }
        if (log.TextLength > 100000) log.Clear();
        log.AppendText(text + Environment.NewLine); log.ScrollToCaret();
    }
    private static string FindCodex()
    {
        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            string path = Path.Combine(directory.Trim('"'), "codex.exe");
            if (File.Exists(path)) return path;
        }
        var bundled = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenAI", "Codex", "bin");
        if (Directory.Exists(bundled))
        {
            var file = Directory.EnumerateFiles(bundled, "codex.exe", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            if (file != null) return file;
        }
        throw new InvalidOperationException("没有找到本机 Codex。请先安装并登录 Codex 桌面应用或 CLI。");
    }
    private static void NewText(string path, string text)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false)); writer.Write(text);
    }
    private static void RejectLinks(string path)
    {
        var directory = new DirectoryInfo(path);
        for (; directory != null; directory = directory.Parent)
            if (directory.Exists && directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new IOException("目标路径包含链接或重定向目录，请选择实际项目目录。");
    }

    private async Task StartAsync()
    {
        if (!leaderboard.Checked && !console.Checked) { status.Text = "请至少勾选一个组件。"; return; }
        Busy(true); canceled = false; jobDirectory = null; report.Enabled = false; log.Clear();
        FileStream? projectLock = null;
        try
        {
            var parameters = console.Checked ? parameterEditor.Read() : new List<CustomParameter>();
            if (console.Checked) parameterEditor.Save(parameters);
            var executable = FindCodex();
            string root = Path.GetFullPath(target.Text);
            if (!Directory.Exists(root) || root == Path.GetPathRoot(root)) throw new IOException("请选择已有项目的根目录。");
            RejectLinks(root);
            string harness = Path.Combine(root, ".harness"); RejectLinks(harness); Directory.CreateDirectory(harness);
            // Exclusive per-project runner lock; disposal releases it even on failure.
            projectLock = new FileStream(Path.Combine(harness, "component-import.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            string imports = Path.Combine(harness, "component-imports"); RejectLinks(imports); Directory.CreateDirectory(imports);
            string id = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];
            jobDirectory = Path.Combine(imports, id); Directory.CreateDirectory(jobDirectory); report.Enabled = true;
            string source = Path.Combine(jobDirectory, "source"); Directory.CreateDirectory(source);
            Extract(source, leaderboard.Checked, console.Checked);
            if (console.Checked) NewText(Path.Combine(source, "Unity", "DeveloperConsoleParameters.json"), CustomParameterEditor.Configuration(parameters));
            string taskDirectory = Path.Combine(root, "tasks", "imports"); RejectLinks(taskDirectory); Directory.CreateDirectory(taskDirectory);
            var tasks = new List<string>();
            void AddTask(string key, string name, string acceptance)
            {
                string path = Path.Combine(taskDirectory, $"TASK-{id}-{key}.md");
                NewText(path, $"# 组件接入任务：{name}\n\n状态：待 Codex 接入\n\n" +
                    $"目标项目：{root}\n素材：{source}\n来源：群岛 TASK-006 / TASK-008；参见 source/provenance.json\n\n" +
                    "## 用户请求\n将选中组件以最小依赖接入当前项目，复用提供的源码并完成可用入口及真实绑定。\n\n" +
                    $"## 接入要求\n{acceptance}\n\n## 补充要求\n{requirements.Text}\n\n" +
                    "## 交付\n交付实际修改的代码、接入位置、使用方法、编译结果和未验证项；只有存在可用入口与绑定后才可标记交付。\n" +
                    "这是导入请求单，不含 Harness 机器状态。若项目使用 Harness，先按该项目流程建立正式 Task 并在此记录关联路径；不得伪造机器状态或审批。\n" +
                    "不得覆盖用户既有改动；不得擅自提交、推送或平台发布。遇到需要产品决策的歧义写明阻塞原因。\n");
                tasks.Add(path);
            }
            if (leaderboard.Checked) AddTask("leaderboard", "排行榜", "接入首页入口、列表、本人成绩固定栏、排序、加载/空/失败/重试。使用项目真实成绩来源。微信小游戏使用开放数据域，好友数据不回传主域；其他平台接项目后端，示例数据不能冒充真实服务。按项目风格适配 UI。");
            if (console.Checked) AddTask("developer-console", "开发者控制台", CustomParameterEditor.TaskDescription(parameters));
            var request = new { id, root, tasks, source, components = new { leaderboard = leaderboard.Checked, developerConsole = console.Checked }, parameters, requirements = requirements.Text };
            NewText(Path.Combine(jobDirectory, "request.json"), JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true }));
            string schema = Path.Combine(jobDirectory, "result-schema.json");
            NewText(schema, """
                {"type":"object","additionalProperties":false,"required":["status","summary","files","remaining"],"properties":{"status":{"type":"string","enum":["delivered","blocked","incomplete"]},"summary":{"type":"string"},"files":{"type":"array","items":{"type":"string"}},"remaining":{"type":"array","items":{"type":"string"}}}}
                """);
            string final = Path.Combine(jobDirectory, "delivery.json");
            string prompt = $"用户通过本地组件看板勾选并要求自动接入和交付。请求详见 {Path.Combine(jobDirectory, "request.json")}。\n" +
                "请读取项目 AGENTS.md、所列 Task 请求单与 source/README.md，检查真实工程，按项目既有约定完成选中组件的接入。" +
                "源码已提供，优先直接复用并做最小适配，不要仅复制文件或返回计划。开发者控制台是调参框架，不是命令执行终端。" +
                "如项目无源码/引擎、缺少真实数据接口或有未决产品问题，返回 blocked 和具体所需信息，不凭空建立另一个游戏。" +
                "如项目要求 worktree/正式 Harness Task，遵守现有规则并将关联路径写回请求单；不得因为导入按钮而假定已有基线提交或审批。" +
                "用户此前要求不运行测试：只做必要编译，不运行自动化测试套件。交付报告列出未验证项，不能把编译通过当作真机验证。" +
                "完成后更新这些请求单的状态、实际修改文件和接入方法，并将可读交付说明写入 " + Path.Combine(jobDirectory, "DELIVERY.md") + ".\n" +
                "保持已有用户改动，不擅自提交/推送/发布，不读取或写入源群岛工程。只有真正接入完成才返回 delivered。";
            NewText(Path.Combine(jobDirectory, "prompt.txt"), prompt);
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!); File.WriteAllText(settingsPath, root);
            Append("源码已导入：" + source); foreach (var task in tasks) Append("已生成 Task：" + task);
            status.Text = "Codex 正在接入组件…";
            var info = new ProcessStartInfo(executable) { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(false), StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
            foreach (var arg in new[] { "exec", "--sandbox", "workspace-write", "--skip-git-repo-check", "--json", "--color", "never", "--cd", root,
                "--output-schema", schema, "--output-last-message", final, "-" }) info.ArgumentList.Add(arg);
            WriteState("starting");
            process = Process.Start(info) ?? throw new IOException("Codex 进程未启动。");
            WriteState("running");
            var stdout = PumpAsync(process.StandardOutput, Path.Combine(jobDirectory, "events.jsonl"), true);
            var stderr = PumpAsync(process.StandardError, Path.Combine(jobDirectory, "stderr.log"), false);
            await process.StandardInput.WriteAsync(prompt); process.StandardInput.Close();
            await process.WaitForExitAsync(); await Task.WhenAll(stdout, stderr);
            if (canceled) { status.Text = "已停止，已产生的代码和任务记录保留。"; WriteState("stopped"); }
            else if (process.ExitCode != 0) { status.Text = "Codex 执行失败，请查看下方日志。"; WriteState("failed"); }
            else if (!File.Exists(final)) { status.Text = "Codex 已退出，但没有交付结果。"; WriteState("incomplete"); }
            else {
                using var result = JsonDocument.Parse(File.ReadAllText(final));
                string resultStatus = result.RootElement.GetProperty("status").GetString() ?? "incomplete";
                status.Text = resultStatus == "delivered" ? "Codex 已报告交付完成，请查看交付目录。" : resultStatus == "blocked" ? "接入遇到阻塞，详见下方结果。" : "接入尚未完成，详见下方结果。";
                Append(result.RootElement.GetProperty("summary").GetString() ?? "");
                foreach (var item in result.RootElement.GetProperty("remaining").EnumerateArray()) Append("待处理：" + item.GetString());
                WriteState(resultStatus);
            }
        }
        catch (Exception e)
        {
            // Never orphan a still-writing child if a log/stdin operation failed.
            try { if (process is { HasExited: false }) { process.Kill(true); await process.WaitForExitAsync(); } } catch { }
            status.Text = "未完成：" + e.Message; Append(e.Message);
            try { if (jobDirectory != null) WriteState("failed"); } catch { }
        }
        finally { process?.Dispose(); process = null; projectLock?.Dispose(); Busy(false); }
    }
    private void WriteState(string state)
    {
        File.WriteAllText(Path.Combine(jobDirectory!, "status.json"), JsonSerializer.Serialize(new {
            state, updatedAt = DateTimeOffset.Now, pid = process?.Id
        }));
    }
    private async Task PumpAsync(StreamReader reader, string file, bool events)
    {
        using var writer = new StreamWriter(file, false, new UTF8Encoding(false)) { AutoFlush = true };
        while (await reader.ReadLineAsync() is { } line)
        {
            await writer.WriteLineAsync(line);
            if (!events) { Append(line); continue; }
            try {
                using var json = JsonDocument.Parse(line); var root = json.RootElement;
                if (root.TryGetProperty("thread_id", out var thread)) Append("Codex 会话：" + thread.GetString());
                if (root.TryGetProperty("item", out var item)) {
                    if (item.TryGetProperty("text", out var text)) Append(text.GetString() ?? "");
                    else if (item.TryGetProperty("command", out var command)) Append("执行：" + command.GetString());
                    else if (item.TryGetProperty("type", out var kind)) Append("进度：" + kind.GetString());
                }
                else if (root.TryGetProperty("type", out var type) && type.GetString() is "error" or "turn.failed") Append(line);
            } catch (JsonException) { Append(line); }
        }
    }
    private static void Extract(string destination, bool ranking, bool tuning)
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.StartsWith("kit/")))
        {
            string path = resource[4..].Replace('\\', '/');
            bool include = path.EndsWith(".md") || path == "provenance.json" ||
                (ranking && (path.Contains("Leaderboard") || path.StartsWith("WeChat/"))) ||
                (tuning && (path.Contains("DeveloperConsole") || path.Contains("UnityParameterStore")));
            if (!include) continue;
            string output = Path.GetFullPath(Path.Combine(destination, path));
            if (!output.StartsWith(destination + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("Unsafe resource path");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            using var input = assembly.GetManifestResourceStream(resource)!;
            using var stream = new FileStream(output, FileMode.CreateNew); input.CopyTo(stream);
        }
    }
}
