using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace HotpotSort.Determinism
{
    public sealed class Pcg32
    {
        public const string Version = "PCG32_v1";
        public const string SeedVersion = "SHA256_U64BE_v1";
        ulong state;
        public ulong State => state;
        public ulong Increment { get; } = 109UL; // (initseq=54 << 1) | 1
        public ulong DrawIndex { get; private set; }
        public Pcg32(ulong seed, string algorithm = Version)
        {
            if (algorithm != Version) throw new ArgumentException("Unknown RNG algorithm");
            Step(); state = unchecked(state + seed); Step(); DrawIndex = 0;
        }
        uint Step()
        {
            ulong old = state; state = unchecked(old * 6364136223846793005UL + Increment);
            uint x = (uint)(((old >> 18) ^ old) >> 27); int r = (int)(old >> 59);
            return (x >> r) | (x << ((-r) & 31));
        }
        public uint Next() { DrawIndex = checked(DrawIndex + 1); return Step(); }
        public uint NextBounded(uint bound, IList<object> trace = null)
        {
            if (bound == 0) throw new ArgumentOutOfRangeException(nameof(bound));
            uint threshold = unchecked(0u - bound) % bound;
            while (true)
            {
                uint raw = Next(); bool accepted = raw >= threshold;
                trace?.Add(CanonicalJson.Object("drawIndex", CanonicalJson.U64(DrawIndex), "raw", raw, "bound", bound,
                    "threshold", threshold, "accepted", accepted, "bounded", accepted ? (object)(raw % bound) : null));
                if (accepted) return raw % bound;
            }
        }
        public object Snapshot() => CanonicalJson.Object("algorithm", Version, "state", CanonicalJson.U64(state), "inc", CanonicalJson.U64(Increment), "drawIndex", CanonicalJson.U64(DrawIndex));
        static ulong DigestSeed(byte[] input)
        {
            using (var sha = SHA256.Create()) { var bytes = sha.ComputeHash(input); ulong result = 0; for (int i = 0; i < 8; i++) result = (result << 8) | bytes[i]; return result; }
        }
        public static ulong DailySeed(string challengeId, string contentVersion, string algorithm = SeedVersion)
        {
            if (algorithm != SeedVersion) throw new ArgumentException("Unknown seed algorithm");
            return DigestSeed(Encoding.UTF8.GetBytes("HOT_POT_DAILY|" + challengeId + "|" + contentVersion));
        }
        public static ulong StreamSeed(ulong seed, string stream)
        {
            if (stream != "MappingRng" && stream != "DirectorRng" && stream != "PresentationRng") throw new ArgumentException("Unknown stream");
            var suffix = Encoding.UTF8.GetBytes("|" + stream); var input = new byte[8 + suffix.Length];
            for (int i = 0; i < 8; i++) input[i] = (byte)(seed >> ((7 - i) * 8));
            System.Array.Copy(suffix, 0, input, 8, suffix.Length); return DigestSeed(input);
        }
    }
}
