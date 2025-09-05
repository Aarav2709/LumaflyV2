using Microsoft.Win32;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lumafly.Enums;
using Lumafly.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Lumafly
{
  [Serializable]
  public class Settings : ISettings
  {
    public string ManagedFolder { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AutoRemoveUnusedDepsOptions AutoRemoveUnusedDeps { get; set; } = AutoRemoveUnusedDepsOptions.Never;
    public bool WarnBeforeRemovingDependents { get; set; } = true;
    public bool UseCustomModlinks { get; set; }
    public string CustomModlinksUri { get; set; } = string.Empty;
    public bool UseGithubMirror { get; set; }
    public string GithubMirrorFormat { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SupportedLanguages? PreferredLanguage { get; set; }
    public bool LowStorageMode { get; set; } = false;
    public string ExtraSpaceTaken
    {
      get
      {
        long size = 0;
        if (Directory.Exists(CacheFolder))
        {
          size += FileUtil.GetAllFilesInDirectory(CacheFolder).Sum(x => x.Length);
        }

        var managed = new DirectoryInfo(ManagedFolder);
        foreach (var dir in managed.EnumerateDirectories())
        {
          if (dir.GetFiles().Any(x => x.Name == PackManager.packInfoFileName))
          {
            size += FileUtil.GetAllFilesInDirectory(dir.FullName).Sum(x => x.Length);
          }
        }

        return $"{size / 1024 / 1024} MB";
      }
    }

    public bool RequiresWorkaroundClient { get; set; }

    /// <summary>
    /// Selected game key. Defaults to Hollow Knight.
    /// </summary>
    public string Game { get; set; } = GameProfiles.HollowKnightKey;

    public GameProfile CurrentProfile => GameProfiles.GetByKey(Game);

    // @formatter:off
    private static ImmutableList<string> BuildStaticPaths(GameProfile profile)
    {
      // Common install roots on Windows
      var candidates = new List<string>
            {
                $"Program Files/Steam/steamapps/common/{profile.Name}",
                $"XboxGames/{profile.Name}/Content",
                $"Program Files (x86)/Steam/steamapps/common/{profile.Name}",
                $"Program Files/GOG Galaxy/Games/{profile.Name}",
                $"Program Files (x86)/GOG Galaxy/Games/{profile.Name}",
                $"Steam/steamapps/common/{profile.Name}",
                $"GOG Galaxy/Games/{profile.Name}"
            };

      return candidates
          .SelectMany(path => DriveInfo.GetDrives().Select(d => Path.Combine(d.Name, path)))
          .ToImmutableList();
    }

    private static ImmutableList<string> BuildUserSuffixPaths(GameProfile profile)
    {
      // Linux defaults and symlinks + macOS Steam default app bundle path
      var name = profile.Name;
      // Attempt a lowercase underscore variant for macOS bundle name if needed
      var macBundleGuess = name.Replace(' ', '_').ToLowerInvariant() + ".app";

      return new List<string>
            {
                // Linux
                $".local/share/Steam/steamapps/common/{name}",
                $".steam/steam/steamapps/common/{name}",
                // Flatpak
                ".var/app/ocm.valvesoftware.Steam/data/Steam/steamapps/common",
                // Symlinks to the Steam root on linux
                ".steam/steam",
                ".steam/root",
                // macOS (Steam default app bundle under common)
                $"Library/Application Support/Steam/steamapps/common/{name}/{macBundleGuess}"
            }.ToImmutableList();
    }
    // @formatter:on

    public static string ConfigFolderPath => Path.Combine
        (
            Environment.GetFolderPath
            (
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create
            ),
            "HKModInstaller"
        );

    private static string ConfigPath => Path.Combine(ConfigFolderPath, "HKInstallerSettings.json");
    public string CacheFolder => Path.Combine(ConfigFolderPath, "HKInstallerCache");

    internal Settings(string path)
    {
      ManagedFolder = path;

      var culture = Thread.CurrentThread.CurrentUICulture;
      if (Enum.TryParse(culture.TwoLetterISOLanguageName, out SupportedLanguages preferredLanguage))
        PreferredLanguage = preferredLanguage;
    }

    // Used by serializer.
    public Settings()
    {
      ManagedFolder = null!;
      AutoRemoveUnusedDeps = AutoRemoveUnusedDepsOptions.Never;
      PreferredLanguage = null;
      LowStorageMode = false;
    }

    public static string GetOrCreateDirPath()
    {
      string dirPath = Path.GetDirectoryName(ConfigPath) ?? throw new InvalidOperationException();

      // No-op if path already exists.
      Directory.CreateDirectory(dirPath);

      return dirPath;
    }

    internal static async Task<ValidPath?> TryAutoDetect()
    {
      ValidPath? path = null;
      // Resolve profile for path detection: if a config file already exists, prefer its Game value
      var profile = GameProfiles.HollowKnight; // default fallback
      try
      {
        var loaded = Load();
        if (loaded is { })
          profile = loaded.CurrentProfile;
      }
      catch { /* ignore and fallback to HK */ }

      // Try static paths first
      var staticPaths = BuildStaticPaths(profile);
      path = staticPaths.Select(p => PathUtil.ValidateWithSuffixForProfile(p, profile)).FirstOrDefault(x => x is not null);

      // If that's valid, use it.
      if (path is not null)
        return path;

      // Otherwise, we go through the user profile suffixes.
      string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

      var userSuffixes = BuildUserSuffixPaths(profile);
      path = userSuffixes
             .Select(suffix => Path.Combine(home, suffix))
             .Select(p => PathUtil.ValidateWithSuffixForProfile(p, profile))
             .FirstOrDefault(x => x is not null);

      if (path is not null)
        return path;

      if (TryDetectFromRegistry(out path, profile))
        return path;

      // since it cant detect from registry assume its because it can't access the registry
      await DisplayErrors.AskForAdminReload("Path was not automatically found from registry.");

      return path; // if couldn't find path it would be null
    }

    private static bool TryDetectFromRegistry([MaybeNullWhen(false)] out ValidPath path, GameProfile profile)
    {
      path = null;

      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return false;

      return TryDetectSteamRegistry(out path, profile) || TryDetectGogRegistry(out path, profile);
    }

    [SupportedOSPlatform(nameof(OSPlatform.Windows))]
    private static bool TryDetectGogRegistry([MaybeNullWhen(false)] out ValidPath path, GameProfile profile)
    {
      path = null;

      // Try known profile GOG ids; fallback to HK default.
      var gogIds = (profile.GogIds != null && profile.GogIds.Count > 0)
        ? profile.GogIds
        : new[] { "1308320804" }; // Hollow Knight GOG id

      foreach (var id in gogIds)
      {
        var val = Registry.GetValue(@$"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\{id}", "workingDir", null) as string;
        if (string.IsNullOrEmpty(val))
          continue;

        if (PathUtil.ValidateWithSuffixForProfile(val, profile) is ValidPath v)
        {
          path = v;
          return true;
        }
      }
      return false;
    }

    [SupportedOSPlatform(nameof(OSPlatform.Windows))]
    private static bool TryDetectSteamRegistry([MaybeNullWhen(false)] out ValidPath path, GameProfile profile)
    {
      path = null;

      if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) is not string steam_install)
        return false;

      IEnumerable<string> lines;

      try
      {
        lines = File.ReadLines(Path.Combine(steam_install, "steamapps", "libraryfolders.vdf"));
      }
      catch (Exception e) when (
          e is FileNotFoundException
              or UnauthorizedAccessException
              or IOException
              or DirectoryNotFoundException
      )
      {
        return false;
      }

      string? Parse(string line)
      {
        line = line.TrimStart();

        if (!line.StartsWith("\"path\""))
          return null;

        string[] pair = line.Split("\t", 2, StringSplitOptions.RemoveEmptyEntries);

        return pair.Length != 2
            ? null
            : pair[1].Trim('"');
      }

      IEnumerable<string> library_paths = lines.Select(Parse).OfType<string>();

      // If we know SteamAppId, parse appmanifest to locate the install dir
      var appId = profile.SteamAppId; // TODO: Confirm Silksong AppId
      if (!string.IsNullOrWhiteSpace(appId))
      {
        foreach (var lib in library_paths)
        {
          var steamapps = Path.Combine(lib, "steamapps");
          var manifest = Path.Combine(steamapps, $"appmanifest_{appId}.acf");
          try
          {
            if (!File.Exists(manifest)) continue;
            string dirName = TryParseInstallDirFromAcf(manifest);
            if (!string.IsNullOrWhiteSpace(dirName))
            {
              var candidate = Path.Combine(steamapps, "common", dirName);
              var v = PathUtil.ValidateWithSuffixForProfile(candidate, profile);
              if (v is not null) { path = v; return true; }
            }
          }
          catch { /* ignore and continue */ }
        }
      }

      // Fallback: search by folder name
      var folderName = profile.Name; // Default folder name
      path = library_paths
        .Select(library_path => Path.Combine(library_path, "steamapps", "common", folderName))
        .Select(p => PathUtil.ValidateWithSuffixForProfile(p, profile))
        .FirstOrDefault(x => x is not null);

      return path is not null;
    }

    /// <summary>
    /// Minimal ACF parser to extract "installdir" value from an appmanifest file.
    /// </summary>
    private static string TryParseInstallDirFromAcf(string manifestPath)
    {
      // Extremely simple approach: scan lines for key "installdir" and read its value.
      // appmanifest files are vdf-like: "installdir"        "Hollow Knight"
      foreach (var line in File.ReadLines(manifestPath))
      {
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith("\"installdir\"")) continue;
        var parts = trimmed.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) continue;
        var value = parts[1].Trim('"');
        if (!string.IsNullOrWhiteSpace(value)) return value;
      }
      return string.Empty;
    }

    public static Settings? Load()
    {
      if (!File.Exists(ConfigPath))
        return null;

      Debug.WriteLine($"ConfigPath: File @ {ConfigPath} exists.");

      string content = File.ReadAllText(ConfigPath);

      try
      {
        return JsonSerializer.Deserialize<Settings>(content);
      }
      // The JSON is malformed, act as if we don't have settings as a backup
      catch (Exception e) when (e is JsonException or ArgumentNullException)
      {
        return null;
      }
    }

    public static Settings Create(string path)
    {
      // Create from ManagedPath.
      var settings = new Settings(path);

      settings.Save();

      return settings;
    }

    public void Save()
    {
      string content = JsonSerializer.Serialize(this, new JsonSerializerOptions()
      {
        WriteIndented = true,
      });

      GetOrCreateDirPath();

      string path = ConfigPath;

      File.WriteAllText(path, content);
    }
  }
}
