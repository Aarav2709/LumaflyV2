using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Needlelight.Interfaces;
using Needlelight.Models;
using Needlelight.Util;

namespace Needlelight.Services
{
    public class ModDatabase : IModDatabase
    {
        public const string LINKS_BASE = "https://raw.githubusercontent.com/hk-modding/modlinks/main";

        private const string FALLBACK_MODLINKS_URI = "https://cdn.jsdelivr.net/gh/hk-modding/modlinks@latest/ModLinks.xml";
        private const string FALLBACK_APILINKS_URI = "https://cdn.jsdelivr.net/gh/hk-modding/modlinks@latest/ApiLinks.xml";

        private const string VanillaApiRepo = "https://raw.githubusercontent.com/TheMulhima/Needlelight/static-resources/AssemblyLinks.json";

        private sealed record GameContentSources(
            IReadOnlyList<Uri> Modlinks,
            IReadOnlyList<Uri> ModlinksFallbacks,
            IReadOnlyList<Uri> ApiLinks,
            IReadOnlyList<Uri> ApiLinksFallbacks,
            IReadOnlyList<Uri> AssemblyJsonUris,
            IReadOnlyList<string> JsonGameKeys);

        private static readonly GameContentSources HollowKnightSources = new(
            Modlinks: new[] { new Uri("https://raw.githubusercontent.com/hk-modding/modlinks/main/ModLinks.xml") },
            ModlinksFallbacks: new[] { new Uri(FALLBACK_MODLINKS_URI) },
            ApiLinks: new[] { new Uri("https://raw.githubusercontent.com/hk-modding/modlinks/main/ApiLinks.xml") },
            ApiLinksFallbacks: new[] { new Uri(FALLBACK_APILINKS_URI) },
            AssemblyJsonUris: new[] { new Uri(VanillaApiRepo) },
            JsonGameKeys: new[] { GameProfiles.HollowKnightKey, "hk", "hollowknight", "hollow_knight" }
        );

        private static readonly GameContentSources SilksongSources = new(
            Modlinks: new[]
            {
                new Uri("https://raw.githubusercontent.com/silksong-modding/modlinks/main/ModLinks.xml"),
                new Uri("https://raw.githubusercontent.com/hk-modding/silksong-modlinks/main/ModLinks.xml"),
                new Uri("https://raw.githubusercontent.com/hk-modding/modlinks/main/silksong/ModLinks.xml")
            },
            ModlinksFallbacks: new[]
            {
                new Uri("https://cdn.jsdelivr.net/gh/silksong-modding/modlinks@latest/ModLinks.xml")
            },
            ApiLinks: new[]
            {
                new Uri("https://raw.githubusercontent.com/silksong-modding/modlinks/main/ApiLinks.xml"),
                new Uri("https://raw.githubusercontent.com/hk-modding/silksong-modlinks/main/ApiLinks.xml"),
                new Uri("https://raw.githubusercontent.com/hk-modding/modlinks/main/silksong/ApiLinks.xml")
            },
            ApiLinksFallbacks: new[]
            {
                new Uri("https://cdn.jsdelivr.net/gh/silksong-modding/modlinks@latest/ApiLinks.xml")
            },
            AssemblyJsonUris: new[]
            {
                new Uri("https://raw.githubusercontent.com/silksong-modding/modlinks/main/AssemblyLinks.json"),
                new Uri("https://raw.githubusercontent.com/TheMulhima/Needlelight/static-resources/SilksongAssemblyLinks.json"),
                new Uri("https://raw.githubusercontent.com/TheMulhima/Needlelight/static-resources/AssemblyLinks.Silksong.json")
            },
            JsonGameKeys: new[] { GameProfiles.SilksongKey, "hkss", "silksong", "hollow_knight_silksong" }
        );

        public static string GetModlinksUri(ISettings settings)
        {
            var sources = GetContentSources(settings);
            var uri = sources.Modlinks.FirstOrDefault();
            if (uri is null)
                throw new InvalidOperationException("No modlinks endpoint configured for the selected game profile.");

            return uri.ToString();
        }

