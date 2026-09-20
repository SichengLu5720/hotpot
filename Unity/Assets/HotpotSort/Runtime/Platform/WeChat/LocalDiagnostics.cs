using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HotpotSort.Contracts;
#if UNITY_WEBGL || UNITY_EDITOR
using WeChatWASM;
#endif
namespace HotpotSort.Platform
{
    public sealed class LocalDiagnostics : IDiagnosticSink
    {
        public RuntimePaths Paths { get; }
        public DiagnosticResult LastResult { get; private set; }
        public LocalDiagnostics(RuntimePaths paths) { Paths = paths ?? throw new ArgumentNullException(nameof(paths)); }
        public Task<DiagnosticResult> SaveAsync(ReplayPackage package)
        {
            string destination = null;
            try
            {
                if (package == null) throw new ArgumentNullException(nameof(package));
                var name = Guid.NewGuid().ToString("N") + ".json";
                destination = Paths.DataRoot.TrimEnd('/', '\\') + "/" + name;
#if UNITY_WEBGL && !UNITY_EDITOR
                var fs = WX.GetFileSystemManager();
                foreach (var directory in new[] { Paths.DataRoot, Paths.CacheRoot })
                {
                    if (fs.AccessSync(directory) != "ok")
                    { var mkdir = fs.MkdirSync(directory, true); if (mkdir != "ok") throw new IOException(mkdir); }
                }
                var result = fs.WriteFileSync(destination, package.CanonicalJson, "utf8");
                if (result != "ok") throw new IOException(result);
#else
                Directory.CreateDirectory(Paths.DataRoot);
                Directory.CreateDirectory(Paths.CacheRoot);
                using (var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) { writer.Write(package.CanonicalJson); writer.Flush(); stream.Flush(true); }
#endif
                LastResult = new DiagnosticResult(true, destination, null);
            }
            catch (Exception ex) { LastResult = new DiagnosticResult(false, destination, ex.GetType().Name + ": " + ex.Message); }
            return Task.FromResult(LastResult);
        }
    }
}
