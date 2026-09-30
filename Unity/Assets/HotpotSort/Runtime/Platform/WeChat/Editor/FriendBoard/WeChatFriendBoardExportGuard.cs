using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace HotpotSort.Platform.Editor
{
    public static class WeChatFriendBoardExportGuard
    {
        public const string ProjectSource = "Assets/HotpotSort/WeChatOpenData";
        public static void ValidateSource(string sourceDirectory)
        {
            string entry=Path.Combine(sourceDirectory,"index.js");
            if (!File.Exists(entry)) throw new InvalidDataException("Formal friend board source missing");
            var scripts=Directory.GetFiles(sourceDirectory,"*.js",SearchOption.AllDirectories);
            if (scripts.Length != 1) throw new InvalidDataException("Friend board must be a self-contained entry");
            string code=File.ReadAllText(entry);
            foreach (var forbidden in new[] { @"Math\s*\.\s*random", @"getGroupCloudStorage", @"showGroupFriendsRank",
                @"user_rank", @"最强战力", @"shareMessageToFriend", @"console\s*\.", @"\brequire\s*\(", @"\brequirePlugin\s*\(", @"\bimport\s" })
                if (Regex.IsMatch(code,forbidden)) throw new InvalidDataException("Non-production open-data content detected");
            foreach (var required in new[] { "hotpot_first_wins_v1", "viewEpoch", "requestId", "getFriendCloudStorage", "getUserCloudStorage", "setUserCloudStorage" })
                if (!code.Contains(required)) throw new InvalidDataException("Missing friend board protocol implementation");
        }
        public static void ValidateExport(string packageDirectory,string sourceDirectory)
        {
            ValidateSource(sourceDirectory);
            string destination=Path.Combine(packageDirectory,"open-data");
            ValidateSource(destination);
            string game=File.ReadAllText(Path.Combine(packageDirectory,"game.json"));
            var fields=Regex.Matches(game,"\"openDataContext\"\\s*:\\s*\"([^\"]*)\"");
            if (fields.Count != 1 || fields[0].Groups[1].Value.TrimEnd('/') != "open-data")
                throw new InvalidDataException("Missing or ambiguous openDataContext binding");
            if (!Digest(Path.Combine(sourceDirectory,"index.js")).SequenceEqual(Digest(Path.Combine(destination,"index.js"))))
                throw new InvalidDataException("Exported friend board differs from validated source");
        }
        private static byte[] Digest(string path)
        { using (var sha=SHA256.Create()) using (var input=File.OpenRead(path)) return sha.ComputeHash(input); }
    }
}
