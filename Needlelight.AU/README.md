# LumaSong (Needlelight) Auto Updater

This is the auto updater for the Needlelight/LumaSong app (multi‑game mod manager for Hollow Knight and Silksong).

## How it works

The AU has Needlelight.exe in it as an embedded resource. When it is opened, it places the embedded exe into the same folder.
and opens it. If required it deleted the old exe before placing the new one.

## How to build

Make sure to place a Needlelight.exe (from dotnet publish of a windows version) next to the csproj and build it. There are no external dependencies