        private static string GetAPILinksUri(ISettings settings)
        {
            var sources = GetContentSources(settings);
            var uri = sources.ApiLinks.FirstOrDefault();
            if (uri is null)
                throw new InvalidOperationException("No API endpoint configured for the selected game profile.");

            return uri.ToString();
        }

        private static GameContentSources GetContentSources(ISettings? settings)
        {
            var key = settings?.Game?.Trim();
            if (!string.IsNullOrEmpty(key) && key.Equals(GameProfiles.SilksongKey, StringComparison.OrdinalIgnoreCase))
                return SilksongSources;

            return HollowKnightSources;
        }

        private static async Task<string?> TryFetchSequentially(HttpClient hc, ISettings? settings, IEnumerable<Uri> candidates)
        {
            foreach (var uri in candidates)
            {
                try
                {
                    var cts = new CancellationTokenSource(TIMEOUT);
                    return await hc.GetStringAsync2(settings, uri, cts.Token);
                }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
                {
                    Trace.TraceWarning($"Failed to fetch {uri}: {e.Message}");
                }
            }

            return null;
        }

        private static async Task<string?> TryFetchWithFallbacks(HttpClient hc, ISettings? settings, GameContentSources sources, bool isModlinks)
        {
            var primary = isModlinks ? sources.Modlinks : sources.ApiLinks;
            var fallback = isModlinks ? sources.ModlinksFallbacks : sources.ApiLinksFallbacks;

            var result = await TryFetchSequentially(hc, settings, primary);
            if (!string.IsNullOrWhiteSpace(result))
                return result;

            if (fallback is { Count: > 0 })
                return await TryFetchSequentially(hc, settings, fallback) ?? result;

            return result;
        }

        private static IEnumerable<string> BuildAssemblyJsonKeyCandidates(GameContentSources sources)
        {
            var osKey = OperatingSystem.IsMacOS()
                ? "Mac"
                : OperatingSystem.IsLinux()
                    ? "Linux"
                    : "Windows";

            var baseKeys = new[]
            {
                "Assembly-CSharp.dll.v",
                $"{osKey}-Assembly-CSharp.dll.v",
                $"{osKey}_Assembly-CSharp.dll.v",
                $"{osKey}/Assembly-CSharp.dll.v"
            };

            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            foreach (var key in baseKeys)
            {
                if (seen.Add(key))
                    yield return key;
            }

            foreach (var raw in sources.JsonGameKeys)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    raw,
                    raw.Replace(" ", string.Empty),
                    raw.Replace("-", "_"),
                    raw.Replace(" ", string.Empty).Replace("-", "_"),
                    raw.ToLowerInvariant(),
                    raw.Replace(" ", string.Empty).Replace("-", "_").ToLowerInvariant()
                };

