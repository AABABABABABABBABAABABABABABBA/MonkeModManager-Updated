using System.Collections.Generic;
using MonkeModManager.Internals.SimpleJSON;

namespace MonkeModManager.Core
{
    /// <summary>One entry from mods.json plus the release info resolved from GitHub.</summary>
    internal class ModInfo
    {
        // From mods.json
        public string Name;
        public string Author;
        public string GitPath;
        public string Group;
        public string InstallLocation;
        public bool Required;
        /// <summary>Only releases whose tag starts with this are considered (e.g. "v5." for BepInEx).</summary>
        public string TagPrefix;
        /// <summary>Regex picking the release asset to download.</summary>
        public string AssetPattern;
        /// <summary>Legacy: index of the asset in the release (used when no AssetPattern is given).</summary>
        public int ReleaseId;
        public string FallbackVersion;
        public string FallbackUrl;
        public readonly List<string> Dependencies = new List<string>();
        public readonly List<string> Dependents = new List<string>();

        // Resolved at runtime
        public string Version;
        public string DownloadUrl;
        public string ResolveError;

        public bool IsBepInEx => Name == "BepInEx";
        public bool IsResolved => !string.IsNullOrEmpty(DownloadUrl);

        public static ModInfo FromJson(JSONNode node)
        {
            var mod = new ModInfo
            {
                Name = node["name"],
                Author = (string)node["author"] ?? "",
                GitPath = node["gitPath"],
                Group = node["group"],
                InstallLocation = NullIfEmpty(node["installPath"]),
                Required = node["required"].AsBool,
                TagPrefix = NullIfEmpty(node["tagPrefix"]),
                AssetPattern = NullIfEmpty(node["assetPattern"]),
                ReleaseId = node["releaseId"].AsInt,
                FallbackVersion = NullIfEmpty(node["fallbackVersion"]),
                FallbackUrl = NullIfEmpty(node["fallbackUrl"]),
            };

            var deps = node["dependencies"].AsArray;
            if (deps != null)
            {
                for (int i = 0; i < deps.Count; i++)
                    mod.Dependencies.Add(deps[i]);
            }
            return mod;
        }

        private static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;
    }
}
