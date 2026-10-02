using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MonkeModManager.Internals.SimpleJSON;

namespace MonkeModManager.Core
{
    /// <summary>
    /// Works out the newest release + download link for a mod.
    /// Tries the GitHub API first, then falls back to scraping github.com (no rate limit),
    /// then to the fallback URL listed in mods.json.
    /// </summary>
    internal static class ReleaseResolver
    {
        public static async Task ResolveAsync(ModInfo mod)
        {
            mod.ResolveError = null;
            try
            {
                if (await TryResolveViaApiAsync(mod).ConfigureAwait(false)) return;
            }
            catch (Exception ex)
            {
                mod.ResolveError = ex is RateLimitedException ? "GitHub rate limit" : ex.Message;
            }

            try
            {
                if (await TryResolveViaWebsiteAsync(mod).ConfigureAwait(false))
                {
                    mod.ResolveError = null;
                    return;
                }
            }
            catch (Exception ex)
            {
                mod.ResolveError = mod.ResolveError ?? ex.Message;
            }

            if (mod.FallbackUrl != null)
            {
                mod.DownloadUrl = mod.FallbackUrl;
                mod.Version = mod.FallbackVersion;
                mod.ResolveError = null;
                return;
            }

            mod.ResolveError = mod.ResolveError ?? "No matching release found";
        }

        private static async Task<bool> TryResolveViaApiAsync(ModInfo mod)
        {
            JSONNode release;
            if (mod.TagPrefix == null)
            {
                // /releases/latest already skips drafts and pre-releases.
                release = JSON.Parse(await Http.GetGitHubApiAsync($"repos/{mod.GitPath}/releases/latest").ConfigureAwait(false));
            }
            else
            {
                var all = JSON.Parse(await Http.GetGitHubApiAsync($"repos/{mod.GitPath}/releases?per_page=50").ConfigureAwait(false)).AsArray;
                release = null;
                if (all != null)
                {
                    // Newest first; pick the first stable release on the wanted major version.
                    foreach (JSONNode r in all.Children)
                    {
                        if (r["draft"].AsBool || r["prerelease"].AsBool) continue;
                        string tag = r["tag_name"];
                        if (tag != null && tag.StartsWith(mod.TagPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            release = r;
                            break;
                        }
                    }
                }
                if (release == null) return false;
            }

            var assets = release["assets"].AsArray;
            if (assets == null || assets.Count == 0) return false;

            var names = new List<string>();
            var urls = new List<string>();
            foreach (JSONNode asset in assets.Children)
            {
                names.Add(asset["name"]);
                urls.Add(asset["browser_download_url"]);
            }

            int index = PickAsset(mod, names);
            if (index < 0) return false;

            mod.Version = release["tag_name"];
            mod.DownloadUrl = urls[index];
            if (string.IsNullOrEmpty(mod.Author))
                mod.Author = assets[index]["uploader"]["login"];
            return true;
        }

        private static async Task<bool> TryResolveViaWebsiteAsync(ModInfo mod)
        {
            // github.com/{repo}/releases/latest redirects to .../releases/tag/{tag}
            var redirect = await Http.GetRedirectAsync($"https://github.com/{mod.GitPath}/releases/latest").ConfigureAwait(false);
            if (redirect == null) return false;

            var match = Regex.Match(redirect.AbsolutePath, @"/releases/tag/(?<tag>[^/]+)$");
            if (!match.Success) return false;

            string tag = WebUtility.UrlDecode(match.Groups["tag"].Value);
            if (mod.TagPrefix != null && !tag.StartsWith(mod.TagPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            // The asset list on the release page is lazy-loaded from this fragment.
            string html = await Http.GetStringAsync(
                $"https://github.com/{mod.GitPath}/releases/expanded_assets/{Uri.EscapeDataString(tag)}").ConfigureAwait(false);

            var names = new List<string>();
            var urls = new List<string>();
            foreach (Match m in Regex.Matches(html, @"href=""(?<href>/[^""]+/releases/download/[^""]+)"""))
            {
                string href = WebUtility.HtmlDecode(m.Groups["href"].Value);
                if (urls.Contains("https://github.com" + href)) continue;
                urls.Add("https://github.com" + href);
                names.Add(WebUtility.UrlDecode(href.Substring(href.LastIndexOf('/') + 1)));
            }

            int index = PickAsset(mod, names);
            if (index < 0) return false;

            mod.Version = tag;
            mod.DownloadUrl = urls[index];
            return true;
        }

        private static int PickAsset(ModInfo mod, List<string> names)
        {
            if (names.Count == 0) return -1;

            if (mod.AssetPattern != null)
            {
                var regex = new Regex(mod.AssetPattern, RegexOptions.IgnoreCase);
                return names.FindIndex(n => n != null && regex.IsMatch(n));
            }

            // Legacy behaviour: pick by index, but only if it is something we can install.
            if (mod.ReleaseId >= 0 && mod.ReleaseId < names.Count && IsInstallable(names[mod.ReleaseId]))
                return mod.ReleaseId;

            return names.FindIndex(IsInstallable);
        }

        private static bool IsInstallable(string fileName)
        {
            var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
            return ext == ".dll" || ext == ".zip";
        }

        /// <summary>Parses "v5.4.23.5" / "5.4.23" style tags. Returns null if it can't.</summary>
        public static Version ParseVersion(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            var m = Regex.Match(tag, @"\d+(\.\d+){1,3}");
            return m.Success && Version.TryParse(m.Value, out var v) ? v : null;
        }

        /// <summary>Compares versions ignoring missing trailing components (5.4.23 == 5.4.23.0).</summary>
        public static int CompareVersions(Version a, Version b)
        {
            return Normalize(a).CompareTo(Normalize(b));
        }

        private static Version Normalize(Version v) =>
            new Version(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
    }
}
