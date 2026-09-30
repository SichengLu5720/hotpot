using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HotpotSort.Build
{
    public sealed class FontSubsetBuildGuard : IPreprocessBuildWithReport
    {
        public const string Root = "Assets/HotpotSort/Resources/Hotpot/TASK002/v9/r001/fonts/";
        public int callbackOrder => -900;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.WebGL) Validate();
        }
        // Inventory is intentionally conservative: all non-comment literal characters, including diagnostics
        // that can be displayed by production error handling. Runtime names in open-data are excluded.
        public static string Inventory()
        {
            var chars = new SortedSet<char>(Enumerable.Range(32,95).Select(i => (char)i));
            var files = Directory.GetFiles("Assets/HotpotSort/Runtime", "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/HotpotSort/Contracts", "*.cs", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles("Assets/HotpotSort/Resources", "*.json", SearchOption.AllDirectories).Where(p=>!p.Replace('\\','/').Contains("/TASK002/")));
            var tokens = new Regex(@"//[^\r\n]*|/\*[\s\S]*?\*/|@""(?:""""|[^""])*""|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])+'", RegexOptions.CultureInvariant);
            foreach (var file in files.OrderBy(p => p, StringComparer.Ordinal))
            {
                var normalized = file.Replace('\\','/');
                if (normalized.Contains("/Editor/") || normalized.Contains("/Diagnostics~/")) continue;
                foreach (Match match in tokens.Matches(File.ReadAllText(file)))
                {
                    string literal = match.Value;
                    if (literal.StartsWith("//") || literal.StartsWith("/*")) continue;
                    string content = literal.StartsWith("@\"") ? literal.Substring(2,literal.Length-3).Replace("\"\"","\"") : Decode(literal.Substring(1,literal.Length-2));
                    foreach (char ch in content)
                    {
                        if(char.IsSurrogate(ch)) throw new BuildFailedException("TASK002 supplementary runtime glyph requires an explicit font contract");
                        if(!char.IsControl(ch)) chars.Add(ch);
                    }
                }
            }
            return new string(chars.ToArray());
        }
        private static string Decode(string value)
        {
            return Regex.Replace(value, @"\\(?:u[0-9a-fA-F]{4}|U[0-9a-fA-F]{8}|x[0-9a-fA-F]{1,4}|.)", m => {
                string token=m.Value.Substring(1);
                if (token[0]=='u' || token[0]=='U' || token[0]=='x') return char.ConvertFromUtf32(Convert.ToInt32(token.Substring(1),16));
                switch(token) { case "n": return "\n"; case "r": return "\r"; case "t": return "\t"; case "0": return "\0"; case "a": return "\a"; case "b": return "\b"; case "f": return "\f"; case "v": return "\v"; default:return token; }
            });
        }
        public static void Validate()
        {
            var font=AssetDatabase.LoadAssetAtPath<Font>(Root+"modern-sans.otf");
            if (!font) throw new BuildFailedException("TASK002 v9 modern sans subset missing");
            if (!File.Exists(Root+"glyphs.txt") || !File.Exists(Root+"font-manifest.json") || !File.Exists(Root+"OFL.txt")) throw new BuildFailedException("TASK002 v9 font provenance missing");
            string inventory=Inventory();
            string glyphs=File.ReadAllText(Root+"glyphs.txt");
            var missing=inventory.Where(c => !glyphs.Contains(c.ToString()) || !font.HasCharacter(c)).ToArray();
            if(missing.Length>0) throw new BuildFailedException("TASK002 v9 missing glyphs: "+string.Join(",",missing.Select(c=>"U+"+((int)c).ToString("X4"))));
            var importer=AssetImporter.GetAtPath(Root+"modern-sans.otf") as TrueTypeFontImporter;
            if(importer==null || !importer.includeFontData) throw new BuildFailedException("TASK002 v9 font data must be embedded");
            var manifest=JsonUtility.FromJson<FontManifest>(File.ReadAllText(Root+"font-manifest.json"));
            if(manifest==null || Hash(Root+"modern-sans.otf")!=manifest.fontSha256 || Hash(Root+"glyphs.txt")!=manifest.glyphsSha256 || Hash(Root+"OFL.txt")!=manifest.licenseSha256)
                throw new BuildFailedException("TASK002 v9 font evidence hash mismatch");
            Debug.Log("HOTPOT_TASK002_FONT_GUARD_OK glyphs="+inventory.Length);
        }
        private static string Hash(string path)
        {
            using(var sha=SHA256.Create()) using(var stream=File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        [Serializable] private sealed class FontManifest { public string fontSha256="",glyphsSha256="",licenseSha256=""; }
    }
}
