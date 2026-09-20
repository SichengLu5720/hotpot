// TASK-006: working/saved separation, explicit persistence and development gate.
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Harness.Reusable
{
    public interface IParameterStore
    {
        IDictionary<string, double> Load();
        void Save(IDictionary<string, double> values);
    }

    public sealed class ParameterDefinition
    {
        public string Key { get; }
        public string Label { get; }
        public double Minimum { get; }
        public double Maximum { get; }
        public double Step { get; }
        public double Default { get; }
        public double ReleaseValue { get; }
        public ParameterDefinition(string key, string label, double minimum, double maximum,
            double step, double defaultValue, double releaseValue)
        {
            if (string.IsNullOrWhiteSpace(key) || !Finite(minimum) || !Finite(maximum) ||
                !Finite(step) || step <= 0 || minimum > maximum || !Finite(defaultValue) ||
                defaultValue < minimum || defaultValue > maximum || !Finite(releaseValue))
                throw new ArgumentException("Invalid parameter definition.");
            Key = key; Label = label; Minimum = minimum; Maximum = maximum;
            Step = step; Default = defaultValue; ReleaseValue = releaseValue;
        }
        internal static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
        internal double Clamp(double value) => Math.Max(Minimum, Math.Min(Maximum, value));
        internal double Quantize(double value) => Clamp(Minimum + Math.Round((Clamp(value) - Minimum) / Step) * Step);
    }

    public sealed class DeveloperConsole
    {
        private readonly IParameterStore store;
        private readonly Dictionary<string, double> working = new Dictionary<string, double>();
        private Dictionary<string, double> saved;
        private readonly Dictionary<string, ParameterDefinition> definitions;
        public bool Enabled { get; }
        public IReadOnlyList<ParameterDefinition> Parameters { get; }
        public string LoadWarning { get; private set; } = "";
        public event Action Changed;
        public bool Dirty => working.Any(p => saved[p.Key] != p.Value);
        public DeveloperConsole(bool development, IEnumerable<ParameterDefinition> parameters, IParameterStore store)
        {
            Enabled = development; this.store = store ?? throw new ArgumentNullException(nameof(store));
            var list = parameters.ToList(); definitions = list.ToDictionary(p => p.Key);
            Parameters = list.AsReadOnly();
            foreach (var p in list) working[p.Key] = development ? p.Default : p.ReleaseValue;
            if (development)
            {
                try {
                    var loaded = store.Load();
                    foreach (var p in list)
                        if (loaded.TryGetValue(p.Key, out var value) && ParameterDefinition.Finite(value) && value >= p.Minimum && value <= p.Maximum)
                            working[p.Key] = value;
                } catch (Exception e) { LoadWarning = e.Message; }
            }
            saved = new Dictionary<string, double>(working);
        }
        public double Get(string key) => working[key];
        public double Saved(string key) => saved[key];
        public void Set(string key, double value)
        {
            if (!Enabled) return;
            if (!ParameterDefinition.Finite(value)) throw new ArgumentException("Value must be finite.");
            var next = definitions[key].Quantize(value);
            if (working[key] == next) return;
            working[key] = next; Changed?.Invoke();
        }
        // Preserve the relative ratio, stopping both at their shared boundary.
        public void SetLinked(string changed, string other, double value)
        {
            if (!Enabled) return;
            if (changed == other || !ParameterDefinition.Finite(value) || working[changed] <= 0 || working[other] <= 0)
                throw new ArgumentException("Linked scales require distinct positive values.");
            var a = definitions[changed]; var b = definitions[other];
            var ratio = a.Quantize(value) / working[changed];
            var min = Math.Max(a.Minimum / working[changed], b.Minimum / working[other]);
            var max = Math.Min(a.Maximum / working[changed], b.Maximum / working[other]);
            ratio = Math.Max(min, Math.Min(max, ratio));
            working[changed] = a.Clamp(working[changed] * ratio);
            working[other] = b.Clamp(working[other] * ratio); Changed?.Invoke();
        }
        public void Save()
        {
            if (!Enabled) return;
            store.Save(new Dictionary<string, double>(working));
            saved = new Dictionary<string, double>(working); Changed?.Invoke();
        }
        public void Discard()
        {
            if (!Enabled) return;
            foreach (var pair in saved) working[pair.Key] = pair.Value;
            Changed?.Invoke();
        }
        public void ResetDefaults()
        {
            if (!Enabled) return;
            foreach (var p in Parameters) working[p.Key] = p.Default;
            Changed?.Invoke();
        }
        public static ParameterDefinition[] DefaultParameters() => new[] {
            new ParameterDefinition("speed", "移动速度", .5, 20, .05, 1.25, 1),
            new ParameterDefinition("platform", "平台视觉比例", .75, 1.25, .01, 1, 1),
            new ParameterDefinition("person", "人物视觉比例", .75, 1.25, .01, 1, 1),
            new ParameterDefinition("linked", "比例联动", 0, 1, 1, 1, 0)
        };
    }
}
