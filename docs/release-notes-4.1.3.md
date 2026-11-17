# Umbra Suite 4.1.3 - Hallowed Concepts

Settings and Visuals now work at the game's title screen and in the lobby, before a player character exists.

- The overview and sidebar allow **Visuals**, **Settings** and the **Keybinds** subpage without starting a run.
- Window size, glass opacity, keyboard navigation, configured God type, keybind editing, menu reload and settings reset are available there.
- Mouse clicks, Numpad selection/adjustment, Back and configured visual shortcuts work outside a run. Lobby chat and key capture suppress shortcuts while typing.
- Visual switches retain their choices through scene changes and menu rebuilds in the current menu session. ESP target collection and drawing wait for gameplay.
- Gameplay pages and their keyboard actions still require an active player. Losing the player closes a gameplay page while allowing Settings or Visuals to stay selected.
- Startup constructs these pages safely before the complete menu registry is assigned.

Restart the game before switching builds, extract **Umbra-Suite-4.1.3-win-x64.zip**, and run **UmbraInjector.exe**. Open the menu with **Insert / Numpad 0**. Existing custom keybinds and saved menu settings are retained.

The release includes **Umbra-Suite-4.1.3-source.zip**, containing clean buildable source without generated binaries, game assemblies, Git metadata, logs or personal settings. The 4.1.2 advisory game-version warning remains in place.

Automated checks exercise the production UI, Settings/Visuals/Keybinds controls and extracted input routines against stand-ins. Live Unity interaction and native injection still require an in-game pass.

See the [4.1.2 release notes](release-notes-4.1.2.md) for the version-warning change and the [4.1.1 notes](release-notes-4.1.1.md) for Cheats features.
