# Umbra Suite

Source and release tooling for the **Umbra Menu** and a new **Umbra Injector** launcher for Risk of Rain 2, including Hallowed Concepts. This directory is independent of the older menu checkout and binary-only injector.

## Use the release

1. Extract the entire `Umbra-Suite-3.0.0-win-x64.zip` into one folder.
2. Run `UmbraInjector.exe`. Start the game yourself, or use **Start game** in the launcher.
3. The launcher finds the game automatically. Click **Inject menu** once it shows ready.
4. Wait for **Menu active**. Open/close the menu with **Insert** or **Numpad 0**; **Numpad 1** goes back.

Keep `UmbraMenu.dll`, `SharpMonoInjector.dll`, `bundle.xml` and the executable/config together. You do not need the old injector's `Data` folder. A complete release includes every redistributable runtime dependency. Windows must have .NET Framework 4.8 installed; game-owned Unity/RoR2 libraries come from the installed game and are deliberately not redistributed.

Existing menu settings/keybinds are preserved. After startup the launcher reports the actual configured toggle key. Restart the game before switching menu builds. See [usage and troubleshooting](docs/USAGE.md).

## Build and test

Windows x64, Visual Studio 2022 or Build Tools with **.NET desktop development** and the **.NET Framework 4.8 targeting pack**, and a current installed copy of Risk of Rain 2 are required. No NuGet download is required.

```powershell
# From this directory; -GameDir is optional when Steam library discovery finds the game.
.\scripts\Build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Risk of Rain 2'
.\scripts\Test.ps1

# Rebuild, test, and generate both release and clean source ZIPs.
.\scripts\Package.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Risk of Rain 2'
```

The solution is `Umbra-Suite.sln`. For Visual Studio, supply `/p:GameDir=...` when building with MSBuild, or set the `GameDir` environment variable before opening the solution. Game references use `Private=false`; DLLs are read from your installation and never copied into the package.

Outputs:

- `artifacts/release/`: ready-to-run injector, menu, backend, compatibility manifest and licenses.
- `artifacts/Umbra-Suite-3.0.0-win-x64.zip`: release ZIP for GitHub Releases.
- `artifacts/Umbra-Suite-3.0.0-source.zip`: clean source ZIP, including all buildable dependency source.
- `artifacts/SHA256SUMS.txt`: package checksums.
- `artifacts/tests/previews/`: off-screen launcher/menu renders produced by the checks.

## Publish a clean GitHub copy

Use **this directory as the repository root**, or extract the source ZIP into a fresh directory and publish its `Umbra-Suite` contents. Source staging uses an explicit allowlist and excludes `bin`, `obj`, game DLLs, executables, personal settings, logs and build artifacts. The supplied `.gitignore` covers generated files. Upload the release ZIP separately through GitHub Releases.

The source includes the MIT-licensed injection backend pinned to an upstream commit; there is no dependency on an opaque old injector executable. Preserve both license notices. See [third-party notices](THIRD_PARTY_NOTICES.md).

## What changed

The launcher detects the correct game process, validates bundle hashes and game compatibility, keeps native injection in a separate helper, and waits for a startup marker written by the menu. A durable per-process attempt gate prevents duplicate injection and unsafe retries after timeouts. A game update requires a freshly built matching package.

Menu startup waits for catalog availability and creates Unity objects on the game's main thread. Unload no longer loops around deferred `Destroy`. Owned textures and Addressables handles are released; icon loading has a 128-entry cache; enemy collection reuses a list at 10 Hz; ESP draws only during repaint; idle stat modifiers avoid repeated recalculation. Managed feature failures back off and pause after repeated errors instead of flooding each frame.

The native backend releases reserved pages with `MEM_RELEASE`, closes thread/process handles on failure, bounds memory reads and allocations, terminates UTF-8 strings correctly, reads 64-bit exception pointers correctly, and uses a finite remote-thread wait. On an uncertain timeout it intentionally preserves potentially executing pages until the game exits.

All existing rarity/DLC sections, catalog icons, item/removal search, item packs, portals, ESP and resize/back-key features are retained. See [item packs](docs/item-packs.md) and [architecture and validation](docs/ARCHITECTURE.md).

Automated checks do not inject a running game. Native injection and gameplay must still be tested in-game; these changes reduce identified risks and cannot guarantee a crash-free game session.

## License

The menu retains its original MIT license. Vendored SharpMonoInjector uses its own MIT notice. Game libraries, assets and names remain their respective owners' property and are not included in source or release packages.
