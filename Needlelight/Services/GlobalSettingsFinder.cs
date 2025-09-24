using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Needlelight.Interfaces;
using Needlelight.Models;

namespace Needlelight.Services;

public class GlobalSettingsFinder : IGlobalSettingsFinder
{
  private readonly ISettings? Settings;
  public GlobalSettingsFinder(ISettings? settings)
  {
    Settings = settings;
  }

  // Make GetSavesFolder independent of instance state to avoid static access to instance members.
  // Instead of referencing the instance field `Settings` from a static method (which produced
  // CS0120), load the persisted settings if available and fall back to the default profile.
  public static string GetSavesFolder()
  {
    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    var osKey = OperatingSystem.IsWindows() ? "windows"
        : OperatingSystem.IsMacOS() ? "mac"
        : OperatingSystem.IsLinux() ? "linux" : "";

    // Load persisted settings (if any) to determine current profile. This avoids accessing
    // the non-static instance field `Settings` from a static method.
    var loadedSettings = Needlelight.Settings.Load();
    var profile = loadedSettings?.CurrentProfile ?? GameProfiles.HollowKnight;

    if (string.IsNullOrEmpty(osKey) || !profile.SavePaths.TryGetValue(osKey, out var relPaths))
      return string.Empty;

    foreach (var rel in relPaths)
    {
      var candidate = Path.Combine(userProfile, rel.Replace('/', Path.DirectorySeparatorChar));
      if (Directory.Exists(candidate) || File.Exists(candidate))
        return candidate;
    }

    // Fallback to first known path even if it does not exist yet, so we place files consistently
    return Path.Combine(userProfile, relPaths.First().Replace('/', Path.DirectorySeparatorChar));
  }

  private readonly string[] ModBaseClasses = new[]
  {
        "Modding.Mod", // main one
        "SFCore.Generics.SaveSettingsMod",
        "SFCore.Generics.FullSettingsMod",
        "SFCore.Generics.GlobalSettingsMod",
        "Satchel.BetterPreloads.BetterPreloadsMod",
    };

  public string? GetSettingsFileLocation(ModItem modItem) => GetSettingsFileLocation(modItem, GetSavesFolder());
  public string? GetSettingsFileLocation(ModItem modItem, string savesFolder)
  {
    if (Settings == null) return null;

    try
    {
      var exactPath = GetGSFileName(savesFolder, modItem.Name);

      if (File.Exists(exactPath))
        return exactPath;

      var strippedPath = GetGSFileName(savesFolder, modItem.Name.Replace(" ", string.Empty));

      if (File.Exists(strippedPath))
        return strippedPath;

      var pathWithModSuffix = GetGSFileName(savesFolder, modItem.Name + "Mod");

      if (File.Exists(pathWithModSuffix))
        return pathWithModSuffix;

      var result = TryGettingModClassName(modItem, savesFolder);

      return result != null ? GetGSFileName(savesFolder, result) : null;
    }
    catch (Exception)
    {
      return null;
    }
  }

  private string? TryGettingModClassName(ModItem modItem, string savesFolder)
  {
    if (modItem.State is not ExistsModState state || Settings == null)
      return null;

    var modsFolder = state.Enabled ? Settings.ModsFolder : Settings.DisabledFolder;

    string modItemFolder = Path.Combine(modsFolder, modItem.Name);

    if (!Directory.Exists(modItemFolder))
      return null;

    foreach (var dll in Directory.GetFiles(modItemFolder).Where(x => x.EndsWith(".dll")))
    {
      using var asmDefinition = AssemblyDefinition.ReadAssembly(dll);
      foreach (var ty in asmDefinition.MainModule.Types.Where(ty => ty.IsClass && !ty.IsAbstract))
      {
        if (ModBaseClasses.Any(x => ty.BaseType is not null && ty.BaseType.FullName.StartsWith(x)) &&
            File.Exists(GetGSFileName(savesFolder, ty.Name)))
        {
          return ty.Name;
        }
      }
    }
    return null;
  }

  private string GetGSFileName(string savesFolder, string modName) =>
      Path.Combine(savesFolder, modName + ".GlobalSettings.json");
}

