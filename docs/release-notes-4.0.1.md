# Umbra Suite 4.0.1 — Hallowed Concepts

Changes since **4.0.0**: a new World Cheats submenu, portal images, faster equipment and chest searches, and corrected item packs.

## New: World Cheats

Open **Spawn → World Cheats** during a run.

| Control | What it does |
| --- | --- |
| **Stage Abundance** | Increases the next stage's interactable budget by **x2–x5**, allowing more chests, shrines, drones and other interactables. |
| **Chest Paradise** | Targets **x2–x5** the normally generated chest count on the next stage, subject to placement, with at most **50 extra chests**. Supports the stage's DLC chest types. |
| **Free Shopping** | Makes **money-priced** purchases free. Lunar-coin, health and item payments retain their normal rules. |
| **Loot Multiplier** | Multiplies unopened chest drops by **x2–x5**, including equipment chests. Increased drop counts are capped at **20 per chest**; higher existing counts are preserved. |
| **Rarity Control** | Chooses **Natural, Common, Uncommon, Legendary, Boss, Lunar, Void, Meal, Hallowed, Hex or Pages** for unopened item chests. Uses items available in the current run. |
| **Proc Chaos** | Adds **+5–+20 luck** to your character for luck-based item effects. Preserves existing item luck; does not guarantee every proc. |
| **Freeze Enemies** | Stops hostile movement and interrupts attacks through renewed short stuns, including supported bosses. Allies and neutral characters remain active. |

- All controls start **OFF** and require **single-player or host authority**.
- **Stage Abundance** and **Chest Paradise** take effect when the **next stage** is generated.
- Closing the submenu keeps enabled cheats active. **Turn Off All World Cheats** disables them and restores reversible changes; ending a run also resets them.
- Extra chests and already-collected loot remain after disabling cheats.
- Rarity Control leaves original loot intact when the selected tier has no available items. Choose **Natural** to restore normal loot rules.
- Disabling Freeze Enemies lets remaining stuns expire. Existing projectiles and damage-over-time effects continue normally.

See [World Cheats controls and limits](world-cheats.md).

## Portal images

- Added **14 portal thumbnails**, matched to their portal entries using wiki images.
- Portals without verified images display a neutral **?** placeholder and remain selectable.
- Images are embedded in the menu DLL and work offline. No separate image folder is required.
- Existing base-game and DLC portal sections are retained.

See [portal image sources](portal-icons.md).

## Search and chest selection

- Added search to **Equipment Spawn** and **Change Chest Item**.
- Search accepts partial names, ignores capitalization, and includes a **Clear** action.
- Results retain item icons and rarity/DLC grouping.
- Change Chest Item selects the nearest unopened chest when opened and shows items or equipment according to that chest. Reopen the page to select another chest.
- Added guards for missing chests, invalid targets and camera/scene transitions.

## Item-pack fixes

Corrected the internal item reference so these packs give **Repulsion Armor Plate** instead of Shattering Justice:

- **Bungus:** 1 Repulsion Armor Plate.
- **Seeker's Flying Object:** 1 Repulsion Armor Plate.
- **Masochist:** 5 Repulsion Armor Plates.

## Stability and validation

World Cheats use bounded scan rates, staged chest placement, capped snapshots and non-compounding values. Cleanup restores owned prices, luck and unopened chest changes, releases owned drop tables, and removes event subscriptions on unload. A managed World Cheats failure disables its controls and reports a status.

The release compiled against the installed Hallowed Concepts game DLLs with warnings treated as errors. **1,359 automated assertions passed**, covering gameplay models, UI, runtime lifecycle and injector checks. These checks do not inject a running game; live gameplay and multiplayer still need verification.

## Installation and version

1. **Restart Risk of Rain 2** before switching to this build.
2. Extract the complete **Umbra-Suite-4.0.1-win-x64.zip** into one folder.
3. Run **UmbraInjector.exe**, start the game, then click **Inject menu** when ready.
4. Open the menu with **Insert / Numpad 0**. Use **Numpad 1** to return to the previous page.

Keep the executable, configuration, menu DLL, injection backend and bundle manifest together. Existing custom keybinds are preserved.

The menu and injector now use **4.0.1**. Suite numbering follows **DLC count.feature update.fix**, with **4** identifying **Hallowed Concepts** support.

