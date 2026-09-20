using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace HotpotSort.Determinism
{
    // canonical_json_v1: ordinal keys, contractual array order, invariant integers,
    // printable Unicode preserved, control characters escaped, UTF-8 without BOM.
    public static class CanonicalJson
    {
        public const string Version = "canonical_json_v1";
        public static Dictionary<string, object> Object(params object[] pairs)
        {
            if (pairs.Length % 2 != 0) throw new ArgumentException("key/value pairs");
            var value = new Dictionary<string, object>(StringComparer.Ordinal);
            for (int i = 0; i < pairs.Length; i += 2) value.Add((string)pairs[i], pairs[i + 1]);
            return value;
        }
        public static string Write(object value)
        {
            var b = new StringBuilder(); Append(b, value); return b.ToString();
        }
        static void Append(StringBuilder b, object value)
        {
            if (value == null) { b.Append("null"); return; }
            if (value is string s) { Quote(b, s); return; }
            if (value is bool flag) { b.Append(flag ? "true" : "false"); return; }
            if (value is IDictionary<string, object> map)
            {
                var keys = new List<string>(map.Keys); keys.Sort(StringComparer.Ordinal);
                b.Append('{'); bool first = true;
                foreach (var key in keys) { if (!first) b.Append(','); first = false; Quote(b, key); b.Append(':'); Append(b, map[key]); }
                b.Append('}'); return;
            }
            if (value is IEnumerable seq)
            {
                b.Append('['); bool first = true;
                foreach (var entry in seq) { if (!first) b.Append(','); first = false; Append(b, entry); }
                b.Append(']'); return;
            }
            if (value is byte || value is short || value is int || value is long || value is uint)
            { b.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
            // UInt64 identities must be explicitly converted to decimal strings by callers.
            throw new ArgumentException("Non-canonical value: " + value.GetType().Name);
        }
        static void Quote(StringBuilder b, string s)
        {
            b.Append('"');
            foreach (char c in s)
            {
                if (c == '"' || c == '\\') b.Append('\\').Append(c);
                else if (c < 32 || char.IsSurrogate(c)) b.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else b.Append(c);
            }
            b.Append('"');
        }
        public static string Hash(string text) => Hash(Encoding.UTF8.GetBytes(text));
        public static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                var b = new StringBuilder(); foreach (byte x in sha.ComputeHash(bytes)) b.Append(x.ToString("x2", CultureInfo.InvariantCulture));
                return b.ToString();
            }
        }
        public static string U64(ulong value) => value.ToString(CultureInfo.InvariantCulture);
        public static ulong ReadU64(object value)
        {
            var s = value as string;
            if (s == null || !ulong.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || U64(n) != s)
                throw new FormatException("Expected canonical UInt64 decimal string");
            return n;
        }
        public static Dictionary<string, object> Map(object value) => value as Dictionary<string, object> ?? throw new FormatException("Expected object");
        public static List<object> Array(object value) => value as List<object> ?? throw new FormatException("Expected array");
        public static int Int(object value)
        {
            if (value is long n && n >= int.MinValue && n <= int.MaxValue) return (int)n;
            if (value is int i) return i;
            throw new FormatException("Expected Int32");
        }
        public static object Parse(string json) => new Parser(json).ReadAll();
        // A strict, culture-independent parser also supports decimal source weights. The
        // canonical writer deliberately refuses decimals: importer converts them first.
        sealed class Parser
        {
            readonly string s; int p;
            public Parser(string text) { s = text ?? throw new ArgumentNullException(nameof(text)); }
            void Space() { while (p < s.Length && (s[p] == ' ' || s[p] == '\r' || s[p] == '\n' || s[p] == '\t')) p++; }
            public object ReadAll() { var v = Read(); Space(); if (p != s.Length) throw new FormatException("Trailing JSON"); return v; }
            char Take() { if (p == s.Length) throw new FormatException("Truncated JSON"); return s[p++]; }
            object Read()
            {
                Space(); if (p == s.Length) throw new FormatException("Missing JSON value");
                char c = s[p];
                if (c == '"') return String();
                if (c == '{')
                {
                    p++; Space(); var d = new Dictionary<string, object>(StringComparer.Ordinal);
                    if (p < s.Length && s[p] == '}') { p++; return d; }
                    while (true) { Space(); var k = String(); Space(); if (Take() != ':') throw new FormatException("Missing colon"); d.Add(k, Read()); Space(); c = Take(); if (c == '}') return d; if (c != ',') throw new FormatException("Missing comma"); }
                }
                if (c == '[')
                {
                    p++; Space(); var a = new List<object>(); if (p < s.Length && s[p] == ']') { p++; return a; }
                    while (true) { a.Add(Read()); Space(); c = Take(); if (c == ']') return a; if (c != ',') throw new FormatException("Missing comma"); }
                }
                foreach (var token in new[] { "true", "false", "null" })
                    if (p + token.Length <= s.Length && s.Substring(p, token.Length) == token)
                    { p += token.Length; return token == "null" ? null : (object)(token == "true"); }
                int start = p; if (c == '-') p++;
                if (p >= s.Length || s[p] < '0' || s[p] > '9') throw new FormatException("Invalid number");
                if (s[p] == '0') p++; else while (p < s.Length && s[p] >= '0' && s[p] <= '9') p++;
                bool fraction = false;
                if (p < s.Length && s[p] == '.') { fraction = true; p++; int at = p; while (p < s.Length && char.IsDigit(s[p])) p++; if (at == p) throw new FormatException("Invalid fraction"); }
                if (p < s.Length && (s[p] == 'e' || s[p] == 'E')) { fraction = true; p++; if (p < s.Length && (s[p] == '+' || s[p] == '-')) p++; int at = p; while (p < s.Length && char.IsDigit(s[p])) p++; if (at == p) throw new FormatException("Invalid exponent"); }
                string number = s.Substring(start, p - start);
                if (fraction) return decimal.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture);
                return long.Parse(number, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            }
            string String()
            {
                if (Take() != '"') throw new FormatException("Expected string"); var b = new StringBuilder();
                while (true)
                {
                    char c = Take(); if (c == '"') return b.ToString(); if (c < 32) throw new FormatException("Control in string");
                    if (c != '\\') { b.Append(c); continue; }
                    c = Take(); switch (c)
                    {
                        case '"': case '\\': case '/': b.Append(c); break;
                        case 'b': b.Append('\b'); break; case 'f': b.Append('\f'); break;
                        case 'n': b.Append('\n'); break; case 'r': b.Append('\r'); break; case 't': b.Append('\t'); break;
                        case 'u': if (p + 4 > s.Length) throw new FormatException("Truncated escape"); b.Append((char)ushort.Parse(s.Substring(p, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); p += 4; break;
                        default: throw new FormatException("Invalid escape");
                    }
                }
            }
        }
    }
}
