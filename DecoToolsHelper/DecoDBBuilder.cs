using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DecoToolsHelper
{
    /// Builds and maintains a unified local decoration database by merging:
    /// - Guild hall decorations (guild upgrades of type "Decoration")
    /// - Homestead decorations
    ///
    /// Uses public GW2 API endpoints only.
    /// Automatically rebuilds when new IDs are detected.
    public static class DecoDBBuilder
    {
        private const int DbVersion = 1;
        private const int BatchSize = 50;

        private const string GuildUpgradesBaseUrl =
            "https://api.guildwars2.com/v2/guild/upgrades";

        private const string HomesteadDecorationsBaseUrl =
            "https://api.guildwars2.com/v2/homestead/decorations";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        // ==================================================
        // PUBLIC ENTRY POINT
        // ==================================================

        public static async Task<BuildResult> EnsureUpToDateAsync(
            Action<string>? progress = null,
            CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                progress?.Invoke("Loading existing database…");
                var existing = TryLoad();

                progress?.Invoke("Fetching ID lists…");
                var guildIds = await FetchIdListAsync(GuildUpgradesBaseUrl, ct);
                var homesteadIds = await FetchIdListAsync(HomesteadDecorationsBaseUrl, ct);

                if (existing != null &&
                    !NeedsRebuild(existing, guildIds, homesteadIds))
                {
                    return new BuildResult
                    {
                        Success = true,
                        Skipped = true,
                        TotalEntries = existing.Decorations.Count,
                        DbPath = GetDbPath()
                    };
                }

                progress?.Invoke("Fetching decoration metadata…");
                var guildDecorations = await FetchGuildDecorationsAsync(guildIds, ct);
                var homesteadDecorations = await FetchHomesteadDecorationsAsync(homesteadIds, ct);

                progress?.Invoke("Merging database…");
                var db = BuildDatabase(
                    guildDecorations,
                    homesteadDecorations,
                    guildIds,
                    homesteadIds);

                progress?.Invoke("Saving database…");
                SaveAtomic(db);

                return new BuildResult
                {
                    Success = true,
                    Skipped = false,
                    TotalEntries = db.Decorations.Count,
                    DbPath = GetDbPath()
                };
            }
            catch (Exception ex)
            {
                return new BuildResult
                {
                    Success = false,
                    Error = ex.Message,
                    DbPath = GetDbPath()
                };
            }
            finally
            {
                _lock.Release();
            }
        }

        // ==================================================
        // REBUILD DECISION
        // ==================================================

        private static bool NeedsRebuild(
            DecorationDatabase db,
            HashSet<int> liveGuildIds,
            HashSet<int> liveHomesteadIds)
        {
            if (db.Version != DbVersion)
                return true;

            if (!liveGuildIds.SetEquals(db.SourceSnapshot.GuildUpgradeIds))
                return true;

            if (!liveHomesteadIds.SetEquals(db.SourceSnapshot.HomesteadDecorationIds))
                return true;

            return false;
        }

        // ==================================================
        // ID LIST FETCH
        // ==================================================

        private static async Task<HashSet<int>> FetchIdListAsync(
            string baseUrl,
            CancellationToken ct)
        {
            var json = await _http.GetStringAsync(baseUrl).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            var ids = JsonConvert.DeserializeObject<List<int>>(json)
                      ?? new List<int>();

            return ids.Where(id => id > 0).ToHashSet();
        }

        // ==================================================
        // METADATA FETCH (BATCHED)
        // ==================================================

        private static async Task<List<GuildUpgradeDto>> FetchGuildDecorationsAsync(
            HashSet<int> ids,
            CancellationToken ct)
        {
            var result = new List<GuildUpgradeDto>();

            foreach (var batch in Batch(ids))
            {
                ct.ThrowIfCancellationRequested();

                var url = $"{GuildUpgradesBaseUrl}?ids={string.Join(",", batch)}";
                var json = await _http.GetStringAsync(url).ConfigureAwait(false);

                var items = JsonConvert.DeserializeObject<List<GuildUpgradeDto>>(json)
                            ?? new List<GuildUpgradeDto>();

                result.AddRange(items.Where(i =>
                    i.Id > 0 &&
                    string.Equals(i.Type, "Decoration", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(i.Name)));
            }

            return result;
        }

        private static async Task<List<HomesteadDecoDto>> FetchHomesteadDecorationsAsync(
            HashSet<int> ids,
            CancellationToken ct)
        {
            var result = new List<HomesteadDecoDto>();

            foreach (var batch in Batch(ids))
            {
                ct.ThrowIfCancellationRequested();

                var url = $"{HomesteadDecorationsBaseUrl}?ids={string.Join(",", batch)}";
                var json = await _http.GetStringAsync(url).ConfigureAwait(false);

                var items = JsonConvert.DeserializeObject<List<HomesteadDecoDto>>(json)
                            ?? new List<HomesteadDecoDto>();

                result.AddRange(items.Where(i =>
                    i.Id > 0 &&
                    !string.IsNullOrWhiteSpace(i.Name)));
            }

            return result;
        }

        // ==================================================
        // DATABASE BUILD
        // ==================================================

        private static DecorationDatabase BuildDatabase(
            List<GuildUpgradeDto> guildDecorations,
            List<HomesteadDecoDto> homesteadDecorations,
            HashSet<int> guildIds,
            HashSet<int> homesteadIds)
        {
            var byName = new Dictionary<string, DecorationEntry>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var g in guildDecorations)
            {
                var name = g.Name.Trim();
                if (!byName.TryGetValue(name, out var entry))
                {
                    entry = new DecorationEntry { Name = name };
                    byName[name] = entry;
                }
                entry.GuildUpgradeId = g.Id;
            }

            foreach (var h in homesteadDecorations)
            {
                var name = h.Name.Trim();
                if (!byName.TryGetValue(name, out var entry))
                {
                    entry = new DecorationEntry { Name = name };
                    byName[name] = entry;
                }
                entry.HomesteadId = h.Id;
            }

            return new DecorationDatabase
            {
                Version = DbVersion,
                GeneratedAtUtc = DateTime.UtcNow,
                SourceSnapshot = new SourceSnapshot
                {
                    GuildUpgradeIds = guildIds,
                    HomesteadDecorationIds = homesteadIds
                },
                Decorations = byName.Values
                    .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        // ==================================================
        // PERSISTENCE
        // ==================================================

        private static void SaveAtomic(DecorationDatabase db)
        {
            var folder = GetDataFolder();
            Directory.CreateDirectory(folder);

            var path = GetDbPath();
            var tmp = path + ".tmp";

            File.WriteAllText(
                tmp,
                JsonConvert.SerializeObject(db, Formatting.Indented),
                Encoding.UTF8);

            if (File.Exists(path))
            {
                var bak = path + ".bak";
                try
                {
                    File.Replace(tmp, path, bak, true);
                    TryDelete(bak);
                    return;
                }
                catch
                {
                    File.Copy(tmp, path, true);
                }
            }
            else
            {
                File.Move(tmp, path);
            }

            TryDelete(tmp);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }

        public static DecorationDatabase? TryLoad()
        {
            try
            {
                var path = GetDbPath();
                if (!File.Exists(path))
                    return null;

                return JsonConvert.DeserializeObject<DecorationDatabase>(
                    File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        // ==================================================
        // HELPERS
        // ==================================================

        private static IEnumerable<List<int>> Batch(IEnumerable<int> ids)
        {
            var batch = new List<int>(BatchSize);
            foreach (var id in ids)
            {
                batch.Add(id);
                if (batch.Count >= BatchSize)
                {
                    yield return batch;
                    batch = new List<int>(BatchSize);
                }
            }

            if (batch.Count > 0)
                yield return batch;
        }

        private static string GetDataFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DecoToolsHelper");
        }

        public static string GetDbPath()
        {
            return Path.Combine(GetDataFolder(), "decorations.db.json");
        }

        // ==================================================
        // MODELS
        // ==================================================

        public sealed class BuildResult
        {
            public bool Success { get; set; }
            public bool Skipped { get; set; }
            public int TotalEntries { get; set; }
            public string? Error { get; set; }
            public string DbPath { get; set; } = "";
        }

        public sealed class DecorationDatabase
        {
            public int Version { get; set; }
            public DateTime GeneratedAtUtc { get; set; }
            public SourceSnapshot SourceSnapshot { get; set; } = new();
            public List<DecorationEntry> Decorations { get; set; } = new();
        }

        public sealed class SourceSnapshot
        {
            public HashSet<int> GuildUpgradeIds { get; set; } = new();
            public HashSet<int> HomesteadDecorationIds { get; set; } = new();
        }

        public sealed class DecorationEntry
        {
            public string Name { get; set; } = "";
            public int? HomesteadId { get; set; }
            public int? GuildUpgradeId { get; set; }
        }

        private sealed class GuildUpgradeDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
        }

        private sealed class HomesteadDecoDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
    }
}
