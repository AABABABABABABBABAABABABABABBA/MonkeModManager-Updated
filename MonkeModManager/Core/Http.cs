using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MonkeModManager.Core
{
    /// <summary>Thrown when GitHub refuses an API request because of rate limiting.</summary>
    internal class RateLimitedException : Exception
    {
        public RateLimitedException(string message) : base(message) { }
    }

    internal static class Http
    {
        private static readonly HttpClient Client;
        private static readonly HttpClient NoRedirectClient;

        static Http()
        {
            // .NET Framework defaults to old TLS versions on some machines; GitHub requires 1.2+.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            Client = Create(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            NoRedirectClient = Create(new HttpClientHandler { AllowAutoRedirect = false });
        }

        private static HttpClient Create(HttpClientHandler handler)
        {
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MonkeModManager", AppInfo.VersionString));
            return client;
        }

        /// <summary>GET a GitHub REST API endpoint, e.g. "repos/BepInEx/BepInEx/releases".</summary>
        public static async Task<string> GetGitHubApiAsync(string path)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/" + path))
            {
                request.Headers.Accept.ParseAdd("application/vnd.github+json");
                request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

                // Optional: set GITHUB_TOKEN to raise the anonymous limit of 60 requests/hour.
                var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());

                using (var response = await Client.SendAsync(request).ConfigureAwait(false))
                {
                    if ((response.StatusCode == HttpStatusCode.Forbidden || (int)response.StatusCode == 429)
                        && IsRateLimited(response))
                    {
                        throw new RateLimitedException("GitHub API rate limit reached.");
                    }
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }

        private static bool IsRateLimited(HttpResponseMessage response)
        {
            if ((int)response.StatusCode == 429) return true;
            return response.Headers.TryGetValues("X-RateLimit-Remaining", out var values)
                   && string.Join("", values).Trim() == "0";
        }

        public static async Task<string> GetStringAsync(string url)
        {
            using (var response = await Client.GetAsync(url).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        public static async Task<byte[]> GetBytesAsync(string url)
        {
            using (var response = await Client.GetAsync(url).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Returns the Location header of a redirect, or null if the URL does not redirect.</summary>
        public static async Task<Uri> GetRedirectAsync(string url)
        {
            using (var response = await NoRedirectClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                var location = response.Headers.Location;
                if (location == null) return null;
                return location.IsAbsoluteUri ? location : new Uri(new Uri(url), location);
            }
        }
    }
}
