  using System.Collections.Generic;

  namespace Lumafly.Models;

  /// <summary>
  /// Describes a target game that Lumafly can manage (e.g., Hollow Knight or Silksong).
  /// </summary>
  public class GameProfile
  {
    /// <summary>
    /// Canonical display name and default Steam folder name (e.g., "Hollow Knight", "Silksong").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Steam App ID as string. May be empty if unknown.
    /// </summary>
    public string SteamAppId { get; init; } = string.Empty;

    /// <summary>
    /// Executable file names for Windows/macOS derivation (e.g., ["Hollow Knight.exe", "hollow_knight.exe"]).
    /// </summary>
    public IReadOnlyList<string> ExeNames { get; init; } = new List<string>();

    /// <summary>
    /// Unity data folder name (e.g., "Hollow Knight_Data", "Silksong_Data").
    /// </summary>
    public string DataFolder { get; init; } = string.Empty;

    /// <summary>
    /// Optional GOG registry/game IDs.
    /// </summary>
    public IReadOnlyList<string>? GogIds { get; init; }

    /// <summary>
    /// Save path patterns by OS. Keys: "windows", "mac", "linux". Each value is an array of candidate relative paths under the user profile.
    /// These are provided for services that need to locate save/global settings folders.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> SavePaths { get; init; } = new Dictionary<string, string[]>();
  }

  public static class GameProfiles
  {
    public const string HollowKnightKey = "hollow_knight";
    public const string SilksongKey = "silksong";

    public static readonly GameProfile HollowKnight = new()
    {
      Name = "Hollow Knight",
      SteamAppId = "367520",
      ExeNames = new[] { "Hollow Knight.exe", "hollow_knight.exe" },
      DataFolder = "Hollow Knight_Data",
      GogIds = new[] { "1308320804" },
      SavePaths = new Dictionary<string, string[]>
      {
        // Under user profile
        ["windows"] = new[] { "AppData/LocalLow/Team Cherry/Hollow Knight" },
        ["mac"] = new[] { "Library/Application Support/unity.Team Cherry.Hollow Knight" },
        ["linux"] = new[] { ".config/unity3d/Team Cherry/Hollow Knight" }
      }
    };

    public static readonly GameProfile Silksong = new()
    {
      // Use the full Steam folder name so Settings.TryAutoDetect (which composes
      // steamapps/common/{profile.Name}) will find the Silksong installation in the
      // Steam default location: ".../steamapps/common/Hollow Knight Silksong".
      Name = "Hollow Knight Silksong",
  SteamAppId = "1030300", // Silksong Steam App ID (provided)
      // Use the Steam game executable name as observed in the official Steam folder.
      ExeNames = new[] { "Hollow Knight Silksong.exe", "Silksong.exe" }, // include common variants
      // Data folder name in Steam common directory; keep consistent with exe prefix + _Data
      DataFolder = "Hollow Knight Silksong_Data",
      GogIds = new string[] { /* TODO: Confirm GOG IDs if applicable */ },
      SavePaths = new Dictionary<string, string[]>
      {
        // Likely save locations - mirror Hollow Knight pattern but use Silksong naming
        ["windows"] = new[] { "AppData/LocalLow/Team Cherry/Silksong" },
        ["mac"] = new[] { "Library/Application Support/unity.Team Cherry.Silksong" },
        ["linux"] = new[] { ".config/unity3d/Team Cherry/Silksong" }
      }
    };

    public static GameProfile GetByKey(string key)
    {
      key = key?.Trim().ToLowerInvariant() ?? HollowKnightKey;
      return key switch
      {
        SilksongKey or "hkss" => Silksong,
        _ => HollowKnight
      };
    }

    /// <summary>
    /// Produces candidate Unity data folder names given a profile. This includes the canonical DataFolder and a Steam-style lower/underscored variant.
    /// </summary>
    public static string[] GetDataFolderCandidates(GameProfile profile)
    {
      var canonical = profile.DataFolder;
      var alt = canonical;

      // Convert prefix to steam-style lowercase with underscores, preserving the trailing "_Data" with capital D if present
      const string suffix = "_Data";
      if (canonical.EndsWith(suffix))
      {
        var prefix = canonical.Substring(0, canonical.Length - suffix.Length);
        prefix = prefix.Replace(' ', '_').ToLowerInvariant();
        alt = prefix + suffix;
      }
      else
      {
        alt = canonical.Replace(' ', '_').ToLowerInvariant();
      }

      return canonical == alt ? new[] { canonical } : new[] { canonical, alt };
    }
  }
