using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using MsBox.Avalonia.Enums;
using PropertyChanged.SourceGenerator;
using Lumafly.Enums;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Services;
using Lumafly.Util;

namespace Lumafly.ViewModels;

public partial class InfoViewModel : ViewModelBase
{
  private readonly IInstaller _installer;
  private readonly IModSource _modSource;
  private readonly ISettings _settings;
  private readonly IUrlSchemeHandler _urlSchemeHandler;
  private readonly HttpClient _hc;

  [Notify]
  private bool _isLaunchingGame;
  [Notify]
  private string _additionalInfo = "";
  [Notify]
  private bool _additionalInfoVisible;

  public InfoViewModel(IInstaller installer, IModSource modSource, ISettings settings, HttpClient hc, IUrlSchemeHandler urlSchemeHandler)
  {
    Trace.WriteLine("Initializing InfoViewModel");
    _installer = installer;
    _modSource = modSource;
    _settings = settings;
    _hc = hc;
    _urlSchemeHandler = urlSchemeHandler;
    Task.Run(FetchAdditionalInfo);
    Dispatcher.UIThread.Invoke(() => HandleLaunchUrlScheme(_urlSchemeHandler));
  }
  public void OpenLink(object link) => Process.Start(new ProcessStartInfo((string)link) { UseShellExecute = true });

  // Process name detection now derived from profile exe names

  public async Task LaunchGame(object _isVanilla) => await _LaunchGame(bool.Parse((string)_isVanilla));


  /// <summary>
  /// Launches the game
  /// </summary>
  /// <param name="isVanilla">Set to true for vanilla game, set to false for modded game and set to null for no change to current api state</param>
  private async Task _LaunchGame(bool? isVanilla)
  {
    Trace.WriteLine("Launching game");
    IsLaunchingGame = true;
    try
    {
      // remove any existing instance of the selected game (match by exe name prefixes without extension)
      bool IsGameProcess(Process p)
      {
        var prefixes = _settings.CurrentProfile.ExeNames
            .Select(n => Path.GetFileNameWithoutExtension(n))
            .Select(n => n)
            .ToArray();
        return prefixes.Any(pre => p.ProcessName.StartsWith(pre, StringComparison.OrdinalIgnoreCase));
      }

      if (Process.GetProcesses().FirstOrDefault(IsGameProcess) is { } proc)
        proc.Kill();

      await _installer.CheckAPI();

      if (isVanilla != null)
      {
        if (!(_modSource.ApiInstall is NotInstalledState or InstalledState { Enabled: false } && isVanilla.Value
              || _modSource.ApiInstall is InstalledState { Enabled: true } && !isVanilla.Value))
        {
          await ModListViewModel.ToggleApiCommand(_modSource, _installer);
        }
      }

      // Resolve install directory and executable from the currently selected profile.
      // We derive the install folder from the configured Managed folder (which points to the Managed directory inside the game's Data folder).
      var currentProfile = _settings.CurrentProfile;

      // ManagedFolder is expected to point at the game's Managed folder (e.g., .../Hollow Knight_Data/Managed).
      // Navigate up to the install folder (exe folder). This mirrors previous logic but centralizes resolution here so
      // launching always uses the selected profile and its configured ExeNames.
      var managedFolderInfo = new DirectoryInfo(_settings.ManagedFolder);
      var managedParent = managedFolderInfo.Parent; // *_Data or Data folder
      var installFolder = managedParent?.Parent; // exe folder (two levels up from Managed)

      // Mac has an extra nested structure; preserve previous mac handling by walking up additional folders when needed.
      if (OperatingSystem.IsMacOS() && managedParent?.Parent != null)
      {
        // On macOS, the structure can add two extra levels; follow prior approach to reach exe folder.
        installFolder = managedParent.Parent.Parent; // safe to use Parent twice -- will be null if structure differs
      }

      if (installFolder == null)
      {
        // Preserve original behavior: user-facing exception when executable cannot be resolved.
        throw new Exception($"{currentProfile.Name} executable not found");
      }

      // Build executable path from profile's configured exe names. Use the first name as canonical launch target.
      var exeNameFromProfile = currentProfile.ExeNames.FirstOrDefault() ?? string.Empty;
      var exeFullPath = Path.Combine(installFolder.FullName, exeNameFromProfile);

      // Determine whether the install appears to be a Steam install by checking for steam_api64.dll in the Managed plugins folder.
      var isSteam = false;
      try
      {
        if (managedParent != null)
        {
          isSteam = File.Exists(Path.Combine(managedParent.FullName, "Plugins", "x86_64", "steam_api64.dll"));
        }
      }
      catch
      {
        // Keep existing behavior of ignoring detection exceptions.
        isSteam = false;
      }

      // Launch via Steam protocol if Steam AppId is known.
      var appId = currentProfile.SteamAppId;
      if (isSteam && !string.IsNullOrWhiteSpace(appId))
      {
        Process.Start(new ProcessStartInfo($"steam://rungameid/{appId}") { UseShellExecute = true });
      }
      else
      {
        // Validate exe exists before attempting to launch; if not, surface a descriptive, user-facing exception (preserving behavior).
        if (!File.Exists(exeFullPath))
          throw new Exception($"{currentProfile.Name} executable not found at {exeFullPath}");

        Process.Start(new ProcessStartInfo
        {
          FileName = exeFullPath,
          WorkingDirectory = installFolder.FullName,
          UseShellExecute = true,
        });
      }

    }
    catch (Exception e)
    {
      await DisplayErrors.DisplayGenericError($"Unable to launch the game", e);
    }

    IsLaunchingGame = false;
  }

