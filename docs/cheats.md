# Cheats

Open **Cheats**, directly below **Overview** in the sidebar. The new character cheats and world controls require single-player or the host of an active multiplayer run. Controls start off.

Click a switch to enable it. Use **− / +** to change quantities or multipliers. **Numpad 1** returns to Overview. Existing keybinds for moved controls still work, and their labels stay synchronized.

| Group | Controls |
| --- | --- |
| Resources | Give Money; Give Lunar Coins; Give Experience; Gold Multiplier |
| Survival | God Mode; Unlimited Shields; No Fall Damage |
| Movement | Flight; Speed Multiplier; Unlimited Jumps |
| Combat and abilities | Aimbot; Infinite Skills; One-Hit Kills; Fast Attack Speed; Max Critical; Proc Chaos; Freeze Enemies |
| Stages and teleporter | Stage Abundance; Chest Paradise; Instant Teleporter Charging |
| Shopping and chest loot | Free Shopping; Loot Multiplier; Rarity Control |
| Session | Turn Off All Cheats |

## New controls

| Control | Behavior |
| --- | --- |
| Unlimited Shields | Continuously refills existing shields. Characters without native shields receive a shield equal to maximum health while enabled. The refill runs on menu updates; shield-bypassing damage and hits exceeding the shield can still damage health. God Mode remains available for invulnerability. |
| No Fall Damage | Enables the character's fall-immunity flag. Restores the previous flag when disabled and preserves protection required by Flight or Jump Pack. |
| Speed Multiplier | **x1.5–x10**, default x2. Scales the game's calculated movement speed, including normal sprint and item bonuses. Increased movement speed is capped at 100; higher existing values are preserved. |
| Unlimited Jumps | Resets the air-jump counter so you can jump again in midair. Press Jump again for each jump. Restores the normal jump limit when disabled. |
| Gold Multiplier | **x2–x10**, default x2. Adds a bounded bonus to native gold-collection events for your local character master. Spending is unchanged. Give Money grants its exact selected quantity. Gold bonuses saturate at the currency limit and do not multiply themselves. Disabling retains gold already collected. |
| One-Hit Kills | Your positive damaging hits queue a finish for living Monster, Lunar and Void targets through normal death handling. Finishes run after the damage callback returns, with at most 128 queued targets and 32 finishes per update. Excludes your body, allies, neutrals, other attackers and blocked/zero-damage hits. Disabling cancels pending finishes. |
| Fast Attack Speed | **x2–x20**, default x5. Scales calculated attack speed, retaining item/stat bonuses. Cheat increases are capped at 100 attack speed; higher existing values are preserved. |
| Max Critical | Sets calculated critical chance to at least **100%**, retaining native values above 100%. Attacks still follow their normal rules for using critical chance. |

Speed, attack speed, critical chance, shield capacity and jump limits are reapplied after native stat calculations. Multipliers use fresh native values instead of multiplying their previous result. Current Player → Stats modifiers remain compatible.

## Existing controls

The Resources controls, God Mode, Aimbot, Infinite Skills, Flight and Instant Teleporter Charging have moved here from their previous pages. Player retains character, buff, stats, respawn and unlock tools. Movement retains Always Sprint and Jump Pack. Teleporter retains stage skipping, Mountain Shrine stacks and portals.

All seven former World Cheats controls are on this page. [World control behavior and limits](world-cheats.md) describes abundance, extra chest placement, purchases, multi-drops, rarity, luck and freezing.

Stage Abundance and Chest Paradise affect the **next generated stage**. Instant Teleporter Charging sets the current teleporter's base charge duration to **one second**, or supported holdout zones when no teleporter is present. Disabling through the session reset restores its original duration while preserving subsequent changes by another system.

Closing the Cheats page keeps enabled controls active. **Turn Off All Cheats** disables the switches on this page and restores owned reversible changes, including prices, unopened chest state, stat values, fall protection, luck and charge durations. It cannot remove extra chests, collected loot, previously granted resources or elapsed charging. Other pages' controls, such as Infinite Equipment and Player stat modifiers, remain independent.

Character cheats reset on run end and unload; stage transitions release old body/master references while retaining enabled options within the run. Native event subscriptions and owned world assets are released on unload. A managed error pauses the affected cheat module and reports a status.

## Validation

The code builds against the installed Hallowed Concepts game libraries. Automated checks cover natural stat changes, non-compounding values, body/master replacement, shield refill, flag ownership, air jumps, gold recursion and overflow, manual grants, hostile hit filtering, moved controls, sidebar/back navigation, authority loss, failures and cleanup. Live Unity gameplay and multiplayer require an in-game acceptance pass.
