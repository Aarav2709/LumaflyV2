# LumaflyV2 (previously Lumafly)

LumaflyV2 is a refreshed, production-ready evolution of the original Lumafly installer. It focuses on first-class support for Hollow Knight: Silksong, improved stability when installing mods, and a cleaner, more maintainable codebase with better localization support.

Key improvements in LumaflyV2:

- Reworked game profile system: automatic detection and per-game executable resolution (better Silksong support).
- Safer installer flow with improved error handling around file access and mod installation.
- Better internationalization and updated translations.
- Small UX improvements: clearer messages, improved settings persistence, and fewer edge-case crashes.

Note: repository and CI targets remain on the original `Lumafly` GitHub repository for now; links and badges will be migrated to a dedicated `LumaflyV2` organization/repo during release. Badges currently point to the existing CI and release pages to remain functional; update them during official v2 rollout.

## Usage

- Download the latest version from the releases page.
- Default game = Hollow Knight. To manage Silksong, open Settings → Game → select "Silksong". The active profile changes immediately and persists to your config.
- Search through and download the mods you like.
- Mods appear in the top left corner of the game title screen after installation.
- Enable/Disable mods using the toggle and update outdated mods using the orange update button.
- If you are unable to connect to the internet, LumaSong can be launched in offline mode where you can toggle mods/API.

## Features

- Multi‑game profiles: Switch between Hollow Knight and Hollow Knight: Silksong in Settings.
- Automatically downloads the [Modding API](https://github.com/hk-modding/api) which is required for mods to load. It also allows switching between modded and vanilla via the Toggle API button.
- Search through the 300+ mods available in the [official modlinks](https://github.com/hk-modding/modlinks).
- Group mods in modpacks and share them using the commands.
- Display mods that were recently updated or released.
- A single place to install, update, configure, view readmes, and report issues for mods.
- Manage mods not available through modlinks via the manual install button.

### Silksong profile details

- Steam App ID: 1030300
- Executables:
  - Windows: "Hollow Knight Silksong.exe"
  - Linux: "Hollow Knight Silksong"
  - macOS: "Hollow Knight Silksong.app"
- Data folder: "Hollow Knight Silksong_Data"
- Save paths (Unity defaults):
  - Windows: %USERPROFILE%/AppData/LocalLow/Team Cherry/Silksong
  - macOS: ~/Library/Application Support/unity.Team Cherry.Silksong
  - Linux: ~/.config/unity3d/Team Cherry/Silksong

## Screenshot: What LumaSong looks like

<!-- screenshots removed (previously referenced original repo's static resources) -->

Screenshots will be updated to showcase the Game dropdown and Silksong profile. [TODO]

## Migration notes

- Older configs continue to load with Hollow Knight as the default (`game` missing or set to `hollow_knight`).
- When you choose Silksong in Settings, the config persists `silksong` and will restore that on next launch.

## Contributions

- If you want to suggest a feature or report a bug, report it on the issues page.
- If you want to contribute, feel free to. You can see what features are currently requested over here.
  If you want to contribute to localizations, please use Crowdin to add the translations.

## Credits

- Source code adapted from https://github.com/TheMulhima/Lumafly

- Programming

  - [56](https://github.com/fifty-six) - Creator of [Scarab](https://github.com/fifty-six/Scarab), on which Lumafly is based
  - [JacksonFaller](https://github.com/JacksonFaller), [Italy](https://github.com/jngo102), and [Acu1000](https://github.com/Acu1000)

- Translations

  - [Clazex](https://github.com/Clazex) - Chinese translations
  - [luiz_eldorado](https://github.com/luizeldorado) - Portuguese translations
  - [Dastan](https://github.com/Dastan21) - French translations
  - [Adrin](https://twitter.com/Adrin63_?t=lbzYGgt-3Zybjb_S2xqt2A&s=09) and [Helen](https://ko-fi.com/helensb) - Spanish translations
  - [Страг](https://discordapp.com/users/274945280775028736) - Russian translations
  - [Acu1000](https://github.com/Acu1000) - Polish translations
  - [Sawayoshi](https://twittter.com/sawayoshiyt) - Japanese translations
  - [Thommie](https://discordapp.com/users/454185487641608193) - Dutch translations

- Art
  - [Dwarfwoot](https://patreon.com/DwarfWoot), [SFGrenade](https://github.com/SFGrenade) - Images and icons used in the installer.
  - [Lime](https://www.tumblr.com/ded-lime) - The Lumafly banner.
  - [HBKit](https://ko-fi.com/hbkit) - The Lumafly icon.
