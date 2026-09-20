using System;
using System.Globalization;
using System.Threading.Tasks;
using HotpotSort.Contracts;

namespace HotpotSort.Session
{
    public interface ITimeProvider { Task<DateTimeOffset> GetUtcAsync(); }
    public interface IMonotonicClock { double Seconds { get; } }
    public sealed class DeviceTimeProvider : ITimeProvider
    {
        public Task<DateTimeOffset> GetUtcAsync() => Task.FromResult(DateTimeOffset.UtcNow);
    }
    public sealed class ResolvedChallenge
    {
        public ChallengeContext Context { get; }
        public DateTimeOffset ResolvedUtc { get; }
        public string FallbackReason { get; }
        public ResolvedChallenge(ChallengeContext context, DateTimeOffset utc, string fallbackReason)
        { Context = context; ResolvedUtc = utc.ToUniversalTime(); FallbackReason = fallbackReason; }
    }
    public sealed class TimeResolver
    {
        private readonly ITimeProvider trusted, device;
        public TimeResolver(ITimeProvider trusted, ITimeProvider device)
        { this.trusted = trusted; this.device = device ?? throw new ArgumentNullException(nameof(device)); }
        public async Task<ResolvedChallenge> ResolveAsync(string contentVersion, string digest)
        {
            DateTimeOffset utc;
            string source = "trusted", reason = null;
            if (trusted == null) { reason = "trusted-provider-not-configured"; utc = await device.GetUtcAsync(); source = "device"; }
            else
            {
                try { utc = await trusted.GetUtcAsync(); }
                catch (Exception ex) { reason = ex.GetType().Name; utc = await device.GetUtcAsync(); source = "device"; }
            }
            utc = utc.ToUniversalTime();
            // Asia/Shanghai Daily modern date rule: UTC+08:00, independent of device timezone.
            var day = utc.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            return new ResolvedChallenge(new ChallengeContext(day, contentVersion, digest, source, 0), utc, reason);
        }
    }
}
