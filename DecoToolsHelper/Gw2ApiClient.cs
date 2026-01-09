using System.Net.Http;
using Newtonsoft.Json;

namespace DecoToolsHelper
{
    /// Minimal wrapper for GW2 API.
    /// 
    /// Design goals:
    /// - Centralize authenticated API requests
    /// - Keep API usage explicit and controlled
    /// - Avoid hiding or abstracting GW2 endpoints
    /// 
    /// This helper intentionally supports:
    /// - GET requests only
    /// - Explicit endpoint paths passed by the caller
    public class Gw2ApiClient
    {
        // Reused HttpClient instance
        private readonly HttpClient _http = new();

        /// Performs an authenticated GET request to the GW2 API
        /// and deserializes the JSON response into the requested type.
        public async Task<T> GetAsync<T>(string endpoint, string apiKey)
        {
            var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.guildwars2.com{endpoint}"
            );

            // GW2 API uses Bearer token authorization
            req.Headers.Add("Authorization", $"Bearer {apiKey}");

            var res = await _http.SendAsync(req);

            // Throw if the API returns a non-success status code
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();

            // Caller guarantees correct response type for the endpoint
            return JsonConvert.DeserializeObject<T>(json)!;
        }
    }
}
