using Needlelight.Enums;
using System.IO;

namespace Needlelight.Interfaces
{
  public interface ISettings
  {
    AutoRemoveUnusedDepsOptions AutoRemoveUnusedDeps { get; set; }
    bool WarnBeforeRemovingDependents { get; set; }
    bool UseCustomModlinks { get; set; }
    string CustomModlinksUri { get; set; }
    SupportedLanguages? PreferredLanguage { get; set; }
    bool LowStorageMode { get; set; }
    string ExtraSpaceTaken { get; }

    string ManagedFolder { get; set; }
    string CacheFolder { get; }

    bool RequiresWorkaroundClient { get; set; }

    string ModsFolder => Path.Combine(ManagedFolder, "Mods");
    string DisabledFolder => Path.Combine(ModsFolder, "Disabled");

    string GithubMirrorFormat { get; set; }
    bool UseGithubMirror { get; set; }

    /// <summary>
    /// Selected game key (e.g., "hollow_knight", "silksong"). Defaults to Hollow Knight when not set.
    /// </summary>
    string Game { get; set; }

    /// <summary>
    /// The resolved profile for the selected game.
    /// </summary>
    Needlelight.Models.GameProfile CurrentProfile { get; }

    void Save();
  }
}

