using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using JetBrains.Annotations;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Microsoft.Extensions.DependencyInjection;
using Lumafly.Interfaces;
using Lumafly.Services;
using Lumafly.Util;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PropertyChanged.SourceGenerator;
using Lumafly.Enums;
using FileSystem = System.IO.Abstractions.FileSystem;
using Lumafly.Models;

namespace Lumafly.ViewModels
{
  public partial class MainWindowViewModel : ViewModelBase
  {
    internal static bool _Debug
    {
      get
      {
#if DEBUG
                return true;
#else
        return false;
#endif
      }
    }

    private static bool isFirstLoad { get; set; } = true;
    public static MainWindowViewModel? Instance { get; private set; }

    public bool IsLoadingModPack = false;
    public string LastLoadedPack = "";

    [Notify]
    private LoadingViewModel _loadingPage { get; set; }

    [UsedImplicitly]
    private ViewModelBase Content => Loading || SelectedTabIndex < 0 ? LoadingPage : Tabs[SelectedTabIndex].Item;
    public IBrush BorderBrush => new SolidColorBrush(Color.FromRgb(0x28, 0x28, 0x28));
    public Thickness BorderThickness => new(1);
    public CornerRadius CornerRadius => new(3);
  public string AppName => "LumaflyV2";
    public string AppVersion => $"v{Assembly.GetExecutingAssembly().GetName().Version}";

    // Expose mode/profile for app bar pills; sensible defaults if services not yet initialized
    public string CurrentMode => "Modded";
    public string ActiveProfileName
    {
      get
      {
        try
        {
          var s = Settings.Load();
          var key = s?.Game ?? Lumafly.Models.GameProfiles.HollowKnightKey;
          return Lumafly.Models.GameProfiles.GetByKey(key).Name;
        }
        catch
        {
          return Lumafly.Models.GameProfiles.HollowKnight.Name;
        }
      }
    }

    // Top bar game selector backing properties
    public ObservableCollection<string> TopBarAvailableGames { get; } = new(new[] { GameProfiles.HollowKnight.Name, GameProfiles.Silksong.Name });

    public string TopBarSelectedGame
    {
      get
      {
        try
        {
          var s = Settings.Load();
          var key = s?.Game ?? GameProfiles.HollowKnightKey;
          return GameProfiles.GetByKey(key).Name;
        }
        catch
        {
          return GameProfiles.HollowKnight.Name;
        }
      }
      set
      {
        var key = value == GameProfiles.Silksong.Name ? GameProfiles.SilksongKey : GameProfiles.HollowKnightKey;
        // Defer to a centralized async switch that updates ManagedFolder and reloads services.
        Dispatcher.UIThread.Post(async () =>
        {
          await SwitchGameAsync(key);
        });
      }
    }

    [Notify]
    private ObservableCollection<SelectableItem<ViewModelBase>> _tabs = new();

    [Notify]
    private int _selectedTabIndex = -1;

    [Notify]
    private bool _loading = true;

  private int _bottomNavIndex;

    public int BottomNavIndex
    {
      get => SelectedTabIndex switch
      {
        // Map existing tabs to: 0=Game Launcher(Info), 1=Mods, 2=Settings
        0 => 0,
        1 => 1,
        3 => 2,
        _ => 0
      };
      set
      {
        // Remap bottom nav to actual SelectedTabIndex: 0->0 (Info), 1->1 (Mods), 2->3 (Settings)
        var mapped = value switch
        {
          0 => 0,
          1 => 1,
          2 => 3,
          _ => 0
        };
        if (SelectedTabIndex != mapped)
          SelectedTabIndex = mapped;
        _bottomNavIndex = value;
        RaisePropertyChanged(nameof(BottomNavIndex));
      }
    }

  // BottomNavIndex getter maps to SelectedTabIndex; bindings will re-evaluate when SelectedTabIndex changes.

