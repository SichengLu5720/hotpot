using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace HarnessModelDashboard;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var root = FindRoot();
        if (args.Contains("--validate", StringComparer.OrdinalIgnoreCase)) { ModelConfig.Validate(root); return; }
        ApplicationConfiguration.Initialize();
        Application.Run(new Dashboard(root));
    }

    private static string FindRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "models.toml")) && Directory.Exists(Path.Combine(d.FullName, ".codex", "agents"))) return d.FullName;
        throw new InvalidOperationException("找不到 Harness 项目目录。请把程序放在项目根目录。");
    }
}

internal sealed class Dashboard : Form
{
    private const string Inherit = "继承宿主";
    private static readonly (string Key, string Label)[] Agents = [("code_agent", "Code Agent"), ("visual_agent", "Visual Agent")];
    private readonly string root;
    private readonly Dictionary<string, (ComboBox Model, ComboBox Effort)> fields = [];
    private readonly Label status = new();

    public Dashboard(string root)
    {
        this.root = root;
        Text = "Harness Lite 模型看板"; Font = new Font("Microsoft YaHei UI", 9F); BackColor = Color.FromArgb(247, 248, 250);
        ClientSize = new Size(800, 340); StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Color.FromArgb(38, 132, 214) });
        Controls.Add(new Label { Text = "Agent 模型设置", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Location = new Point(24, 24) });
        Controls.Add(new Label { Text = "两个轻量执行角色；Integrator 由其中一个 Agent 兼任", AutoSize = true, ForeColor = Color.DimGray, Location = new Point(24, 52) });
        var grid = new TableLayoutPanel { Location = new Point(24, 88), Size = new Size(752, 126), ColumnCount = 3, RowCount = 3, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single, BackColor = Color.White };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        foreach (var text in new[] { "Subagent", "模型", "强度" }) grid.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(9,0,0,0), Font = new Font(Font, FontStyle.Bold) }, Array.IndexOf(new[] { "Subagent", "模型", "强度" }, text), 0);
        for (var i = 0; i < Agents.Length; i++)
        {
            grid.Controls.Add(new Label { Text = Agents[i].Label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(9,0,0,0) }, 0, i + 1);
            var model = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown, Margin = new Padding(5) };
            model.Items.AddRange([Inherit, "gpt-6-astra", "gpt-5.6-sol", "gpt-5.6-terra", "gpt-5.6-luna", "gpt-5.5"]);
            var effort = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(5) };
            effort.Items.AddRange(["none", "minimal", "low", "medium", "high", "xhigh", "max", "ultra"]);
            grid.Controls.Add(model, 1, i + 1); grid.Controls.Add(effort, 2, i + 1); fields[Agents[i].Key] = (model, effort);
        }
        Controls.Add(grid);
        status.Location = new Point(24, 246); status.Size = new Size(500, 32); Controls.Add(status);
        var refresh = new Button { Text = "刷新", Location = new Point(580, 242), Size = new Size(82, 32) }; refresh.Click += (_, _) => Reload(); Controls.Add(refresh);
        var save = new Button { Text = "保存应用", Location = new Point(674, 242), Size = new Size(100, 32) }; save.Click += (_, _) => Save(); Controls.Add(save);
        Reload();
    }
    private void Reload()
    {
        try { foreach (var (key, value) in ModelConfig.Load(root)) { var f = fields[key]; f.Model.Text = string.IsNullOrWhiteSpace(value.Model) ? Inherit : value.Model; f.Effort.Text = value.Effort; } status.Text = "配置已同步"; status.ForeColor = Color.ForestGreen; } catch (Exception e) { status.Text = e.Message; status.ForeColor = Color.Firebrick; }
    }
    private void Save()
    {
        try { ModelConfig.Save(root, fields.ToDictionary(x => x.Key, x => new ModelValue(x.Value.Model.Text == Inherit ? "" : x.Value.Model.Text.Trim(), x.Value.Effort.Text))); status.Text = "已保存；新配置对之后启动的 Agent 生效"; status.ForeColor = Color.ForestGreen; } catch (Exception e) { status.Text = e.Message; status.ForeColor = Color.Firebrick; }
    }
}

