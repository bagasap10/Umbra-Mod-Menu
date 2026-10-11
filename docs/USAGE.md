# Run Umbra Suite

**Version 4.0.1 / Hallowed Concepts (DLC#4).** Suite versions use DLC count.feature update.fix numbering.

Extract the full release ZIP and double-click **UmbraInjector.exe**. The launcher does not need a separate injector installation, an arbitrary DLL picker, a manual PID, or a working directory set by a shortcut.

Use **Start game** to ask Steam to launch Risk of Rain 2, or start the game normally. The launcher checks every two seconds. If more than one game process exists, select the one to use. It enables **Inject menu** after the game runtime and matching package are available.

Click **Inject menu** once. The UI remains responsive while the helper performs native injection. The menu then waits for the game's catalogs and initializes on the main thread. The launcher confirms **Menu active** after the menu's actual startup, rather than reporting success merely because the DLL was submitted.

**Insert / Numpad 0** toggles the menu; **Numpad 1** returns to the previous page. Existing custom keybinds are retained and the active toggle key is shown after startup. Key capture, search entry and game chat keep their existing input guards. Drag a border/corner to resize; change Glass Opacity in Settings.

The Item Spawn, Remove Items, Equipment Spawn, and Change Chest Item pages have a search field above the list. Type part of the pickup name; matching ignores capitalization and surrounding spaces. **Clear** restores the list. Each page keeps its own query and matching rarity/DLC sections. **Enter / Escape** finishes typing so keyboard navigation can resume.

Change Chest Item locks the nearest unopened chest when the page opens. Its list switches automatically between items and equipment according to the selected chest, preserving the search query. Selecting a result changes that chest's drop. Close and reopen the page to select a different nearby chest. Empty scenes and camera transitions safely skip chest actions.

The Teleporter / Portals page includes embedded wiki thumbnails. A neutral ? icon marks a portal whose photograph is unavailable; its spawn action still works. Images work offline and need no additional image files. Image sources are listed in THIRD_PARTY_NOTICES.md.

Open **Spawn → World Cheats** for Stage Abundance, Chest Paradise, Free Shopping, Loot Multiplier, Rarity Control, Proc Chaos and Freeze Enemies. These controls require the host of an active run. Stage generation controls apply to future stages; the other controls apply during the run. Leaving the page keeps cheats enabled. Use **Turn Off All World Cheats** to release the effects; ending a run also resets them. Extra chests and loot already collected remain in the game.

## Troubleshooting

| Status | Action |
| --- | --- |
| Waiting for game | Start Risk of Rain 2. |
| Game is starting / catalogs loading | Wait for loading to complete; the launcher updates automatically. |
| More than one game | Select the desired PID in the dropdown. |
| Missing or mismatched release file | Extract a fresh complete release ZIP; do not mix DLLs from different releases. |
| Different game update | Rebuild with `scripts/Build.ps1 -GameDir <your installation>` or use a release built for that update. |
| Game access unavailable | Run the game and launcher at the same privilege level. Normal non-administrator launch is the default. |
| Menu already active | Use the displayed toggle key; the injector prevents a second copy. |
| Different Umbra build already loaded | Restart the game before injecting the new build. |
| Pending / uncertain injection / helper timeout | Wait for the menu. If it never starts, restart the game before another attempt. |
| A feature was paused | Inspect the game's `Player.log`; the failing feature stops retrying after three consecutive errors. A scene change resets routine guards; unload/reload or restart if needed. |

The launcher leaves the game running on a timeout. Do not delete session markers to force retries: a timed-out native thread may still be executing. A fresh game process naturally has a fresh identity. Clean menu unload allows reinjecting the **same build**; changing builds always requires a game restart.

**Open logs** opens `%LOCALAPPDATA%\UmbraSuite\Logs`. Launcher logs rotate near 1 MB with one previous file. Session markers live in `%LOCALAPPDATA%\UmbraSuite\Sessions` and include PID plus process start time to avoid PID reuse. Existing game/menu settings remain in the menu's previous AppData location; neither settings nor logs are bundled.

Bulk item operations limit a single grant quantity to 1,000 per item and a single roll to 500 items to bound work from one click. Individual integer stack overflow is rejected. Item packs resolve and validate every ingredient before granting any. These limits do not change existing inventory or recipe quantities.

For release acceptance, start a fresh game, inject once at the title screen, start a run, test item/search/pack/removal actions, toggle ESP, travel to another stage, return to the title screen, unload, and reinject the same build. Test Hallowed portals and interactables on the host of an active run. Test the game update and missing-file messages separately. Automated source tests cover the modeled behaviors, while real native injection and Unity gameplay need this live pass.
