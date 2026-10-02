# Monke Menu Template

A starter mod menu for Gorilla Tag (BepInEx 5) that you can turn into your own mod.
The menu sits on your left hand and you press its buttons with your right hand.

The layout (button list, categories, pages) is inspired by iiDk's Gorilla Tag menu template,
but all of the code here is new.

## Playing fair

Gorilla Tag only allows mods in **private / modded lobbies**. Every button has a `ModdedOnly` setting
(on by default). Mods with it on only run offline or in a modded lobby, and **turn themselves off
automatically** when you join a normal public lobby. Keep it on for anything that changes gameplay.
That's what keeps your menu from ruining public games or getting your account banned.

This template does **not** try to hide from anti-cheat, and you shouldn't add things that affect
other players in public lobbies.

## Controls

| Action | Button |
| --- | --- |
| Open menu | Hold **Y** (left controller) |
| Press a button | Touch it with the white dot on your right hand |
| Change page | `<` / `>` at the bottom |
| Back to main page | `Back` |

The example mods are: Fly (hold **A**), Platforms (hold **grip**), Low Gravity and Show FPS.

## Setup

1. Install BepInEx into Gorilla Tag (the Monke Mod Manager in this repo does it for you) and run the game once.
2. Install the [.NET SDK](https://dotnet.microsoft.com/download) (8 or newer).
3. Build:
   ```
   cd MonkeMenuTemplate
   dotnet build -c Release
   ```
   If your game isn't in `C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag`, add
   `-p:GamePath="D:\path\to\Gorilla Tag"` (or change `GamePath` in `MonkeMenuTemplate.csproj`).
4. The DLL is copied to `Gorilla Tag\BepInEx\plugins\MonkeMenu\MonkeMenu.dll` automatically. Start the game.

The project references the game's own DLLs, so it only builds on a PC with Gorilla Tag installed.

## Making it yours

1. **Rename it.** Change `Guid`, `Name` and `Version` in `Plugin.cs` (`PluginInfo`), and
   `AssemblyName` in `MonkeMenuTemplate.csproj`.
2. **Add mods.** Write a method in `Mods/`, then add a button for it in `Menu/Buttons.cs`:
   ```csharp
   new ButtonInfo("Super Jump", "Jump really high")
   {
       OnUpdate = () => { if (Inputs.RightPrimary) Inputs.Body.velocity += Vector3.up * 0.5f; }
   },
   ```
   - `OnEnable`: runs once when switched on (or every press for `IsToggle = false` buttons)
   - `OnUpdate`: runs every frame while on
   - `OnDisable`: runs once when switched off; clean up anything you created here
3. **Add a category.** Add a `new Category("Name", ...)` to `Buttons.Categories` and a `Link("Name", index)`
   on the main page.
4. **Restyle it.** Colours, size, how many buttons per page, and where the menu sits on your hand are all
   in `Menu/MenuSettings.cs`. If the menu shows up at a weird angle, tweak `MenuOffset` / `MenuRotation`.

## Files

| File | What it is |
| --- | --- |
| `Plugin.cs` | BepInEx entry point + your mod's name/GUID |
| `Menu/Buttons.cs` | **The button list: edit this** |
| `Menu/MenuSettings.cs` | Look and feel |
| `Menu/MenuManager.cs` | Draws the menu, handles presses, runs enabled mods |
| `Mods/*.cs` | Example mods |
| `Utilities/Inputs.cs` | Controllers, hands, head, body. **If a game update breaks the build, fix it here.** |
| `Utilities/LobbyCheck.cs` | Detects modded lobbies |
| `Utilities/Draw.cs` | Makes cubes/text that render correctly in Gorilla Tag |
| `Utilities/Notifications.cs` | Pop-up messages in front of your face |

## If a Gorilla Tag update breaks it

Game updates sometimes rename things like `GorillaTagger`, `ControllerInputPoller` or `NetworkSystem`.
Nearly all game-specific code is in `Utilities/Inputs.cs` and `Utilities/LobbyCheck.cs`, so fix those
two files and everything else keeps working.
