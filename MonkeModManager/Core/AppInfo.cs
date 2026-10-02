using System;
using System.Reflection;

namespace MonkeModManager.Core
{
    internal static class AppInfo
    {
        public const string RepoOwner = "AABABABABABABBABAABABABABABBA";
        public const string RepoName = "MonkeModManager-Updated";
        public const string RepoSlug = RepoOwner + "/" + RepoName;

        /// <summary>Live mod list. HEAD always follows the repository's default branch.</summary>
        public const string ModListUrl = "https://raw.githubusercontent.com/" + RepoSlug + "/HEAD/mods.json";

        public const string ReleasesPage = "https://github.com/" + RepoSlug + "/releases/latest";

        public const string GameExeName = "Gorilla Tag.exe";
        public const string GameProcessName = "Gorilla Tag";

        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public static string VersionString
        {
            get
            {
                var v = Version;
                return v.Build > 0 ? $"{v.Major}.{v.Minor}.{v.Build}" : $"{v.Major}.{v.Minor}";
            }
        }
    }
}
