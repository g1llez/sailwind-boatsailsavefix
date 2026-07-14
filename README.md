# Boat Sail Save Fix

Fixes a **vanilla Sailwind bug** that drops custom sails from **owned boats left in the world** when you sail on another vessel (anchored, moored, or at port).

Affects boats with modular parts and multiple mast configurations (often reported on the **sanbuq**).

## The bug

On save (manual save, autosave, sleep), `SaveableBoatCustomization.GetData()` only serializes sails on masts where `gameObject.activeInHierarchy` is true. Inactive masts can still hold sails in memory. The save file then stores an empty or incomplete sail list. After reload, those boats spawn without sails.

Leaving and returning **in the same session without saving** does not trigger this code path; the problem appears after **autosave** (default: every 15 minutes) and the next load.

## What this mod does

1. **On save** — also writes sails from inactive masts (same data the game should have saved).
2. **On load** — keeps an in-session cache per boat `sceneIndex`. If a save file already has zero sails, restores the last known good rig from cache (safety net only).

## Install

1. Install [BepInEx](https://thunderstore.io/c/sailwind/p/BepInEx/BepInExPack/) for Sailwind.
2. Extract this folder into `BepInEx/plugins/`.
3. Launch the game. Check `BepInEx/LogOutput.log` for: `Boat Sail Save Fix 1.0.0 loaded`.

## Already corrupted saves

This mod **prevents new data loss**. It does not edit save files on disk. For a save that already has no sails stored, either:

- Load once after restoring sails with a save editor / patch tool, then play with this mod enabled, or
- Load a backup from before the bug occurred.

After one successful load with sails present, the session cache can cover empty entries until you fix the file.

## Compatibility

- **BepInEx 5.4+**
- Tested on game versions **0.38**
- No config file required

## Uninstall

Delete the `BoatSailSaveFix` folder from `BepInEx/plugins/`.

## Building from source

From the repository root:

```powershell
dotnet build -c Release -p:SailwindDir="$env:SAILWIND_DIR"
```

Set `SAILWIND_DIR` to your Sailwind folder. The standard Steam location is
`%ProgramFiles(x86)%\Steam\steamapps\common\Sailwind`.

Copy `bin/Release/netstandard2.0/BoatSailSaveFix.dll` into `BepInEx/plugins/BoatSailSaveFix/`.

## Thunderstore

1. Ensure `icon.png` (256×256) is at the repository root.
2. `.\build-thunderstore.ps1`
3. Upload `g1llez-BoatSailSaveFix-1.0.0.zip` on [Thunderstore](https://thunderstore.io/package/create/) (community: Sailwind).

## Links

- Source: [github.com/g1llez/sailwind-boatsailsavefix](https://github.com/g1llez/sailwind-boatsailsavefix)
- License: [MIT](https://github.com/g1llez/sailwind-boatsailsavefix/blob/main/LICENSE)

Upstream fix in Sailwind is welcome — this mod can be retired when the vanilla bug is patched.