  private (string path, string name, bool isSteam) GetExecutableDetails()
  {
    string exeName;

    // get exe path
    var managedFolder = new DirectoryInfo(_settings.ManagedFolder);
    var managedParent = managedFolder.Parent; // now in *_Data or (for mac) Data folder

    var hkExeFolder = managedParent!.Parent; // now in the hk exe folder or (for mac) resources folder;

    // mac os path has 2 extra folders
    if (OperatingSystem.IsMacOS())
    {
      hkExeFolder = managedParent.Parent! // now in contents folder
          .Parent; // now in hk exe folder

      // Use the first configured exe name without extension as mac executable name (TODO confirm for Silksong)
      exeName = Path.GetFileNameWithoutExtension(_settings.CurrentProfile.ExeNames.FirstOrDefault() ?? "");
    }
    else
    {
      // Prefer profile-configured exe names on Windows; fallback to deriving from *_Data
      if (OperatingSystem.IsWindows())
      {
        var preferred = _settings.CurrentProfile.ExeNames.FirstOrDefault();
        exeName = !string.IsNullOrWhiteSpace(preferred)
          ? preferred
          : managedParent.Name.Replace("_Data", string.Empty) + ".exe"; // unity appends _Data; add .exe
      }
      else
      {
        // Non-Windows (Linux): executable typically matches *_Data prefix
        exeName = managedParent.Name.Replace("_Data", string.Empty);
      }
    }

    if (hkExeFolder is null) throw new Exception($"{_settings.CurrentProfile.Name} executable not found");
    string exePath = hkExeFolder.FullName;

    // check if path contains steam_api64.dll
    var isSteam = File.Exists(Path.Combine(
        managedParent.FullName,
        "Plugins",
        "x86_64",
        "steam_api64.dll"
    ));

    return (exePath, exeName, isSteam);
  }

  public async Task FetchAdditionalInfo()
  {
    const string additionalInfoLink = "https://raw.githubusercontent.com/TheMulhima/Lumafly/static-resources/AdditionalInfo.md";
    try
    {
      AdditionalInfo = await _hc.GetStringAsync2(
          _settings,
          new Uri(additionalInfoLink),
          new CancellationTokenSource(ModDatabase.TIMEOUT).Token);

      if (!string.IsNullOrEmpty(AdditionalInfo))
        AdditionalInfoVisible = true;
    }
    catch (Exception)
    {
      // ignored not important
    }
  }

  private async Task HandleLaunchUrlScheme(IUrlSchemeHandler urlSchemeHandler)
  {
    if (urlSchemeHandler is { Handled: false, UrlSchemeCommand: UrlSchemeCommands.launch })
    {
      if (urlSchemeHandler.Data is "")
        await _LaunchGame(null);
      else if (urlSchemeHandler.Data.ToLower() is "vanilla" or "false")
        await _LaunchGame(true);
      else if (urlSchemeHandler.Data.ToLower() is "modded" or "true")
        await _LaunchGame(false);
      else
        await _urlSchemeHandler.ShowConfirmation("Launch Game",
            "Launch game command is invalid. Please specify the launch as vanilla or modded or leave blank for regular launch",
            Icon.Warning);

      _urlSchemeHandler.FinishHandlingUrlScheme();
    }
  }
}
