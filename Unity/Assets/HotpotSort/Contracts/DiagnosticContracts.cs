using System;
using System.Threading.Tasks;

namespace HotpotSort.Contracts
{
    public sealed class ReplayPackage
    {
        public string SessionId { get; }
        public string SchemaVersion { get; }
        public string CanonicalJson { get; }
        public ReplayPackage(string sessionId, string schemaVersion, string canonicalJson)
        {
            SessionId = ChallengeContext.Require(sessionId, nameof(sessionId));
            SchemaVersion = ChallengeContext.Require(schemaVersion, nameof(schemaVersion));
            CanonicalJson = ChallengeContext.Require(canonicalJson, nameof(canonicalJson));
        }
    }
    public sealed class DiagnosticResult
    {
        public bool Success { get; }
        public string Location { get; }
        public string Error { get; }
        public DiagnosticResult(bool success, string location, string error)
        { Success = success; Location = location; Error = error; }
    }
    public interface IDiagnosticSink
    {
        Task<DiagnosticResult> SaveAsync(ReplayPackage package);
    }
    public sealed class RuntimePaths
    {
        public string DataRoot { get; }
        public string CacheRoot { get; }
        public string BuildRoot { get; }
        public RuntimePaths(string dataRoot, string cacheRoot, string buildRoot)
        {
            DataRoot = ChallengeContext.Require(dataRoot, nameof(dataRoot));
            CacheRoot = ChallengeContext.Require(cacheRoot, nameof(cacheRoot));
            BuildRoot = ChallengeContext.Require(buildRoot, nameof(buildRoot));
        }
    }
}
