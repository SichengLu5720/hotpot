using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Harness.Reusable.Unity
{
    // Assign generated parameterConfiguration and bind real scene targets.
    // No reflection, object-name guessing or hard-coded project parameters.
    public sealed class DeveloperConsoleOverlay : MonoBehaviour
    {
        [Serializable] public sealed class Parameter
        {
            public string key, label, target;
            public float minimum, maximum, step, defaultValue;
        }
        [Serializable] public sealed class Configuration
        {
            public int version = 1;
            public Parameter[] parameters;
        }
        public enum BindingKind { VisualScale, ParticleSpeed, Custom }
        [Serializable] public sealed class FloatEvent : UnityEvent<float> {}
        [Serializable] public sealed class Binding
        {
            public string key;
            public BindingKind kind;
            public Transform visual;
            public ParticleSystem particles;
            public FloatEvent apply = new FloatEvent();
            [NonSerialized] public Vector3 baseScale;
            [NonSerialized] public float baseParticleSpeed;
        }

        public string projectKey = "my-game";
        public TextAsset parameterConfiguration;
        public Binding[] bindings = Array.Empty<Binding>();
        public DeveloperConsole Console { get; private set; }
        private bool open;
        private string error = "";
        private Vector2 scroll;

        private void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            const bool development = true;
#else
            const bool development = false;
#endif
            // Release builds do not parse configuration or invoke debug bindings.
            if (!development) { enabled = false; return; }
            try
            {
                if (!parameterConfiguration) throw new InvalidOperationException("Assign DeveloperConsoleParameters.json.");
                var config = JsonUtility.FromJson<Configuration>(parameterConfiguration.text);
                if (config == null || config.version != 1 || config.parameters == null || config.parameters.Length == 0)
                    throw new InvalidOperationException("No custom debug parameters configured.");
                var definitions = new List<ParameterDefinition>();
                var keys = new HashSet<string>();
                foreach (var p in config.parameters)
                {
                    if (p == null || !keys.Add(p.key)) throw new InvalidOperationException("Duplicate/invalid parameter key.");
                    definitions.Add(new ParameterDefinition(p.key, p.label, p.minimum, p.maximum, p.step, p.defaultValue, p.defaultValue));
                }
                var bound = new HashSet<string>();
                foreach (var b in bindings)
                {
                    if (b == null || !keys.Contains(b.key) || !bound.Add(b.key))
                        throw new InvalidOperationException("Each binding requires a unique configured key.");
                    if (b.kind == BindingKind.VisualScale)
                    {
                        if (!b.visual) throw new InvalidOperationException("Missing visual target: " + b.key);
                        b.baseScale = b.visual.localScale;
                    }
                    else if (b.kind == BindingKind.ParticleSpeed)
                    {
                        if (!b.particles) throw new InvalidOperationException("Missing particle target: " + b.key);
                        b.baseParticleSpeed = b.particles.main.simulationSpeed;
                    }
                    else if (b.apply == null || b.apply.GetPersistentEventCount() == 0)
                        throw new InvalidOperationException("Bind a persistent custom float callback: " + b.key);
                }
                if (bound.Count != keys.Count) throw new InvalidOperationException("Some parameters have no real binding.");
                Console = new DeveloperConsole(true, definitions, new UnityParameterStore(projectKey));
                Console.Changed += ApplyValues;
                ApplyValues();
            }
            catch (Exception e) { error = e.Message; Debug.LogError("Developer console: " + error, this); }
        }
        private void OnDestroy() { if (Console != null) Console.Changed -= ApplyValues; }
        private void ApplyValues()
        {
            if (Console == null || !Console.Enabled) return;
            foreach (var b in bindings)
            {
                float value = (float)Console.Get(b.key);
                if (b.kind == BindingKind.VisualScale && b.visual) b.visual.localScale = b.baseScale * value;
                else if (b.kind == BindingKind.ParticleSpeed && b.particles)
                { var main = b.particles.main; main.simulationSpeed = b.baseParticleSpeed * value; }
                else if (b.kind == BindingKind.Custom) b.apply.Invoke(value);
            }
        }
        private Rect Bounds()
        {
            var safe = Screen.safeArea;
            float width = Mathf.Min(380, safe.width - 16);
            return new Rect(safe.xMax - width - 8, Screen.height - safe.yMax + 8, width,
                open ? Mathf.Min(440, safe.height - 16) : 42);
        }
        // Gate the complete gameplay gesture after a pointer starts on this overlay.
        public bool BlocksPointer(Vector2 screenPosition)
        { return isActiveAndEnabled && Bounds().Contains(new Vector2(screenPosition.x, Screen.height - screenPosition.y)); }
        private void OnGUI()
        {
            GUILayout.BeginArea(Bounds(), GUI.skin.box);
            if (GUILayout.Button(open ? "开发者控制台 · 收起" : "开发者控制台", GUILayout.Height(34))) open = !open;
            if (open)
            {
                if (Console != null)
                {
                    scroll = GUILayout.BeginScrollView(scroll);
                    foreach (var p in Console.Parameters)
                    {
                        float value = (float)Console.Get(p.Key);
                        GUILayout.Label(p.Label + "  " + value.ToString("G5"));
                        float changed = GUILayout.HorizontalSlider(value, (float)p.Minimum, (float)p.Maximum);
                        if (Mathf.Abs(value - changed) > .000001f)
                            try { Console.Set(p.Key, changed); } catch (Exception e) { error = e.Message; }
                    }
                    GUILayout.EndScrollView();
                    GUILayout.Label(Console.Dirty ? "未保存 · 仅本次预览" : "当前参数已同步");
                    if (GUILayout.Button("保存当前参数"))
                        try { Console.Save(); error = ""; } catch (Exception e) { error = e.Message; }
                    if (GUILayout.Button("撤销未保存修改"))
                        try { Console.Discard(); } catch (Exception e) { error = e.Message; }
                }
                if (error.Length > 0) GUILayout.Label(error);
            }
            GUILayout.EndArea();
        }
    }
}
