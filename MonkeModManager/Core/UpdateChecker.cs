using System;
using System.Threading.Tasks;
using MonkeModManager.Internals.SimpleJSON;

namespace MonkeModManager.Core
{
    internal static class UpdateChecker
    {
        /// <summary>Returns the newer release tag of this app, or null if up to date / unknown.</summary>
        public static async Task<string> GetNewerVersionAsync()
        {
            try
            {
                var release = JSON.Parse(await Http.GetGitHubApiAsync($"repos/{AppInfo.RepoSlug}/releases/latest").ConfigureAwait(false));
                string tag = release["tag_name"];
                var latest = ReleaseResolver.ParseVersion(tag);
                if (latest != null && ReleaseResolver.CompareVersions(latest, AppInfo.Version) > 0)
                    return tag;
            }
            catch
            {
                // no releases yet, offline, or rate limited - not important enough to bother the user
            }
            return null;
        }
    }
}
