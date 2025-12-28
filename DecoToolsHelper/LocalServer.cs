using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace DecoToolsHelper
{
    /// <summary>
    /// Lightweight local HTTP server used by the Deco Tools web UI.
    /// 
    /// Responsibilities:
    /// - Proxy selected Guild Wars 2 API endpoints
    /// - Read live MumbleLink data
    /// - Enforce safe, targeted API usage (no bulk guild scans)
    /// - Provide CORS-safe local access for browser-based tools
    /// 
    /// The server listens ONLY on localhost (127.0.0.1).
    /// </summary>
    public class LocalServer
    {
        private readonly HttpListener _listener = new();
        private readonly HelperConfig _config;
        private readonly Gw2ApiClient _gw2 = new();

        public LocalServer(HelperConfig config)
        {
            _config = config;
            _listener.Prefixes.Add("http://127.0.0.1:61337/");
        }

        public void Start()
        {
            _listener.Start();
            _ = Task.Run(ListenLoop);
        }

        private async Task ListenLoop()
        {
            while (_listener.IsListening)
            {
                var ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleRequest(ctx));
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                var path = ctx.Request.Url?.AbsolutePath ?? "/";
                var method = ctx.Request.HttpMethod;

                // ==================================================
                // CORS PREFLIGHT
                // ==================================================
                if (method == "OPTIONS")
                {
                    ApplyCors(ctx);
                    ctx.Response.StatusCode = 200;
                    ctx.Response.Close();
                    return;
                }

                // ==================================================
                // STATUS
                // ==================================================
                if (path.Equals("/status", StringComparison.OrdinalIgnoreCase))
                {
                    WriteJson(ctx, new
                    {
                        running = true,
                        version = "1.1.0",
                        apiKeyPresent = !string.IsNullOrWhiteSpace(_config.ApiKey),
                        mumbleAvailable = MumbleService.TryGet(out _, out _, out _, out _)
                    });
                    return;
                }

                // ==================================================
                // SET API KEY
                // ==================================================
                if (path.Equals("/config/apikey", StringComparison.OrdinalIgnoreCase)
                    && method == "POST")
                {
                    using var reader = new StreamReader(ctx.Request.InputStream);
                    var body = reader.ReadToEnd();
                    var data = JsonConvert.DeserializeObject<dynamic>(body);
                    var key = (string?)data?.apiKey;

                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        _config.ApiKey = key.Trim();
                        ConfigManager.Save(_config);
                        WriteJson(ctx, new { success = true });
                        return;
                    }

                    ctx.Response.StatusCode = 400;
                    WriteJson(ctx, new { success = false, error = "Invalid API key" });
                    return;
                }

                // ==================================================
                // HOMESTEAD DECORATION COUNTS
                // ==================================================
                if (path.Equals("/decos/homestead", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(_config.ApiKey))
                    {
                        ctx.Response.StatusCode = 400;
                        WriteJson(ctx, new { error = "API key not configured" });
                        return;
                    }

                    var list = _gw2
                        .GetAsync<List<AccountHomesteadDeco>>(
                            "/v2/account/homestead/decorations",
                            _config.ApiKey)
                        .GetAwaiter()
                        .GetResult();

                    var result = list.ToDictionary(
                        d => d.Id.ToString(),
                        d => d.Count
                    );

                    WriteJson(ctx, result);
                    return;
                }

                // ==================================================
                // GUILD LIST (ID + NAME + TAG)
                // ==================================================
                if (path.Equals("/guilds", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(_config.ApiKey))
                    {
                        ctx.Response.StatusCode = 400;
                        WriteJson(ctx, new { error = "API key not configured" });
                        return;
                    }

                    var account = _gw2
                        .GetAsync<AccountInfo>("/v2/account", _config.ApiKey)
                        .GetAwaiter()
                        .GetResult();

                    var result = new List<GuildSummary>();

                    foreach (var guildId in account.Guilds)
                    {
                        var guild = _gw2
                            .GetAsync<GuildSummary>(
                                $"/v2/guild/{guildId}",
                                _config.ApiKey)
                            .GetAwaiter()
                            .GetResult();

                        result.Add(new GuildSummary
                        {
                            Id = guild.Id,
                            Name = guild.Name,
                            Tag = guild.Tag
                        });
                    }

                    WriteJson(ctx, result);
                    return;
                }

                // ==================================================
                // GUILD DECORATION COUNTS (AUTHORITATIVE STORAGE)
                // ==================================================
                if (path.StartsWith("/decos/guild/", StringComparison.OrdinalIgnoreCase)
                    && method == "POST")
                {
                    if (string.IsNullOrWhiteSpace(_config.ApiKey))
                    {
                        ctx.Response.StatusCode = 400;
                        WriteJson(ctx, new { error = "API key not configured" });
                        return;
                    }

                    var guildId = path.Substring("/decos/guild/".Length);

                    using var reader = new StreamReader(ctx.Request.InputStream);
                    var body = reader.ReadToEnd();
                    var payload = JsonConvert.DeserializeObject<GuildIdRequest>(body);

                    if (payload == null || payload.Ids.Count == 0)
                    {
                        ctx.Response.StatusCode = 400;
                        WriteJson(ctx, new { error = "No IDs provided" });
                        return;
                    }

                    var idList = string.Join(",", payload.Ids);

                    // 🔑 AUTHORITATIVE GUILD STORAGE ENDPOINT
                    var storage = _gw2
                        .GetAsync<List<GuildStorageItem>>(
                            $"/v2/guild/{guildId}/storage?ids={idList}",
                            _config.ApiKey)
                        .GetAwaiter()
                        .GetResult();

                    var result = payload.Ids.ToDictionary(
                        id => id.ToString(),
                        _ => 0
                    );

                    foreach (var item in storage)
                    {
                        result[item.Id.ToString()] = item.Count;
                    }

                    WriteJson(ctx, result);
                    return;
                }

                // ==================================================
                // MUMBLE POSITION
                // ==================================================
                if (path.Equals("/mumble", StringComparison.OrdinalIgnoreCase))
                {
                    if (MumbleService.TryGet(out int mapId, out float x, out float y, out float z))
                    {
                        WriteJson(ctx, new
                        {
                            available = true,
                            mapId,
                            position = new { x, y, z }
                        });
                    }
                    else
                    {
                        WriteJson(ctx, new { available = false });
                    }
                    return;
                }

                ctx.Response.StatusCode = 404;
                WriteJson(ctx, new { error = "Not Found" });
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 500;
                WriteJson(ctx, new { error = "Server Error", detail = ex.Message });
            }
        }

        private static void WriteJson(HttpListenerContext ctx, object obj)
        {
            ApplyCors(ctx);

            var json = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(json);

            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentEncoding = Encoding.UTF8;
            ctx.Response.StatusCode = 200;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }

        private static void ApplyCors(HttpListenerContext ctx)
        {
            var origin = ctx.Request.Headers["Origin"];

            if (origin == null || origin == "null" || origin.StartsWith("http://localhost"))
            {
                ctx.Response.Headers["Access-Control-Allow-Origin"] = origin ?? "null";
                ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
                ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            }
        }

        public void Stop()
        {
            try
            {
                if (_listener.IsListening)
                    _listener.Stop();

                _listener.Close();
            }
            catch
            {
                // Ignore shutdown errors
            }
        }
    }
}