internal sealed record ModelValue(string Model, string Effort);
internal static class ModelConfig
{
    private static readonly Dictionary<string,string> Files = new() { ["code_agent"]="code-agent.toml", ["visual_agent"]="visual-agent.toml" };
    private static readonly Regex Section = new(@"^\[agents\.([a-z0-9_]+)\]$");
    private static readonly Regex Value = new("^(model|effort)\\s*=\\s*\"(.*)\"$");
    private static readonly Regex Managed = new(@"(?ms)^# BEGIN HARNESS MODEL\r?\n.*?^# END HARNESS MODEL\r?\n?");
    private static readonly Regex DirectModelValue = new(@"(?m)^(model|model_reasoning_effort)\s*=.*\r?\n");
    public static Dictionary<string,ModelValue> Load(string root)
    {
        var output = new Dictionary<string,ModelValue>(); string? section = null;
        foreach (var raw in File.ReadAllLines(Path.Combine(root,"models.toml"))) { var line = raw.Trim(); var s = Section.Match(line); if(s.Success){ section=s.Groups[1].Value; output[section]=new("","medium"); continue; } var v=Value.Match(line); if(section is null || !v.Success) continue; var old=output[section]; output[section]=v.Groups[1].Value=="model" ? old with { Model=Regex.Unescape(v.Groups[2].Value) } : old with { Effort=v.Groups[2].Value }; }
        return output;
    }
    public static void Validate(string root)
    {
        var config=Load(root); if(!config.Keys.ToHashSet().SetEquals(Files.Keys)) throw new InvalidOperationException("models.toml 必须且只能包含当前两个 Agent。");
        foreach(var (key,value) in config) { if(!new[]{"none","minimal","low","medium","high","xhigh","max","ultra"}.Contains(value.Effort)) throw new InvalidOperationException("强度无效。"); var agentPath=Path.Combine(root,".codex","agents",Files[key]); if(!File.Exists(agentPath)) throw new InvalidOperationException("缺少 Agent 配置。"); var agentText=File.ReadAllText(agentPath); var bodyAt=agentText.IndexOf("developer_instructions = \"\"\"",StringComparison.Ordinal); var header=bodyAt>=0 ? agentText[..bodyAt] : agentText; if(Regex.Matches(header,@"(?m)^model\s*=").Count!=1 || Regex.Matches(header,@"(?m)^model_reasoning_effort\s*=").Count!=1) throw new InvalidOperationException($"Agent 模型字段必须且只能各有一项：{key}"); if(!header.Contains($"model = \"{value.Model}\"",StringComparison.Ordinal) || !header.Contains($"model_reasoning_effort = \"{value.Effort}\"",StringComparison.Ordinal)) throw new InvalidOperationException($"Agent 模型配置与 models.toml 不一致：{key}"); }
        var dispatchPath=Path.Combine(root,".codex","config.toml"); if(!File.Exists(dispatchPath)) throw new InvalidOperationException("缺少 .codex/config.toml 调度映射。");
        var dispatch=File.ReadAllText(dispatchPath); foreach(var (key,file) in Files) { if(!dispatch.Contains($"[agents.{key}]",StringComparison.Ordinal) || !dispatch.Contains($"config_file = \"./agents/{file}\"",StringComparison.Ordinal)) throw new InvalidOperationException($"Agent 调度映射无效：{key}"); }
    }
    public static void Save(string root, Dictionary<string,ModelValue> values)
    {
        foreach(var v in values.Values) if(!new[]{"none","minimal","low","medium","high","xhigh","max","ultra"}.Contains(v.Effort)) throw new InvalidOperationException("强度无效。");
        var lines=new List<string>{"# Harness model targets. Blank model means inherit the host default.","schema_version = 1",""}; foreach(var key in Files.Keys){ var v=values[key]; lines.Add($"[agents.{key}]"); lines.Add($"model = \"{v.Model.Replace("\\","\\\\").Replace("\"","\\\"")}\""); lines.Add($"effort = \"{v.Effort}\""); lines.Add(""); }
        File.WriteAllLines(Path.Combine(root,"models.toml"),lines,new UTF8Encoding(false));
        foreach(var (key,file) in Files) { var path=Path.Combine(root,".codex","agents",file); var text=File.ReadAllText(path); var v=values[key]; var block=$"# BEGIN HARNESS MODEL\nmodel = \"{v.Model}\"\nmodel_reasoning_effort = \"{v.Effort}\"\n# END HARNESS MODEL\n"; var bodyAt=text.IndexOf("developer_instructions = \"\"\"",StringComparison.Ordinal); var header=bodyAt>=0 ? text[..bodyAt] : text; var body=bodyAt>=0 ? text[bodyAt..] : ""; header=Managed.Replace(header,""); header=DirectModelValue.Replace(header,""); if(!header.Contains("sandbox_mode",StringComparison.Ordinal)) throw new InvalidOperationException($"Agent 缺少 sandbox_mode：{key}"); header=header.Replace("sandbox_mode",block+"sandbox_mode",StringComparison.Ordinal); File.WriteAllText(path,header+body,new UTF8Encoding(false)); }
    }
}
