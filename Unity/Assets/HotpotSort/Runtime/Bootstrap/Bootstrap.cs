using System;
using System.IO;
using HotpotSort.Contracts;
using HotpotSort.Session;
using HotpotSort.Platform;
using UnityEngine;
#if UNITY_WEBGL || UNITY_EDITOR
using WeChatWASM;
#endif

namespace HotpotSort.Bootstrap
{
    // Sole production scene entry. Fixed real factories are supplied at integration.
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] private ProductionComposition composition;
        private static Bootstrap owner;
        private WeChatPlatform platform;
        private bool destroyed;
        public SessionController Controller { get; private set; }
        public LocalDiagnostics Diagnostics { get; private set; }
        public bool IsConfigured => Controller != null;
        public string Status { get; private set; } = "not-started";
        private async void Start()
        {
            if (owner != null && owner != this) { Status = "duplicate-bootstrap"; enabled = false; return; }
            owner = this;
            try
            {
                if (composition == null) throw new InvalidOperationException("production-core-view-factories-missing");
                if (composition.UsesDevelopmentDoubles) throw new InvalidOperationException("production-rejects-development-doubles");
                if (string.IsNullOrWhiteSpace(composition.RuntimeNamespace) ||
                    System.Text.RegularExpressions.Regex.IsMatch(composition.RuntimeNamespace, "[^a-zA-Z0-9_-]"))
                    throw new InvalidOperationException("invalid-runtime-namespace");
                Status = "initializing-platform";
                await WeChatPlatform.InitializeAsync();
                if (destroyed) return;
#if UNITY_WEBGL && !UNITY_EDITOR
                var root = WX.env.USER_DATA_PATH + "/" + composition.RuntimeNamespace;
#else
                var configured = Environment.GetEnvironmentVariable("HOTPOT_RUNTIME_ROOT");
                var root = string.IsNullOrWhiteSpace(configured)
                    ? Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/wechat/runtime", composition.RuntimeNamespace))
                    : Path.GetFullPath(Path.Combine(configured, composition.RuntimeNamespace));
#endif
                Diagnostics = new LocalDiagnostics(new RuntimePaths(root + "/data", root + "/cache", root + "/build"));
                composition.BindDiagnostics(Diagnostics);
                platform = new WeChatPlatform();
                Controller = new SessionController(composition.CoreFactory, composition.ViewFactory,
                    new TimeResolver(composition.TrustedTime, new DeviceTimeProvider()), new UnityClock(),
                    platform, composition.ContentVersion, composition.ConfigurationDigest);
                composition.BindSessionObservation(Controller);
                await Controller.StartTodayAsync();
                Status = Controller.Error ?? "ready";
            }
            catch (Exception ex) { Status = ex.GetType().Name + ": " + ex.Message; Debug.LogError(Status); Controller?.Dispose(); Controller = null; platform?.Dispose(); }
        }
        private void OnApplicationPause(bool paused) { platform?.NotifyEditorPause(paused); }
        private void OnDestroy()
        {
            destroyed = true;
            Controller?.Dispose(); platform?.Dispose();
            if (owner == this) owner = null;
        }
    }
}