                foreach (var variant in variants)
                {
                    foreach (var format in new[]
                    {
                        $"{variant}-{osKey}-Assembly-CSharp.dll.v",
                        $"{osKey}-{variant}-Assembly-CSharp.dll.v",
                        $"{variant}_{osKey}_Assembly-CSharp.dll.v",
                        $"{osKey}_{variant}_Assembly-CSharp.dll.v",
                        $"{variant}/{osKey}/Assembly-CSharp.dll.v"
                    })
                    {
                        if (seen.Add(format))
                            yield return format;
                    }
                }
            }
        }

        private static IEnumerable<JsonElement> EnumerateAssemblyNodes(JsonElement root, IReadOnlyList<string> jsonGameKeys)
        {
            foreach (var key in jsonGameKeys)
            {
                if (TryGetPropertyCaseInsensitive(root, key, out var perGame))
                    yield return perGame;
            }

            if (TryGetPropertyCaseInsensitive(root, "games", out var gamesNode) && gamesNode.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in jsonGameKeys)
                {
                    if (TryGetPropertyCaseInsensitive(gamesNode, key, out var nested))
                        yield return nested;
                }
            }

            yield return root;
        }

        private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static ApiLinks CreateEmptyApiLinks()
        {
            return new ApiLinks
            {
                Manifest = new ApiManifest
                {
                    Version = 0,
                    Files = new List<string>(),
                    Links = new Links
                    {
                        Windows = new Link { SHA256 = string.Empty, URL = string.Empty },
                        Mac = new Link { SHA256 = string.Empty, URL = string.Empty },
                        Linux = new Link { SHA256 = string.Empty, URL = string.Empty }
                    }
                }
            };
        }

        internal const int TIMEOUT = 30_000;

        public (string Url, int Version, string SHA256) Api { get; }

        public List<ModItem> Items => _items;

        private readonly List<ModItem> _items = new();
        private readonly List<string> _itemNames = new();

        private ModDatabase(IModSource mods,
            IGlobalSettingsFinder _settingsFinder,
            ModLinks ml,
            ApiLinks al,
            ISettings? settings = null)
        {
            foreach (var mod in ml.Manifests)
            {
                var item = new ModItem
                (
                    settings,
                    link: mod.Links.OSUrl,
                    version: mod.Version.Value,
                    name: mod.Name,
                    shasum: mod.Links.SHA256,
                    description: mod.Description,
                    repository: mod.Repository,
                    issues: mod.Issues,
                    dependencies: mod.Dependencies,

                    tags: mod.Tags,
                    integrations: mod.Integrations,
                    authors: mod.Authors,

                    state: mods.FromManifest(mod)

                );

                _items.Add(item);
                _itemNames.Add(mod.Name);
            }

            if (settings is not null)
            {
                foreach (var (externalModName, externalModState) in mods.NotInModlinksMods)
                {
                    if (externalModState.ModlinksMod)
                    {
                        var mod = _items.First(x => x.Name == externalModName);
                        mod.State = externalModState;
                    }
                    else
                    {
                        _items.Add(ModItem.Empty(
                            settings,
                            state: externalModState,
                            name: externalModName,
                            description: "This mod is not from official modlinks"));
                    }
                }
            }

            _items.Sort((a, b) => string.Compare(a.Name, b.Name));
            _items.ForEach(i => i.FindSettingsFile(_settingsFinder));

            Api = (al.Manifest.Links.OSUrl, al.Manifest.Version, al.Manifest.Links.SHA256);
        }

        public ModDatabase(IModSource mods, IGlobalSettingsFinder settingsFinder, (ModLinks ml, ApiLinks al) links, ISettings settings)
            : this(mods, settingsFinder, links.ml, links.al, settings) { }

        public ModDatabase(IModSource mods, IGlobalSettingsFinder settingsFinder, string modlinks, string apilinks)
            : this(mods, settingsFinder, FromString<ModLinks>(modlinks), FromString<ApiLinks>(apilinks)) { }

        public static async Task<(ModLinks, ApiLinks)> FetchContent(HttpClient hc, ISettings settings, bool fetchOfficial = true)
        {
            // although slower to fetch one by one, prevents silent errors and hence resulting in
            // empty screen with no error
            ModLinks ml = await FetchModLinks(hc, settings, fetchOfficial);
            ApiLinks al = await FetchApiLinks(hc, settings);

            return (ml, al);
        }

        public static T FromString<T>(string xml) where T : XmlDataContainer
        {
            var serializer = new XmlSerializer(typeof(T));

            using TextReader reader = new StringReader(xml);

            var obj = (T?) serializer.Deserialize(reader);

            if (obj is null)
                throw new InvalidDataException();

            obj.Raw = xml;

            return obj;
        }

        private static async Task<ApiLinks> FetchApiLinks(HttpClient hc, ISettings settings)
        {
            var apisettings = settings ?? Settings.Load() ?? new Settings();
            var sources = GetContentSources(apisettings);

            var xml = await TryFetchWithFallbacks(hc, apisettings, sources, isModlinks: false);
            if (!string.IsNullOrWhiteSpace(xml))
                return FromString<ApiLinks>(xml);

            Trace.TraceWarning($"Unable to fetch API links for {apisettings.Game}; using empty manifest.");
            return CreateEmptyApiLinks();
        }

        private static async Task<ModLinks> FetchModLinks(HttpClient hc, ISettings settings, bool fetchOfficial)
        {
            // Normalize settings to a guaranteed non-null instance for the rest of this method.
            var effectiveSettings = settings ?? Settings.Load() ?? new Settings();

            if (!fetchOfficial && effectiveSettings.UseCustomModlinks)
            {
                try
                {
                    // Copy to local to satisfy nullable flow analysis and avoid mid-flow mutations.
                    var customUri = effectiveSettings.CustomModlinksUri;
                    if (string.IsNullOrWhiteSpace(customUri))
                        throw new InvalidModlinksException();

                    var modlinksUri = new Uri(customUri);
                    if (modlinksUri.IsFile)
                    {
                        return FromString<ModLinks>(await File.ReadAllTextAsync(modlinksUri.LocalPath));
                    }

                    var cts = new CancellationTokenSource(TIMEOUT);

                    // Get raw versions of common URLs.
                    Regex githubRegex = new Regex(@"^(http(s?):\/\/)?(www\.)?github.com?");
                    Regex pasteBinRegex = new Regex(@"^(http(s?):\/\/)?(www\.)?pastebin.com?");

                    if (githubRegex.IsMatch(customUri))
                    {
                        customUri = customUri.Replace("github.com", "raw.githubusercontent.com").Replace("/blob/", "/");
                    }
                    if (pasteBinRegex.IsMatch(customUri))
                    {
                        customUri = customUri.Replace("pastebin.com", "pastebin.com/raw");
                    }

            return FromString<ModLinks>(await hc.GetStringAsync2(effectiveSettings, new Uri(customUri), cts.Token));
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Unable to load custom modlinks because {e}");
                    throw new InvalidModlinksException();
                }
            }

            if (!fetchOfficial && !effectiveSettings.UseCustomModlinks)
            {
                // fall through to official fetch so UI matches official catalog
                fetchOfficial = true;
            }

            if (fetchOfficial)
            {
                var sources = GetContentSources(effectiveSettings);
                var xml = await TryFetchWithFallbacks(hc, effectiveSettings, sources, isModlinks: true);
                if (!string.IsNullOrWhiteSpace(xml))
                    return FromString<ModLinks>(xml);

                Trace.TraceWarning($"Unable to fetch modlinks for {effectiveSettings.Game}; returning empty manifest list.");
            }

            return new ModLinks { Manifests = Array.Empty<Manifest>(), Raw = string.Empty };
        }

        public static async Task<string> FetchVanillaAssemblyLink(ISettings? settings)
        {
            var hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("User-Agent", "Needlelight");

            var sources = GetContentSources(settings);
            var candidateKeys = BuildAssemblyJsonKeyCandidates(sources).ToList();

            foreach (var endpoint in sources.AssemblyJsonUris)
            {
                try
                {
                    var cts = new CancellationTokenSource(TIMEOUT);
                    var payload = await hc.GetStringAsync2(settings, endpoint, cts.Token);

                    using var json = JsonDocument.Parse(payload);
                    foreach (var node in EnumerateAssemblyNodes(json.RootElement, sources.JsonGameKeys))
                    {
                        foreach (var key in candidateKeys)
                        {
                            if (TryGetPropertyCaseInsensitive(node, key, out var linkElem) &&
                                linkElem.ValueKind == JsonValueKind.String)
                            {
                                var link = linkElem.GetString();
                                if (!string.IsNullOrWhiteSpace(link))
                                    return link;
                            }
                        }
                    }
                }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
                {
                    Trace.TraceWarning($"Failed to fetch assembly link manifest from {endpoint}: {e.Message}");
                }
                catch (JsonException je)
                {
                    Trace.TraceWarning($"Invalid assembly link manifest at {endpoint}: {je.Message}");
                }
            }

            throw new Exception("Needlelight was unable to get vanilla assembly link from its resources. Please verify integrity of game files instead");
        }
    }

    public class InvalidModlinksException : Exception
    {
        public InvalidModlinksException() { }
    }
}

