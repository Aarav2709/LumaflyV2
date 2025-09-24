# Needlelight Changelog

Source code adapted from: https://github.com/TheMulhima/Lumafly

[v6.0.0.0](#changes-in-v6000)

## Changes in v5.1.0.2

DATE: 2025-09-18

## Changes in v5.1.0.1

DATE: 2025-09-09

- ## Changes in v5.1.0.0
- DATE: 2025-09-09
- - Upgrade runtime to .NET 9.0 for better performance and longevity.
- - Rename application binary to Needlelight (AssemblyName change); resources updated accordingly.
- DATE: 2025-09-07
- - Silksong-inspired red theme and modernized UI components (surfaces, borders, primary buttons).
- - Fixed navigation and layout issues; top-bar game switcher and bottom navigation with icons + text.
- - Robust per-game behavior and path detection; profile-aware mods folder and installed-mods isolation.
- - Empty-state banner and success notification styling; success bar now solid maroon with border.
- - Upgraded to .NET 7 and modernized build to reduce platform-specific issues.
- - Compile-time fixes and resource sync to ensure a clean build.
- - Refactored game launch logic to be profile-aware (Hollow Knight & Silksong support).
- - Updated Silksong profile and default Steam App ID.
- - Documentation rebrand (README -> Needlelight). More UI string updates TBD.
- Update Spanish, French, Portuguese, and Chinese localizations (thanks to [Helen](https://ko-fi.com/helensb), [Dastan](https://github.com/Dastan21), [luiz_eldorado](https://github.com/luizeldorado), and [lxymahatma](https://github.com/lxymahatma)).
- Fix scroll position in modlist being reset on any popup being shown.
- Open edit pack window on pack creation.
- Make edit pack button more visible

## Changes in v3.0.0.0

- UI overhaul (side panel in mod list, new colors)
- Improve mod download speed (by not downloading MAPI every time)
- Add button to view mod readmes in app
  - [HBKit](https://ko-fi.com/hbkit) - Needlelight icon

## Changes in v2.3.0.0

- Add option to cache downloads (making uninstalling and reinstalling quicker).
- Add ability to change preffered language that Scarab uses.
- Add option to skip update.
- Use noto sans font - for uniformity bewteen OS and fixes issue with Chinese fonts.
- New url scheme commands: useOfficialModlinks and launch (usage scarab://launch/vanilla or scarab://launch/modded)
- Fully localize settings page.
- Fix issue with big log file causing Scarab to not open.
- Fix issue with showing prompts on resetting mod
- If toggling will delete a folder, ask for confirmation before doing so

- Credits
  - Original project: https://github.com/TheMulhima/Lumafly
  - [Adrin](https://twitter.com/Adrin63_?t=lbzYGgt-3Zybjb_S2xqt2A&s=09) and [Helen](https://ko-fi.com/helensb) - Spanish translations
  - [Страг](https://discordapp.com/users/274945280775028736) - Russain transaltions

## Changes in v2.2.0.0

- Add confirmation prompt for url scheme commands.
- Add additional info that can be changed without needing update.
- Correct SHA256 checking when downloading from modlinks.
- Fix handling of download url scheme when custom links provided.
- Fix crash that happens on auto uninstall dependencies confimation window.

- Credits
  - Original project: https://github.com/TheMulhima/Needlelight

## Changes in v2.1.0.0

- Additional Features

  - Improve performace by upgrading framework
  - Make it easier to update via the new auto updater in windows. Also shows change log before asking to update.
  - Add mod actions panel, a set of buttons in the mod's expander that allow you to
    - Get shareable link of that mod.
    - Reset the mod.
    - Report an error in the mod.
    - Open the folder of the mod.
    - Edit the mod's global settings file.
  - Sort mods in whats new tab by date.
  - Add info panel with relevant links and launch vanilla/modded game
  - Add French Translations (by @Dastan21).
  - Add enable all bulk action.
  - Add URL scheme handling for linux.
  - Add ability to provide custom download urls in download command.
  - Add url scheme to remove global settings.

- Bug Fixes

  - Fix the issue where scarab sometimes freezes on load.
  - Prevent occasional random crashes on load.
  - Fix the issue where toggling a mod causes it to appear in both mods folder and disabled folder.
  - Fix pinned mods
    - Fix not in modlinks mods pinned status not being reset.
    - Fix removed mods pinned status not being removed.
  - Fix manually installed button
    - Ensure only 1 mod exists by deleting old if exists.
    - Ensure the mod state is correct after placing it.
  - Fix mod filter button showing selected when its not.
  - Fix not installed mods showing in ask prompt for uninstalled unused dependencies.
  - Don't delete log files in reset url scheme which causes resetting to always show an error.
  - Get correct vanilla assembly for each os on disable API

- QoL Changes

  - Improve path selector.
  - New install and update button UI.
  - Ability to register mod as not from modlinks by right clicking it.
  - When updating, disable use of Install/Enable/Update buttons

- Credits
  - Original project: https://github.com/TheMulhima/Needlelight
  - [Dastan21](https://github.com/Dastan21) - French Translation.
  - [luiz_eldorado](https://github.com/luizeldorado) - Portuguese translations.
  - [Clazex](https://github.com/Clazex) - Chinese translations.

## Changes in v2.0.0.0

- Additional Features

  - Improved search options (Filter by tags, by authors, search for dependents).
  - An offline mode.
  - Filter for newly released/updated mods.
  - Ability to pin mods. (by @JacksonFaller)
  - Manually install button.
  - Auto uninstall unused dependencies. (by @JacksonFaller)
  - Show all mods that are installed, even if they are manually installed.
  - Adds a button to open any mod's global settings file.
  - Ability to load Scarab's mod list from custom modlinks.
  - Lists authors of mods.
  - A settings tab for scarab's settings.

- Bug Fixes

  - Fixes the bug where scarab sometimes didn't download a mod or update its dependencies properly.
  - Update on version difference instead of only on lower version.
  - Fixed bug where Scarab showed a modded install as vanilla. Instead now if that happens and scarab can't fix the issue by itself, it prompts to verify integrity.
  - Ensures the config file can be written to before starting app.
  - Makes sure that the enabled states of mod actually match if they are in disabled folder or not.
  - Better handling for MS Store version of the game.
  - Checks dependencies are installed when toggling on a mod. (by @JacksonFaller)
  - Fixed Scarab showing blank screen on initial load.

- QoL Changes

  - More detailed errors.
  - Mod details are more compact.
  - Better UI for repository link.
  - Indication of what mod filter is on.
  - Button to clear search.
  - Button to open saves folder.
  - Allows clickable links in descriptions of mods.
  - Adds more bulk actions (Disable All, Uninstall All, Update All, Enable All).

- Credits
  - Original project: https://github.com/TheMulhima/Needlelight
  - [Clazex](https://github.com/Clazex) - Chinese translations and compatibility
  - [JacksonFaller](https://github.com/JacksonFaller) - Pinning Mods, Auto uninstall unused dependencies, check dependencies are installed when toggling on a mod.
  - [Italy](https://github.com/jngo102) - Tabs
  - [luiz_eldorado](https://github.com/luizeldorado) - Portuguese translations