    /// <summary>
    /// The main function that loads the data the app needs and sets up all the services
    /// </summary>
    /// <param name="initialTab">The index of the tab that is shown after load is finished</param>
    /// <param name="loadingTaskDetails">If a task needs to be done that needs a reload, its details are provided here</param>
    private async Task Impl(int initialTab, LoadingTaskDetails? loadingTaskDetails)
    {
      if (loadingTaskDetails is not null)
      {
        LoadingPage = new LoadingViewModel(loadingTaskDetails.Value.LoadingMessage);
        await loadingTaskDetails.Value.Task.Invoke();
      }
      else
      {
        LoadingPage = new LoadingViewModel();
      }

  Trace.WriteLine($"Opening LumaflyV2 Version: {Assembly.GetExecutingAssembly().GetName().Version}");

      var urlSchemeHandler = new UrlSchemeHandler(handled: !isFirstLoad);

      await HandleURLSchemeCommand(urlSchemeHandler);

      Trace.WriteLine("Checking if up to date...");
      Settings settings = Settings.Load() ?? Settings.Create(await GetSettingsPath());
      var appUpdater = new AppUpdater(settings);

      await appUpdater.CheckUpToDate();

      var sc = new ServiceCollection();
      var fs = new FileSystem();

      Trace.WriteLine("Loading settings.");

      HandleResetUrlScheme(urlSchemeHandler);
      HandleResetAllGlobalSettingsUrlScheme(urlSchemeHandler);



      if (settings.PreferredLanguage == null)
      {
        settings.PreferredLanguage =
            Enum.TryParse(Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName, out SupportedLanguages preferredLanguage)
            ? preferredLanguage
            : SupportedLanguages.en;
        settings.Save();
      }

      if (!PathUtil.ValidateExisting(settings.ManagedFolder))
        settings = await ResetSettings();

      await EnsureAccessToConfigFile();

      HandleLinkUrlScheme(settings, urlSchemeHandler);

      Trace.WriteLine("Fetching links");

      (ModLinks ml, ApiLinks al) content;

      void AddSettings(HttpClient hc)
      {
        hc.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
          NoCache = true,
          MustRevalidate = true
        };

  hc.DefaultRequestHeaders.Add("User-Agent", "LumaflyV2");
      }

      HttpClient hc = new HttpClient();
      LumaflyMode lumaflyMode;

      var modLinksCache = Path.Combine(Settings.ConfigFolderPath, "Modlinks.xml");
      var apiLinksCache = Path.Combine(Settings.ConfigFolderPath, "ApiLinks.xml");

      try
      {
        ResultInfo<(ModLinks, ApiLinks)>? res = null;

        if (settings.UseCustomModlinks)
        {
          try
          {
            res = await WorkaroundHttpClient.TryWithWorkaroundAsync(
                settings.RequiresWorkaroundClient
                    ? HttpSetting.OnlyWorkaround
                    : HttpSetting.TryBoth,
                f => ModDatabase.FetchContent(f, settings, fetchOfficial: false),
                AddSettings);
          }
          catch (InvalidModlinksException)
          {
      await MessageBoxUtil.GetMessageBoxStandardWindow(
        "Invalid Custom Modlinks",
        $"LumaflyV2 was unable to load modlinks from {settings.CustomModlinksUri}. LumaflyV2 will load official modlinks instead.",
                icon: Icon.Error
            ).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
          }
        }

        // if above failed or skipped, res will be null

        res ??= await WorkaroundHttpClient.TryWithWorkaroundAsync(
            settings.RequiresWorkaroundClient
                ? HttpSetting.OnlyWorkaround
                : HttpSetting.TryBoth,
            f => ModDatabase.FetchContent(f, settings, fetchOfficial: true),
            AddSettings);


        content = res.Result;

        if (res.NeededWorkaround && !settings.RequiresWorkaroundClient)
        {
          settings.RequiresWorkaroundClient = true;
          settings.Save();
        }

        hc = res.Client;
        lumaflyMode = LumaflyMode.Online;
        await CacheModAndApiLinksForOffline(modLinksCache, apiLinksCache, content);
      }
      catch (Exception e) when (e is TaskCanceledException { CancellationToken.IsCancellationRequested: true } or HttpRequestException)
      {
        string failedOp = e switch
        {
          TaskCanceledException => Resources.MWVM_Impl_Error_Fetch_ModLinks_Timeout,
          HttpRequestException when e.ToString().Contains("No such host is known.")
              => string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Error, Resources.MVVM_DNSError),
          HttpRequestException http => string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Error, http.StatusCode),
          _ => throw new ArgumentOutOfRangeException()
        };

        var linksCacheExists = File.Exists(modLinksCache) && File.Exists(apiLinksCache);

