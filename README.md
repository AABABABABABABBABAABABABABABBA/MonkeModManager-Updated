# Monke Mod Manager (Updated)

An up-to-date version of [Monke Mod Manager](https://github.com/burritosoftware/MonkeModManager) for Gorilla Tag.
It installs **BepInEx** and mods into Gorilla Tag and can be re-run to update them.

## What's new

- **Always gets the newest BepInEx automatically.** On every launch it asks GitHub for the latest stable
  BepInEx 5 release (currently v5.4.23.5) and picks the right Windows x64 download by name, so it never
  goes stale. If GitHub's API is rate-limited it falls back to github.com, then to a known-good link.
- **Auto-update BepInEx.** If your installed BepInEx is older than the latest, it updates it on launch
  (you can turn this off with the checkbox). Your plugins and configs are kept.
- Shows installed vs. latest BepInEx version and which mods are already installed.
- Finds Gorilla Tag in **any Steam library** and in the **Meta/Oculus PC app**, and remembers the folder.
- Won't install while the game is running.
- Fixed the broken mod list (the old `mods.json` was invalid JSON and pointed at a dead repo).
  The list now lives in this repo, with a built-in copy for offline use.
- New utilities: open the plugins folder / BepInEx log, fully remove BepInEx, dated mod backups.
- Modern build: SDK-style project on .NET Framework 4.7.2 (built into Windows 10/11, nothing to install),
  plus a GitHub Actions build that produces `MonkeModManager.exe`.

## Download

A ready-to-run build is in this repo at [`Release/MonkeModManager.exe`](Release/MonkeModManager.exe).
Download it and run it. You can also get it from the [Releases](../../releases) page or the latest
[Actions](../../actions) build artifact. It asks for admin rights because Gorilla Tag
is usually installed under Program Files.

## Mod menu template

[`MonkeMenuTemplate/`](MonkeMenuTemplate) is a starter in-game mod menu (a BepInEx plugin) you can build your own
mod on top of. It has categories, pages, toggle/one-shot buttons and a few example mods, and gameplay
mods automatically switch off outside modded lobbies. See its [README](MonkeMenuTemplate/README.md).

## Adding / editing mods

Edit [`mods.json`](mods.json). Changes go live for everyone on their next launch — no rebuild needed.

| Field | Meaning |
| --- | --- |
| `name`, `author`, `group` | Shown in the list |
| `gitPath` | `owner/repo` on GitHub; the latest release is used |
| `assetPattern` | Regex picking which release file to download (recommended) |
| `releaseId` | Old style: index of the release file (used if no `assetPattern`) |
| `tagPrefix` | Only use releases whose tag starts with this (e.g. `v5.` for BepInEx) |
| `installPath` | Where to put the file, relative to the game folder |
| `dependencies` | Names of mods that get ticked automatically |
| `required` | Can't be unticked |
| `fallbackUrl`, `fallbackVersion` | Used if GitHub can't be reached |

Many of the original 2021 mods may no longer work with the current Gorilla Tag; update the list as needed.

## Building

```
dotnet build MonkeModManager.sln -c Release
```

Works on Windows, macOS or Linux (thanks to `Microsoft.NETFramework.ReferenceAssemblies`).
Output: `MonkeModManager/bin/Release/net472/MonkeModManager.exe`.

After changing the code, rebuild and copy the exe over `Release/MonkeModManager.exe` so the
committed build stays current.

To release: push a tag like `v2.0.0` and the workflow attaches the exe to a GitHub release.
Bump `<Version>` in `MonkeModManager.csproj` first so the in-app update check works.

## Credits

Based on Monke Mod Manager by DeadlyKitten / burritosoftware. BepInEx by the BepInEx Team.
