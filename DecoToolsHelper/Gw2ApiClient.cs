using System.Net.Http;
using Newtonsoft.Json;

namespace DecoToolsHelper
{
    /// <summary>
    /// Minimal wrapper around the Guild Wars 2 public API.
    /// 
    /// Design goals:
    /// - Centralize authenticated API requests
    /// - Keep API usage explicit and controlled
    /// - Avoid hiding or abstracting GW2 endpoints
    /// 
    /// This helper intentionally supports:
    /// - GET requests only
    /// - Explicit endpoint paths passed by the caller
    /// </summary>
    public class Gw2ApiClient
    {
        // Reused HttpClient instance (recommended best practice)
        private readonly HttpClient _http = new();

        /// <summary>
        /// Performs an authenticated GET request to the GW2 API
        /// and deserializes the JSON response into the requested type.
        /// </summary>
        /// <typeparam name="T">Expected deserialized response type</typeparam>
        /// <param name="endpoint">
        /// API endpoint path (example: /v2/account)
        /// </param>
        /// <param name="apiKey">
        /// User-provided GW2 API key
        /// </param>
        /// <returns>
        /// Deserialized response object
        /// </returns>
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
