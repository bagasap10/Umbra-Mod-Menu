# World controls in Cheats

These controls now share the [Cheats page](cheats.md) with character, movement and resource controls.

Open **Cheats**, directly below Overview, during a run. These controls require the host (including single-player). All cheats start off. They stay enabled when moving between stages in the same run, and reset when the run ends, settings are reset, or the menu unloads. Leaving this page keeps the selected cheats active.

Click a switch to toggle it; use **− / +** to adjust its value. **Numpad 1** returns to Overview. **Turn Off All Cheats** restores the changes that can still be restored.

| Control | Behavior |
| --- | --- |
| Stage Abundance | x2–x5 interactable credits before the next stage populates. Covers the stage's normal chests, shrines, drones and other interactables. Does not change monster credits. Increased credits are capped at 5,000; a larger pre-existing budget is preserved. |
| Chest Paradise | x2–x5 the number of chests that actually spawned on the next stage, subject to valid placement and at most 50 extra chests. Reuses that stage's chest spawn cards, including DLC cards. Queues at most two placement attempts every 0.2 seconds. |
| Free Shopping | Sets money-priced purchases to zero while enabled, including money-priced shops and shrines. Health, lunar-coin, item and other payment types keep their rules. Restores the latest observed original price when disabled. |
| Loot Multiplier | x2–x5 the native drop count of unopened chests, including equipment chests. Added loot is capped at 20 drops per chest; a higher pre-existing native count is preserved. Uses the game's normal droplet/orb handling. |
| Rarity Control | Cycle Natural, Common, Uncommon, Legendary, Boss, Lunar, Void, Meal, Hallowed, Hex and Pages. Forces unopened **item** chests to roll from the selected tier's available run pool. Includes only visible, available items. If a pool is unavailable, retains the original loot and displays that status. Natural restores normal loot rules. |
| Proc Chaos | Adds +5–+20 luck to your local character master, default +10. This increases chances for item effects that use the game's luck-based rolls; it does not guarantee every proc or affect luck-independent effects. Existing item luck is retained and refreshed when it changes. |
| Freeze Enemies | Renews short stuns for living Monster, Lunar and Void bodies with a supported hurt-state controller, including stun-immune bosses. Stops movement and interrupts attacks while allies and neutrals keep moving. Disabling stops renewal; the game finishes remaining stun duration. Existing projectiles, damage-over-time effects and global time continue normally. |

Stage Abundance and Chest Paradise apply when a future stage is generated. A larger interactable budget is not an exact chest-count multiplier. Chest Paradise uses the population after Stage Abundance, so both can be combined.

Free Shopping, Loot Multiplier, Rarity Control, Proc Chaos and Freeze Enemies apply during the current run. New objects are discovered automatically. Free Shopping and chest changes can take up to roughly half a second to appear after an object initializes. Already-opened chests are not rerolled.

Rarity Control rerolls unopened item chests and supplies all their multiplied drops from the selected rarity. Its owned drop table disables random pickup replacement that could change the chosen rarity. It preserves the chest's storage type and normal pickup spawning. Changing or disabling it restores the original table and unopened pickup, while preserving a subsequent manual Change Chest Item override.

Turning cheats off cannot remove extra chests already spawned or items already picked up. Disabling Chest Paradise cancels pending placement. Closing a run or unloading releases prices, luck, unopened chest state, owned drop tables and event subscriptions. A managed feature error disables these cheats and displays a status instead of propagating into stage generation.

Work is bounded: up to 512 purchase snapshots and 256 chest snapshots per stage, 256 enemy bodies examined per update, scans at most twice per second, and cheat updates at most five times per second. Original spawn cards and original drop tables are never modified. No extra runtime DLL is needed.

## Validation

Automated checks exercise budgets, integer bounds, stage-card eligibility, placement failure/throttling, snapshot caps, independent toggles, price restoration, native drop counts, rarity/DLC availability, manual chest overrides, luck changes and master replacement, hostile filtering, immunity restoration, authority loss, failure handling, page navigation, run-end cleanup and unsubscribe behavior.

The code is compiled against the installed Hallowed Concepts game DLLs. Live gameplay and multiplayer still require an acceptance pass: enable the stage controls before changing stages; check chest placement, multi-drops, each available rarity, money-priced shops, luck procs and enemy recovery; then end the run and unload the menu.
