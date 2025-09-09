# LumaflyV2 v5.1.0.0 Release Notes!

## Overview

This release upgrades the app to **.NET 9.0**, finalizes the rename to **LumaflyV2** across the executable and window title, ensures the Windows app **icon** is applied correctly, and updates CI/build scripts accordingly. Functionality remains the same as v5.0.0.0 with performance and longevity improvements from the runtime update.

## Major Features

- **Runtime Upgrade to .NET 9.0**
  Faster startup and improved platform support with the latest .NET runtime.
- **Branding & Executable Name**
  App binary and metadata are now consistently named: `LumaflyV2` (`LumaflyV2.exe` on Windows). Window title and product metadata updated.
- **Windows Icon Restored**
  Valid application icon wired via `Assets/Lumafly.ico`.
- **CI/Build Updates**
  Workflows and scripts now target `net9.0` for Windows, Linux, and macOS.

## Fixes & Improvements

- Auto‑Updater build resilience: embedding of `LumaflyV2.exe` is now conditioned, preventing local build failures; CI supplies the binary during release.
- Removed legacy `net7.0` targeting; projects now target `net9.0` only.
- Polished docs and metadata to reflect the new name and runtime.

## Developer Notes

- Version: `5.1.0.0` (set in `Lumafly/Lumafly.csproj`).
- Target Framework: `net9.0` for app, tests, and AU. Requires .NET SDK 9.0.x.
- Executable: `LumaflyV2` (`.exe` on Windows).
- Icon: `Assets/Lumafly.ico` via `<ApplicationIcon>` in the csproj.
- CI: GitHub Actions workflow now triggers on version tags (e.g. `v5.1.0.0`) and publishes single‑file, self‑contained artifacts per OS.

## Upgrade Notes

- No data migration required from v5.0.0.0; settings are preserved.
- If you have shortcuts/scripts pointing to `Lumafly.exe`, update them to `LumaflyV2.exe`.
- Download the platform zip from the release page or use the standalone `LumaflyV2.exe` on Windows.
