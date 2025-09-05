# LumaSong (previously Lumafly)

![build](https://github.com/TheMulhima/Lumafly/actions/workflows/build.yml/badge.svg)
![test](https://github.com/TheMulhima/Lumafly/actions/workflows/test.yml/badge.svg)
[![website](https://img.shields.io/website?down_color=red&down_message=offline&up_color=32c854&up_message=online&url=https%3A%2F%2Fthemulhima.github.io%2FLumafly)](https://themulhima.github.io/Lumafly)
[![GitHub all releases](https://img.shields.io/github/downloads/TheMulhima/Lumafly/total)](https://github.com/TheMulhima/Lumafly/releases)
[![contributors](https://img.shields.io/github/contributors/TheMulhima/Lumafly)](https://github.com/TheMulhima/Lumafly/graphs/contributors)
[![Crowdin](https://badges.crowdin.net/lumafly/localized.svg)](https://crowdin.com/project/lumafly)
[![discord](https://img.shields.io/discord/879125729936298015?label=discord)](https://discord.gg/VDsg3HmWuB)

Cross‑platform mod manager for [Hollow Knight](https://www.hollowknight.com) and Hollow Knight: Silksong. Fully localized in English, Spanish, Portuguese, French, Chinese, Japanese, Russian, Dutch, Polish, and more.

Note: The application and repository are still named "Lumafly". The LumaSong brand emphasizes first‑class Silksong support while remaining fully backward compatible with the Lumafly name.

Formerly known as **Scarab+**

## Usage

- Download the latest version from the [download link](https://themulhima.github.io/Lumafly?download) (or manually from the [releases page](https://github.com/TheMulhima/Lumafly/releases/latest)).
- Default game = Hollow Knight. To manage Silksong, open Settings → Game → select "Silksong". The active profile changes immediately and persists to your config.
- Search through and download the mods you like.
- Mods appear in the top left corner of the game title screen after installation.
- Enable/Disable mods using the toggle and update outdated mods using the orange update button.
- If you are unable to connect to the internet, LumaSong can be launched in offline mode where you can toggle mods/API.

## Features

- Multi‑game profiles: Switch between Hollow Knight and Hollow Knight: Silksong in Settings.
- Automatically downloads the [Modding API](https://github.com/hk-modding/api) which is required for mods to load. It also allows switching between modded and vanilla via the Toggle API button.
- Search through the 300+ mods available in the [official modlinks](https://github.com/hk-modding/modlinks).
- Group mods in modpacks and share them using the [commands](https://github.com/TheMulhima/Lumafly/wiki/Commands).
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

![info](https://github.com/TheMulhima/Lumafly/blob/static-resources/Readme%20Assets/Info.png?raw=true)
![demo](https://github.com/TheMulhima/Lumafly/blob/static-resources/Readme%20Assets/ModList.png?raw=true)

Screenshots will be updated to showcase the Game dropdown and Silksong profile. [TODO]

## Migration notes

- Older configs continue to load with Hollow Knight as the default (`game` missing or set to `hollow_knight`).
- When you choose Silksong in Settings, the config persists `silksong` and will restore that on next launch.

## Contributions

- If you want to suggest a feature or report a bug, report it on the [issues page](https://github.com/TheMulhima/Lumafly/issues/new/choose).
- If you want to contribute to Lumafly, feel free to. You can see what features are currently requested over [here](https://github.com/TheMulhima/Lumafly/labels/enhancement)
- If you want to contribute to localizations, please use [crowdin](https://crowdin.com/project/lumafly) to add the translations.

<details>
<summary><h3>Credits</h3></summary>

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

</details>
