using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DecoToolsHelper
{
    /// Lightweight local HTTP server used by the Deco Tools web UI.
    /// 
    /// Responsibilities:
    /// - Proxy selected Guild Wars 2 API endpoints
    /// - Read live MumbleLink data
    /// - Enforce safe, targeted API usage (no bulk guild scans)
    /// - Provide CORS-safe local access for browser-based tools
    /// 
    /// The server listens ONLY on localhost.
    public class LocalServer
    {
        private readonly HttpListener _listener = new();
        private readonly HelperConfig _config;
        private readonly Gw2ApiClient _gw2 = new();

        public LocalServer(HelperConfig config)
        {
            _config = config;
            _listener.Prefixes.Add("http://localhost:61337/");
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
                // KNOWN GOOD CORS BEHAVIOR (DO NOT MODIFY)
                ApplyCors(ctx);

                var path = ctx.Request.Url?.AbsolutePath ?? "/";
                var method = ctx.Request.HttpMethod;

                // ==================================================
                // CORS PREFLIGHT (IMPORTANT: MUST END CLEANLY)
                // ==================================================
                if (method == "OPTIONS")
                {
                    // CORS headers already applied via ApplyCors(ctx)
                    ctx.Response.StatusCode = 204; // No Content (preferred for preflight)
                    ctx.Response.ContentLength64 = 0;
                    ctx.Response.OutputStream.Close();
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
                        version = "1.2.0",
                        apiKeyPresent = !string.IsNullOrWhiteSpace(_config.ApiKey),
                        mumbleAvailable = MumbleService.TryGet(out _, out _, out _, out _)
                    });
                    return;
                }

                // ==================================================
                // DECORATION DATABASE (FULL, READ-ONLY)
                // ==================================================
                if (path.Equals("/decorations", StringComparison.OrdinalIgnoreCase)
                    && method == "GET")
                {
                    var db = DecoDBBuilder.TryLoad();

                    if (db == null)
                    {
                        ctx.Response.StatusCode = 503;
                        WriteJson(ctx, new { error = "Decoration database not ready" });
                        return;
                    }

                    WriteJson(ctx, db);
                    return;
                }

                // ==================================================
                // DECORATION LOOKUP (LIGHTWEIGHT)
                // GET /decorations/lookup?name=Exact Name
                // ==================================================
                if (path.Equals("/decorations/lookup", StringComparison.OrdinalIgnoreCase)
                    && method == "GET")
                {
                    var db = DecoDBBuilder.TryLoad();

                    if (db == null)
                    {
                        ctx.Response.StatusCode = 503;
                        WriteJson(ctx, new { error = "Decoration database not ready" });
                        return;
                    }

                    var name = ctx.Request.QueryString["name"];
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        ctx.Response.StatusCode = 400;
                        WriteJson(ctx, new { error = "Missing name parameter" });
                        return;
                    }

                    var match = db.Decorations.FirstOrDefault(d =>
                        string.Equals(d.Name, name.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                    if (match == null)
                    {
                        ctx.Response.StatusCode = 404;
                        WriteJson(ctx, new { error = "Decoration not found" });
                        return;
                    }

                    WriteJson(ctx, new
                    {
                        name = match.Name,
                        homesteadId = match.HomesteadId,
                        guildUpgradeId = match.GuildUpgradeId
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
                if (path.Equals("/guilds", StringComparison.OrdinalIgnoreCase)
                    && method == "GET")
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
            var json = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(json);

            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentEncoding = Encoding.UTF8;
            ctx.Response.StatusCode = ctx.Response.StatusCode == 0 ? 200 : ctx.Response.StatusCode;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }

        private static void ApplyCors(HttpListenerContext ctx)
        {
            var origin = ctx.Request.Headers["Origin"];

            if (origin == null ||
                origin == "null" ||
                origin.StartsWith("http://localhost") ||
                origin == "https://gw2decotools.com" ||
                (origin.StartsWith("https://") && origin.Contains(".github.io")))
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
