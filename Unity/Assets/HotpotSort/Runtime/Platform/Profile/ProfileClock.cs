using System;
using HotpotSort.Contracts;

namespace HotpotSort.Profile
{
    public sealed class ProfileClock
    {
        readonly Func<DateTimeOffset> deviceUtc;
        readonly Func<double> monotonic;
        DateTimeOffset trustedUtc;
        double anchor;
        bool trusted;
        public ProfileClock(Func<DateTimeOffset> deviceUtc,Func<double> monotonic)
        {this.deviceUtc=deviceUtc??throw new ArgumentNullException(nameof(deviceUtc));this.monotonic=monotonic??throw new ArgumentNullException(nameof(monotonic));}
        public void Trust(DateTimeOffset utc){trustedUtc=utc.ToUniversalTime();anchor=monotonic();trusted=true;}
        public void Offline()=>trusted=false;
        public ProfileTimeSnapshot Read()
        {
            double elapsed=monotonic()-anchor;
            if(trusted && elapsed>=0 && !double.IsNaN(elapsed) && !double.IsInfinity(elapsed))
                return new ProfileTimeSnapshot(trustedUtc.AddSeconds(elapsed),ProfileTimeSource.TrustedServer);
            trusted=false;return new ProfileTimeSnapshot(deviceUtc(),ProfileTimeSource.DeviceTime);
        }
    }
}
