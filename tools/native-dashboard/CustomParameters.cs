using System.Globalization;
using System.Text.Json;

namespace HarnessModelDashboard;

internal sealed class CustomParameter
{
    public string Key { get; set; } = "parameter_" + Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public string Target { get; set; } = "";
    public double Default { get; set; } = 1;
    public double Minimum { get; set; } = .1;
    public double Maximum { get; set; } = 3;
    public double Step { get; set; } = .01;
}

internal sealed class CustomParameterEditor : UserControl
{
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, BackgroundColor = Color.White, RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.CellSelect };
    private readonly string file;
    public string LoadWarning { get; private set; } = "";
    public CustomParameterEditor(string root)
    {
        Dock = DockStyle.Fill;
        file = Path.Combine(root, ".harness", "custom-console-parameters.json");
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        foreach (var (key, title, width) in new[] {
            ("name", "参数名称", 120), ("target", "调试对象 / 作用说明", 230),
            ("default", "默认值", 75), ("min", "最小值", 75), ("max", "最大值", 75), ("step", "步长", 65) })
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = key, HeaderText = title, FillWeight = width, MinimumWidth = 60, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.RowTemplate.Height = 34;
        body.Controls.Add(grid, 0, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var add = new Button { Text = "添加参数", AutoSize = true };
        add.Click += (_, _) => {
            Add(new CustomParameter()); grid.CurrentCell = grid.Rows[grid.Rows.Count - 1].Cells[0]; grid.BeginEdit(true);
        };
        var remove = new Button { Text = "删除当前行", AutoSize = true };
        remove.Click += (_, _) => { if (grid.CurrentCell != null) grid.Rows.RemoveAt(grid.CurrentCell.RowIndex); };
        var example = new Button { Text = "填入圆球 / 特效示例", AutoSize = true };
        example.Click += (_, _) => {
            Add(new CustomParameter { Name = "圆球大小", Target = "玩家圆球的视觉缩放倍率；不改变碰撞体", Minimum = .1, Maximum = 3 });
            Add(new CustomParameter { Name = "特效速度", Target = "命中特效的粒子播放速度倍率", Minimum = .1, Maximum = 5 });
        };
        var save = new Button { Text = "保存参数列表", AutoSize = true };
        save.Click += (_, _) => {
            try { Save(Read(false)); MessageBox.Show(this, "参数列表已保存，导入时会自动带入 Task。", "已保存"); }
            catch (Exception e) { MessageBox.Show(this, e.Message, "请检查参数"); }
        };
        actions.Controls.AddRange(new Control[] { add, remove, example, save }); body.Controls.Add(actions, 0, 1); Controls.Add(body);
        if (File.Exists(file)) try {
            foreach (var p in JsonSerializer.Deserialize<List<CustomParameter>>(File.ReadAllText(file)) ?? new()) Add(p);
        } catch (Exception e) { LoadWarning = "参数列表读取失败：" + e.Message; }
    }
    private void Add(CustomParameter p)
    {
        int i = grid.Rows.Add(p.Name, p.Target, p.Default, p.Minimum, p.Maximum, p.Step);
        grid.Rows[i].Tag = p.Key;
    }
    public List<CustomParameter> Read(bool required = true)
    {
        grid.EndEdit();
        var result = new List<CustomParameter>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in grid.Rows)
        {
            string name = Convert.ToString(row.Cells[0].Value)?.Trim() ?? "";
            string target = Convert.ToString(row.Cells[1].Value)?.Trim() ?? "";
            if (name.Length == 0) throw new ArgumentException($"第 {row.Index + 1} 行请填写参数名称。");
            if (!names.Add(name)) throw new ArgumentException($"参数名称重复：{name}");
            double Number(int column) {
                string text = Convert.ToString(row.Cells[column].Value) ?? "";
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value) || !double.IsFinite(value))
                    throw new ArgumentException($"{name} 的{grid.Columns[column].HeaderText}必须是有效数字。");
                return value;
            }
            var p = new CustomParameter { Key = (string)row.Tag!, Name = name, Target = target,
                Default = Number(2), Minimum = Number(3), Maximum = Number(4), Step = Number(5) };
            if (p.Minimum >= p.Maximum || !double.IsFinite(p.Maximum - p.Minimum) || p.Step <= 0 || p.Step > p.Maximum - p.Minimum ||
                p.Default < p.Minimum || p.Default > p.Maximum || Math.Abs(p.Minimum) > float.MaxValue || Math.Abs(p.Maximum) > float.MaxValue)
                throw new ArgumentException($"{name}：最小值必须小于最大值，默认值须在范围内，步长须大于 0 且不超过范围。");
            result.Add(p);
        }
        if (required && result.Count == 0) throw new ArgumentException("请先在“开发者控制台”页添加想要调试的参数，例如圆球大小、特效速度。");
        return result;
    }
    public void Save(List<CustomParameter> parameters)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        string temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(parameters, new JsonSerializerOptions { WriteIndented = true })); File.Move(temporary, file, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static string Configuration(List<CustomParameter> parameters) => JsonSerializer.Serialize(new {
        version = 1, parameters = parameters.Select(p => new { key = p.Key, label = p.Name, target = p.Target,
            minimum = p.Minimum, maximum = p.Maximum, step = p.Step, defaultValue = p.Default })
    }, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    public static string TaskDescription(List<CustomParameter> parameters) =>
        "接入用户自定义的开发调参面板、即时预览、显式保存、重启加载及输入隔离。正式构建不应用任何调试覆盖。\n" +
        "每项参数必须绑定项目内真实对象/字段，交付报告列出映射；有多个合理目标或含义不清时返回具体问题。未填写作用说明时依据参数名称查找，不臆造目标。\n" +
        "不要默认添加行走速度、平台比例、人物比例或联动；只实现以下参数。参数是开发调试需求，不授权修改关卡规则与存档。\n\n" +
        string.Join("\n", parameters.Select(p => $"- {p.Name}（{p.Key}）：{p.Target}；默认 {p.Default.ToString(CultureInfo.InvariantCulture)}，范围 {p.Minimum.ToString(CultureInfo.InvariantCulture)}–{p.Maximum.ToString(CultureInfo.InvariantCulture)}，步长 {p.Step.ToString(CultureInfo.InvariantCulture)}。"));
}
