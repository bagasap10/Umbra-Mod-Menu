# Items menu and packs

Open **Items → Item Packs**, search by pack or ingredient name, choose a pack to review the exact quantities, then click **Give Pack**. Each click adds a complete pack to the current player's permanent inventory. It does not use the Drop Items modes. A pack containing equipment replaces the active equipment slot; the preview says so before giving it. A missing catalog entry prevents the whole grant.

The list has separate **General packs** and **CHEF recipes** sections, with 21 packs in total. Search spans both sections; searching `CHEF` shows just the five recipes. CHEF packs grant the ingredients for cooking.

Each section sorts from lower to higher relative value: Common = 1 point, Uncommon/Lunar = 3, Legendary/Boss = 9, equipment = 6. Void items use their corresponding rarity. Item points are multiplied by quantity. These points are a sorting aid, not game currency or a ranking of build strength.

## General packs

| Pack | Contents |
| --- | --- |
| Starbucks | 10 Mocha |
| Arsonist | 10 Gasoline, 1 Ignition Tank |
| Shield Tank | 1 Transcendence, 5 Warped Echo, 5 Oddly-shaped Opal |
| Seeker's Flying Object | 1 Planula, 1 Repulsion Armor Plate, 1 Tougher Times, 1 Razorwire |
| 1 Health Shrimp | 1 Plasma Shrimp, 1 Transcendence, 1 Pocket I.C.B.M. |
| Free Red | 5 Regenerating Scrap |
| Primary Heresy | 1 57 Leaf Clover, 1 Purity, 1 Visions of Heresy |
| Crits | 10 Lens-Maker's Glasses, 1 Predatory Instincts, 1 Harvester's Scythe |
| Bungus | 5 Bustling Fungus, 1 Tougher Times, 1 Soldier's Syringe, 1 Aegis, 1 Leeching Seed, 1 Tri-Tip Dagger, 1 Repulsion Armor Plate |
| Drug Addict | 5 Gesture of the Drowned, 1 Spinel Tonic |
| Special Forces | 1 Goobo Jr., 5 Gesture of the Drowned |
| Remote Katyusha | 1 Remote Caffeinator, 1 Bundle of Fireworks, 1 Pocket I.C.B.M., 1 Squid Polyp, 1 AtG Missile Mk. 1 |
| DMG Stack | 1 Forgive Me Please, 1 Gesture of the Drowned, 1 Fuel Cell, 5 Chronic Expansion, 1 Soulbound Catalyst |
| Zeus | 1 Royal Capacitor, 5 Gesture of the Drowned, 5 Fuel Cells |
| Katyusha | 1 AtG Missile Mk. 1, 1 Disposable Missile Launcher, 5 Gesture of the Drowned, 5 Fuel Cells, 1 Pocket I.C.B.M. |
| Masochist | 10 Razorwire, 5 Repulsion Armor Plates, 1 Planula, 1 Helfire Tincture |

## CHEF recipes

| Pack | Ingredients |
| --- | --- |
| Quick Fix | 1 Item Scrap, White, 1 Mocha |
| Seared Steak | 1 Bison Steak, 1 Mocha |
| Hearty Stew | 1 Lepton Daisy, 1 Leeching Seed |
| Sauteed Worms | 1 Molten Perforator, 1 Charged Perforator |
| Ultimate Meal | 1 57 Leaf Clover, 1 Alien Head |

The recipe names resolve to verified internal item/equipment names from the installed game's catalogs. Row icons come from those same definitions, including asynchronous DLC sprite references and the sprite's atlas coordinates. Only visible rows start icon loads; loads are shared and released on unload. Spawn Items, Spawn Equipment, Remove Items, Change Chest Item, and pack contents use these icons.

The Items page now follows the requested sequence: Give All Items; Give All Items (No Lunar/Void); Roll Items; Infinite Equipment; an unlabelled divider; Item Spawn Menu; Equipment Spawn Menu; Remove Items; Drop Items; Drop From Inventory; Stack Inventory; Change Chest Item; Clear Inventory. Item Packs appears after a second unlabelled divider.

**Drop From Inventory is retained:** it removes a selected owned item/equipment and creates a world pickup through the spawn lists. **Remove Items** deletes one permanent item stack without creating a pickup. The two Drop modes remain mutually exclusive, including their keyboard shortcuts. Child-page links and keyboard shortcut row IDs follow the new order.

Numpad 1 returns to the parent menu, including from a pack preview to the selected pack in the list. It works with keyboard selection disabled, and is suppressed during search entry, keybind capture and game chat.

Validation: the installed-game Debug/x64 build passed. Managed checks cover 695 catalog, pack, icon, menu, portal and ESP assertions plus 91 UI/resizing/back shortcut assertions. All 21 recipes, repeat quantities, equipment slots, missing-entry rejection, sprite atlas identity, asynchronous loading and release, menu order, parent/Back links, section headings and navigation, cross-section ingredient search, CHEF-only search and previews, Numpad 1 navigation and focus guards, and the two removal behaviors were checked. The existing seven obsolete-API warnings remain. Rendering and asset loading still need an in-game play test.