        if (!linksCacheExists)
        {
          await MessageBoxUtil.GetMessageBoxStandardWindow
          (
              title: Resources.MWVM_Impl_Error_Fetch_ModLinks_Msgbox_Title,
              text: string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Msgbox_Text, failedOp),
              icon: Icon.Error
          ).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
          throw;
        }

        var offlineMode = await MessageBoxUtil.GetMessageBoxStandardWindow
        (
            title: Resources.MVVM_UnableToGetModlinks,
            text: string.Format(Resources.MWVM_Impl_Error_Fetch_ModLinks_Msgbox_Text, failedOp) + "\n\n" +
                  $"{Resources.MVVM_LaunchOfflineMode}",
            icon: Icon.Warning,
            @enum: ButtonEnum.YesNo
        ).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow()) == ButtonResult.Yes;

        if (!offlineMode) throw;

        content = (ModDatabase.FromString<ModLinks>(await File.ReadAllTextAsync(modLinksCache)),
            ModDatabase.FromString<ApiLinks>(await File.ReadAllTextAsync(apiLinksCache)));

        lumaflyMode = LumaflyMode.Offline;
      }

      Trace.WriteLine("Fetched links successfully");

      var installedMods = await InstalledMods.Load(
          fs,
          settings,
          content.ml
      );

      Trace.WriteLine("Creating service collection");
      sc
        .AddSingleton<ISettings>(_ => settings)
        .AddSingleton<IUrlSchemeHandler>(_ => urlSchemeHandler)
        .AddSingleton(hc)
        .AddSingleton<IAppUpdater>(_ => appUpdater)

        .AddSingleton<IGlobalSettingsFinder, GlobalSettingsFinder>()
        .AddSingleton<ICheckValidityOfAssembly, CheckValidityOfAssembly>()
        .AddSingleton<IOnlineTextStorage, PastebinTextStorage>()
        .AddSingleton<IFileSystem>(_ => fs)
        .AddSingleton<IModSource>(_ => installedMods)
        .AddSingleton<IModDatabase, ModDatabase>(sp
            => new ModDatabase(sp.GetRequiredService<IModSource>(), sp.GetRequiredService<IGlobalSettingsFinder>(), content, settings))
        .AddSingleton<IInstaller, Installer>()
        .AddSingleton<IPackManager, PackManager>()
        .AddSingleton<ModListViewModel>(sp =>
            new ModListViewModel(sp.GetRequiredService<ISettings>(),
                sp.GetRequiredService<IModDatabase>(),
                sp.GetRequiredService<IInstaller>(),
                sp.GetRequiredService<IModSource>(),
                sp.GetRequiredService<IGlobalSettingsFinder>(),
                sp.GetRequiredService<IUrlSchemeHandler>(),
                sp.GetRequiredService<IPackManager>(),
                lumaflyMode))
        .AddSingleton<SettingsViewModel>()
        .AddSingleton<InfoViewModel>()
        .AddSingleton<PackManagerViewModel>();

      Trace.WriteLine("Building service provider");
      ServiceProvider sp = sc.BuildServiceProvider(new ServiceProviderOptions
      {
        ValidateOnBuild = true
      });
      Trace.WriteLine("Built service provider");

      Trace.WriteLine("Displaying model");
      Tabs = new ObservableCollection<SelectableItem<ViewModelBase>>
            {
                new(sp.GetRequiredService<InfoViewModel>(), Resources.XAML_Info, false),
                new(sp.GetRequiredService<ModListViewModel>(), Resources.XAML_Mods, false),
                new(sp.GetRequiredService<PackManagerViewModel>(), Resources.XAML_Packs, false),
                new(sp.GetRequiredService<SettingsViewModel>(), Resources.XAML_Settings, false),
            };
      SelectedTabIndex = initialTab;
      Trace.WriteLine("Selected Tab 0");
    }

    private async Task HandleURLSchemeCommand(IUrlSchemeHandler urlSchemeHandler)
    {
      var args = Environment.GetCommandLineArgs();
      if (args.Length == 2) // only accept 2 args, the exe location and the uri
      {
        urlSchemeHandler.SetCommand(args[1]);
      }

      if (!urlSchemeHandler.Handled && urlSchemeHandler.UrlSchemeCommand != UrlSchemeCommands.none)
      {
        var prompt = urlSchemeHandler.UrlSchemeCommand switch
        {
          UrlSchemeCommands.download => $"Download the following mods: {GetListOfMods()}",
          UrlSchemeCommands.reset => $"Reset LumaflyV2's persistent settings",
          UrlSchemeCommands.forceUpdateAll => $"Reinstall all mods which could help fix issues that happened because mods are not downloaded correctly.",
          UrlSchemeCommands.customModLinks => $"Load a custom mod list from: {urlSchemeHandler.Data}",
          UrlSchemeCommands.removeAllModsGlobalSettings => $"Reset all mods' global settings",
          UrlSchemeCommands.removeGlobalSettings => $"Remove global settings for the following mods: {GetListOfMods()}",
          _ => string.Empty
        };

        if (prompt == string.Empty) return;

        bool accepted = await LoadingPage.ShowUrlSchemePrompt(prompt);

        if (!accepted) urlSchemeHandler.SetCommand(UrlSchemeCommands.none);
      }


      string GetListOfMods()
      {
        var mods = urlSchemeHandler.ParseDownloadCommand(urlSchemeHandler.Data);
        if (mods.Count == 0) return "None";
        var list = "\n\n";
        foreach (var (mod, url) in mods)
        {
          list += mod;
          if (url is not null)
            list += $" ({Resources.MVVM_NotInModlinks_Disclaimer}),\n";
          else
            list += ",\n";
        }

        return list;
      }
    }

    private void HandleResetUrlScheme(IUrlSchemeHandler urlSchemeHandler)
    {
      if (urlSchemeHandler is { Handled: false, UrlSchemeCommand: UrlSchemeCommands.reset })
      {
        bool success = false;
        Exception? exception = null;
        try
        {
          foreach (var file in FileUtil.GetAllFilesInDirectory(Settings.GetOrCreateDirPath())
                       .Where(file => !file.Name.EndsWith(Program.LoggingFileExtension)))
          {
            file.Delete();
          }

          success = true;
        }
        catch (Exception e)
        {
          Trace.TraceError(e.ToString());
          success = false;
          exception = e;
        }

        Dispatcher.UIThread.InvokeAsync(async () => await urlSchemeHandler.ShowConfirmation(
            title: "Reset installer from command",
            message: success ? "The installer has been reset" : $"The installer could not be reset. Please try again.\\n{exception}",
            success ? Icon.Success : Icon.Warning
        ));
      }
    }

    private void HandleResetAllGlobalSettingsUrlScheme(IUrlSchemeHandler urlSchemeHandler)
    {
      if (urlSchemeHandler is { Handled: false, UrlSchemeCommand: UrlSchemeCommands.removeAllModsGlobalSettings })
      {
        bool success = false;
        Exception? exception = null;
        try
        {
          var di = new DirectoryInfo(GlobalSettingsFinder.GetSavesFolder());

          foreach (var file in di.GetFiles())
          {
            if (file.FullName.EndsWith(".GlobalSettings.json") ||
                file.FullName.EndsWith(".GlobalSettings.json.bak"))
            {
              file.Delete();
            }
          }

          success = true;
        }
        catch (Exception e)
        {
          Trace.TraceError(e.ToString());
          success = false;
          exception = e;
        }

        Dispatcher.UIThread.InvokeAsync(async () => await urlSchemeHandler.ShowConfirmation(
            title: "Reset all mod global settings installer from command",
            message: success
                ? "All mods global settings have been reset."
                : $"All mods global settings could not be reset. Please try again.\n{exception}",
            success ? Icon.Success : Icon.Warning));
      }
    }

    private void HandleLinkUrlScheme(ISettings settings, IUrlSchemeHandler urlSchemeHandler)
    {
      if (!urlSchemeHandler.Handled)
      {
        if (urlSchemeHandler.UrlSchemeCommand == UrlSchemeCommands.customModLinks)
        {
          bool success = false;
          if (string.IsNullOrEmpty(urlSchemeHandler.Data))
          {
            Trace.TraceError($"{UrlSchemeCommands.customModLinks}:{urlSchemeHandler.Data} not found");
            success = false;
          }
          else
          {
            settings.UseCustomModlinks = true;
            settings.CustomModlinksUri = urlSchemeHandler.Data;
            success = true;
          }

          Dispatcher.UIThread.InvokeAsync(async () => await urlSchemeHandler.ShowConfirmation(
              title: "Load custom modlinks from command",
              message: success ? $"Got the custom modlinks \"{settings.CustomModlinksUri}\" from command." : "No modlinks were provided. Please try again",
              success ? Icon.Success : Icon.Warning));
        }

        if (urlSchemeHandler.UrlSchemeCommand == UrlSchemeCommands.useOfficialModLinks)
        {
          settings.UseCustomModlinks = false;

          Dispatcher.UIThread.InvokeAsync(async () => await urlSchemeHandler.ShowConfirmation(
              title: "Use official modlinks url command",
              message: "LumaflyV2 will now use official modlinks"));
        }

        settings.Save();
      }
    }

    private async Task CacheModAndApiLinksForOffline(string modLinksCache, string apiLinksCache, (ModLinks ml, ApiLinks al) content)
    {
      try
      {
        await File.WriteAllTextAsync(modLinksCache, content.ml.Raw);
        await File.WriteAllTextAsync(apiLinksCache, content.al.Raw);
      }
      catch (Exception e)
      {
        Trace.TraceError($"Unable to cache modlinks or apilinks {e}");
      }
    }

    private static async Task EnsureAccessToConfigFile()
    {
      var s = Settings.Load();
      var cfgPath = s is null ? InstalledMods.LegacyConfigPath : InstalledMods.GetConfigPathFor(s);
      if (!File.Exists(cfgPath)) return;
      try
      {
        bool configAccessSuccess = false;
        while (!configAccessSuccess)
        {
          try
          {
            var configFile = File.Open(cfgPath, FileMode.Open, FileAccess.ReadWrite,
                FileShare.None);
            configFile.Close();
            configAccessSuccess = true;
          }
          catch (IOException)
          {
            string additionalInfo = "";
            try
            {
              if (OperatingSystem.IsWindows())
              {
                var processes = FileAccessLookup.WhoIsLocking(cfgPath);
                additionalInfo =
                    $"\n\nProcesses that are locking the file:\n{string.Join("\n", processes.Select(x => x.ProcessName))}";
              }
            }
            catch (Exception)
            {
              //ignored as its not a requirement
            }

            await MessageBoxUtil.GetMessageBoxStandardWindow
            (
                title: "File access error!!",
                text: $"LumaflyV2 cannot run without being able to access {cfgPath}.\n" +
                      $"Please close any other apps that could be using that" + additionalInfo,
                icon: Icon.Error
            ).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());
          }
        }
      }
      catch (Exception e)
      {
        Trace.TraceError($"Error happened when trying to find out if config files are locked. {e}");
      }
    }

    private static async Task<Settings> ResetSettings()
    {
      Trace.WriteLine("Settings path is invalid, forcing re-selection.");

      await MessageBoxUtil.GetMessageBoxStandardWindow
      (
          new MessageBoxStandardParams
          {
            ContentHeader = Resources.MWVM_Warning,
            ContentMessage = Resources.GAME_InvalidSavedPath_Message.FormatWith(Lumafly.Models.GameProfiles.HollowKnight.Name),
            // The auto-resize for this lib is buggy, so
            // ensure that the message doesn't get cut off
            MinWidth = 550
          }
      ).ShowWindowDialogAsync(AvaloniaUtils.GetMainWindow());

      return Settings.Create(await GetSettingsPath());
    }

    private static async Task<string> GetSettingsPath()
    {
  // Detect based on currently selected/loaded profile, not whatever was persisted before
  var loaded = Settings.Load();
  var profile = loaded?.CurrentProfile ?? Lumafly.Models.GameProfiles.HollowKnight;
  var path = await Settings.TryAutoDetect(profile);

  // Ensure `profile` is available for both the "unable to detect" and "detected" UI flows.
  // Previously `profile` was declared after being used which caused CS0103/CS0841 errors.

      if (path is null)
      {
        var info = MessageBoxUtil.GetMessageBoxStandardWindow
        (
            new MessageBoxStandardParams
            {
              ContentHeader = Resources.MWVM_Info,
              ContentMessage = Resources.GAME_UnableToDetect_Message.FormatWith(profile.Name),
              MinWidth = 550
            }
        );

        await info.ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());

        // If we can load an existing settings just to get the game selection, pass its profile to the dialog
        return await PathUtil.SelectPath(profile: profile);
      }

      Trace.WriteLine($"Settings doesn't exist. Creating it at detected path {path}.");

      var window = MessageBoxUtil.GetMessageBoxStandardWindow
      (
          new MessageBoxStandardParams
          {
            ContentHeader = Resources.MWVM_DetectedPath_Title,
            ContentMessage = Resources.GAME_DetectedPath_Message.FormatWith(profile.Name, path.Root),
            ButtonDefinitions = ButtonEnum.YesNo
          }
      );

      ButtonResult res = await window.ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());

      if (res == ButtonResult.Yes)
        return Path.Combine(path.Root, path.Suffix);

      var loaded2 = Settings.Load();
      var profile2 = loaded2?.CurrentProfile ?? Lumafly.Models.GameProfiles.HollowKnight;
      return await PathUtil.SelectPath(profile: profile2);
    }

    public MainWindowViewModel()
    {
      Instance = this;
      _loadingPage = new LoadingViewModel();
      Dispatcher.UIThread.InvokeAsync(async () => await LoadApp());
      Trace.WriteLine("Loaded app");
      ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)!.ShutdownRequested +=
          (_, _) => Program.CloseTraceFile();
    }

    public async Task LoadApp(int initialTab = 0, LoadingTaskDetails? loadingTaskDetails = null)
    {
      Loading = true;
      try
      {
        await Impl(initialTab, loadingTaskDetails);
      }
      catch (Exception e)
      {
        Trace.TraceError(e.ToString());
        Trace.Flush();

        if (Debugger.IsAttached)
          Debugger.Break();

        Environment.Exit(-1);

        throw;
      }

      Loading = false;
      if (isFirstLoad) isFirstLoad = false;
    }

    /// <summary>
    /// Switches the active game profile, updates the ManagedFolder if a matching install is detected,
    /// saves settings, and reloads the app while preserving the current tab.
    /// </summary>
    public async Task SwitchGameAsync(string key)
    {
      try
      {
        var s = Settings.Load();
        if (s is null) return;
        var oldKey = s.Game ?? GameProfiles.HollowKnightKey;
        if (string.Equals(oldKey, key, StringComparison.OrdinalIgnoreCase))
        {
          // no change
          return;
        }

        s.Game = key;
        // Persist the selected Game immediately so other components see it if they read from disk.
        s.Save();

        // Try to auto-detect install for the selected game and update ManagedFolder accordingly.
        var selectedProfile = Lumafly.Models.GameProfiles.GetByKey(key);
        bool detectedForSelected = false;
        try
        {
          var vp = await Settings.TryAutoDetect(selectedProfile);
          if (vp is not null)
          {
            var managed = System.IO.Path.Combine(vp.Root, vp.Suffix);
            if (System.IO.Directory.Exists(managed))
            {
              s.ManagedFolder = managed;
              detectedForSelected = true;
            }
          }
        }
        catch { /* best-effort; keep existing ManagedFolder if detection fails */ }

        // If we didn’t detect a matching install and the current ManagedFolder doesn’t belong to the selected profile,
        // clear it to force a re-selection on reload. This prevents using HK’s path when Silksong is selected (and vice versa).
        if (!detectedForSelected)
        {
          try
          {
            var dataFolders = Lumafly.Models.GameProfiles.GetDataFolderCandidates(selectedProfile)
                .Concat(new[] { System.IO.Path.Combine("Contents", "Resources", "Data") })
                .Distinct()
                .Select(df => System.IO.Path.Combine(df, "Managed"));

            bool managedMatchesSelected = dataFolders.Any(df => s.ManagedFolder?.Replace('/', System.IO.Path.DirectorySeparatorChar)
                .EndsWith(df.Replace('/', System.IO.Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) == true);

            if (!managedMatchesSelected)
            {
              s.ManagedFolder = string.Empty; // invalidate to trigger ResetSettings()
            }
          }
          catch { /* ignore */ }
        }

        // Clear InstalledMods cache when switching games to avoid cross-game leakage.
        try
        {
          var currentConfigPath = InstalledMods.GetConfigPathFor(s);
          if (System.IO.File.Exists(currentConfigPath))
          {
            System.IO.File.Delete(currentConfigPath);
          }
        }
        catch { /* ignore */ }

        s.Save();

        // Notify header bindings right away
        RaisePropertyChanged(nameof(TopBarSelectedGame));
        RaisePropertyChanged(nameof(ActiveProfileName));

        // Preserve current selected tab when reloading
  var currentTab = SelectedTabIndex;
  await LoadApp(currentTab < 0 ? 0 : currentTab);
      }
      catch { /* ignore dispatch-time errors */ }
    }
  }
}
