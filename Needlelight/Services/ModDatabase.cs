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

        public static string GetModlinksUri(ISettings settings) => LINKS_BASE + "/ModLinks.xml";

        private static string GetAPILinksUri(ISettings settings) => LINKS_BASE + "/ApiLinks.xml";

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
            // If Silksong is selected and no custom modlinks are configured, return a minimal API stub
            if (string.Equals(settings?.Game, GameProfiles.SilksongKey, StringComparison.OrdinalIgnoreCase)
                && !(settings?.UseCustomModlinks ?? false))
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

            var apisettings = settings ?? Settings.Load() ?? new Settings();
            return FromString<ApiLinks>(await FetchWithFallback(hc, apisettings, new Uri(GetAPILinksUri(apisettings)), new Uri(FALLBACK_APILINKS_URI)));
        }

        private static async Task<ModLinks> FetchModLinks(HttpClient hc, ISettings settings, bool fetchOfficial)
        {
            // Normalize settings to a guaranteed non-null instance for the rest of this method.
            var effectiveSettings = settings ?? Settings.Load() ?? new Settings();

            // When Silksong is selected and user hasn't provided a custom list, don't fetch HK modlinks;
            // instead, return an empty mod list so the UI reflects no available mods for Silksong yet.
            if (fetchOfficial)
            {
                var selectedGame = effectiveSettings.Game;
                var useCustom = effectiveSettings.UseCustomModlinks;
                if (string.Equals(selectedGame, GameProfiles.SilksongKey, StringComparison.OrdinalIgnoreCase) && !useCustom)
                {
                    return new ModLinks { Manifests = Array.Empty<Manifest>(), Raw = string.Empty };
                }
            }

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

        var mlsettings = effectiveSettings;
        return FromString<ModLinks>(await FetchWithFallback(hc, mlsettings, new Uri(GetModlinksUri(mlsettings)), new Uri(FALLBACK_MODLINKS_URI)));

        }

        private static async Task<string> FetchWithFallback(HttpClient hc, ISettings? settings, Uri uri, Uri fallback)
        {
            try
            {
                var cts = new CancellationTokenSource(TIMEOUT);
                return await hc.GetStringAsync2(settings, uri, cts.Token);
            }
            catch (Exception e) when (e is TaskCanceledException or HttpRequestException)
            {
                var cts = new CancellationTokenSource(TIMEOUT);
                return await hc.GetStringAsync2(settings, fallback, cts.Token);
            }
        }

        public static async Task<string> FetchVanillaAssemblyLink(ISettings? settings)
        {
            var cts = new CancellationTokenSource(TIMEOUT);
            var hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("User-Agent", "Needlelight");
            var json = JsonDocument.Parse(await hc.GetStringAsync2(settings, VanillaApiRepo, cts.Token));

            var jsonKey = "Assembly-CSharp.dll.v";
            // windows assembly is just called that because initially this was overlooked and only windows assembly was downloaded
            if (OperatingSystem.IsMacOS()) jsonKey = "Mac-Assembly-CSharp.dll.v";
            if (OperatingSystem.IsLinux()) jsonKey = "Linux-Assembly-CSharp.dll.v";

            json.RootElement.TryGetProperty(jsonKey, out var linkElem);

            var link = linkElem.GetString();
            if (link != null)
                return link;
            throw new Exception("Needlelight was unable to get vanilla assembly link from its resources. Please verify integrity of game files instead");
        }
    }

    public class InvalidModlinksException : Exception
    {
        public InvalidModlinksException() { }
    }
}

