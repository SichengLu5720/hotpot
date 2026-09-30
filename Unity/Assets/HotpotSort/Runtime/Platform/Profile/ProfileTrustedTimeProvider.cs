using System;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Session;

namespace HotpotSort.Profile
{
    // Optional TimeResolver trusted provider. Offline snapshots deliberately throw so
    // TimeResolver uses and labels its existing DeviceTime fallback, never false trust.
    public sealed class ProfileTrustedTimeProvider:ITimeProvider
    {
        readonly IAsyncProfileStore profile;
        public ProfileTrustedTimeProvider(IAsyncProfileStore profile){this.profile=profile??throw new ArgumentNullException(nameof(profile));}
        public Task<DateTimeOffset> GetUtcAsync()
        {
            var time=profile.TimeSnapshot;
            if(time.Source!=ProfileTimeSource.TrustedServer)throw new InvalidOperationException("Profile trusted time unavailable; use device fallback");
            return Task.FromResult(time.Utc);
        }
    }
}
