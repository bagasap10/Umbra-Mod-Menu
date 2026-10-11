# Umbra Suite 4.1.1 — Hallowed Concepts

This update adds eight cheats and brings resource, survival, movement, combat and world controls together in one page.

## New Cheats section

**Cheats** now appears directly below **Overview** in the sidebar, grouped by mechanic:

- **Resources:** Give Money, Lunar Coins, Experience and Gold Multiplier.
- **Survival:** God Mode, Unlimited Shields and No Fall Damage.
- **Movement:** Flight, Speed Multiplier and Unlimited Jumps.
- **Combat and abilities:** Aimbot, Infinite Skills, One-Hit Kills, Fast Attack Speed, Max Critical, Proc Chaos and Freeze Enemies.
- **Stages and teleporter:** Stage Abundance, Chest Paradise and Instant Teleporter Charging.
- **Shopping and chest loot:** Free Shopping, Loot Multiplier and Rarity Control.

The previous World Cheats submenu has moved here. Existing resource, ability, Flight and instant-charging controls were removed from their old pages. Their existing keybinds are retained.

## Added

- **Unlimited Shields:** refills shields and supplies shield capacity to characters without native shields.
- **No Fall Damage:** reversible fall protection, shared correctly with Flight and Jump Pack.
- **Speed Multiplier:** x1.5–x10 calculated movement speed.
- **Unlimited Jumps:** renewable air jumps.
- **Gold Multiplier:** x2–x10 collected gold, with exact manual grants and overflow protection.
- **One-Hit Kills:** finishes hostile targets hit by your attacks, excluding allies and neutrals.
- **Fast Attack Speed:** x2–x20 calculated attack speed.
- **Max Critical:** at least 100% calculated critical chance.

## Reset and stability

**Turn Off All Cheats** stops all toggles on the Cheats page and restores reversible changes. Instant charging durations now restore on reset, stage change and unload. Already-earned gold, spawned chests and collected loot remain.

Stat multipliers apply after native calculations, preserve item bonuses, and do not compound each frame. New controls have finite limits, authority guards, body/master replacement handling and event cleanup. Gold bonuses do not recursively multiply themselves. Managed feature failures pause their module and keep Flight fall protection working.

## Installation

Restart the game, extract the full **Umbra-Suite-4.1.1-win-x64.zip**, and run **UmbraInjector.exe**. Inject when ready, then open the menu with **Insert / Numpad 0**. Custom toggle keys are preserved; **Numpad 1** goes back.

The menu, injector, bundle metadata and package names use **4.1.1**. The major version **4** continues to identify Hallowed Concepts support. No additional redistributable runtime DLL is required.

The game-library build succeeded with warnings treated as errors, and **1,432 automated assertions passed**. Automated checks do not inject a running game; live gameplay and multiplayer still need verification.

See [Cheats controls and limits](cheats.md). Earlier changes are listed in the [4.0.1 release notes](release-notes-4.0.1.md).
