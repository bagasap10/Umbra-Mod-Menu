using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    internal sealed class PackPickup
    {
        public readonly string CatalogName, DisplayName;
        public readonly int Quantity;
        public readonly bool IsEquipment;

        public PackPickup(string catalogName, string displayName, int quantity = 1, bool equipment = false)
        { CatalogName = catalogName; DisplayName = displayName; Quantity = quantity; IsEquipment = equipment; }
    }

    internal sealed class ItemPack
    {
        public readonly string Name;
        public readonly bool IsChefRecipe;
        public readonly IReadOnlyList<PackPickup> Pickups;
        public ItemPack(string name, params PackPickup[] pickups) : this(name, false, pickups) { }
        public ItemPack(string name, bool chefRecipe, params PackPickup[] pickups)
        { Name = name; IsChefRecipe = chefRecipe; Pickups = Array.AsReadOnly(pickups); }
    }

    internal sealed class ResolvedPackPickup
    {
        public PackPickup Source;
        public ItemIndex Item = ItemIndex.None;
        public EquipmentIndex Equipment = EquipmentIndex.None;
        public Func<Sprite> Icon;
        public string Name;
        public int Value;
    }

    internal static class ItemPacks
    {
        private static PackPickup Item(string id, string name, int count = 1) => new PackPickup(id, name, count);
        private static PackPickup Equipment(string id, string name) => new PackPickup(id, name, equipment: true);
        public static readonly IReadOnlyList<ItemPack> All = Array.AsReadOnly(new[]
        {
            new ItemPack("Starbucks", Item("AttackSpeedAndMoveSpeed", "Mocha", 10)),
            new ItemPack("Crits", Item("CritGlasses", "Lens-Maker's Glasses", 10), Item("AttackSpeedOnCrit", "Predatory Instincts"), Item("HealOnCrit", "Harvester's Scythe")),
            new ItemPack("Free Red", Item("RegeneratingScrap", "Regenerating Scrap", 5)),
            new ItemPack("Katyusha", Item("Missile", "AtG Missile Mk. 1"), Equipment("CommandMissile", "Disposable Missile Launcher"), Item("AutoCastEquipment", "Gesture of the Drowned", 5), Item("EquipmentMagazine", "Fuel Cell", 5), Item("MoreMissile", "Pocket I.C.B.M.")),
            new ItemPack("Zeus", Equipment("Lightning", "Royal Capacitor"), Item("AutoCastEquipment", "Gesture of the Drowned", 5), Item("EquipmentMagazine", "Fuel Cell", 5)),
            new ItemPack("Masochist", Item("Thorns", "Razorwire", 10), Item("ArmorPlate", "Repulsion Armor Plate", 5), Item("ParentEgg", "Planula"), Equipment("BurnNearby", "Helfire Tincture")),
            new ItemPack("Drug Addict", Item("AutoCastEquipment", "Gesture of the Drowned", 5), Equipment("Tonic", "Spinel Tonic")),
            new ItemPack("Bungus", Item("Mushroom", "Bustling Fungus", 5), Item("Bear", "Tougher Times"), Item("Syringe", "Soldier's Syringe"), Item("BarrierOnOverHeal", "Aegis"), Item("Seed", "Leeching Seed"), Item("BleedOnHit", "Tri-Tip Dagger"), Item("ArmorPlate", "Repulsion Armor Plate")),
            new ItemPack("1 Health Shrimp", Item("MissileVoid", "Plasma Shrimp"), Item("ShieldOnly", "Transcendence"), Item("MoreMissile", "Pocket I.C.B.M.")),
            new ItemPack("Arsonist", Item("IgniteOnKill", "Gasoline", 10), Item("StrengthenBurn", "Ignition Tank")),
            new ItemPack("DMG Stack", Equipment("DeathProjectile", "Forgive Me Please"), Item("AutoCastEquipment", "Gesture of the Drowned"), Item("EquipmentMagazine", "Fuel Cell"), Item("IncreaseDamageOnMultiKill", "Chronic Expansion", 5), Item("Talisman", "Soulbound Catalyst")),
            new ItemPack("Primary Heresy", Item("Clover", "57 Leaf Clover"), Item("LunarBadLuck", "Purity"), Item("LunarPrimaryReplacement", "Visions of Heresy")),
            new ItemPack("Seeker's Flying Object", Item("ParentEgg", "Planula"), Item("ArmorPlate", "Repulsion Armor Plate"), Item("Bear", "Tougher Times"), Item("Thorns", "Razorwire")),
            new ItemPack("Remote Katyusha", Equipment("VendingMachine", "Remote Caffeinator"), Item("Firework", "Bundle of Fireworks"), Item("MoreMissile", "Pocket I.C.B.M."), Item("Squid", "Squid Polyp"), Item("Missile", "AtG Missile Mk. 1")),
            new ItemPack("Special Forces", Equipment("GummyClone", "Goobo Jr."), Item("AutoCastEquipment", "Gesture of the Drowned", 5)),
            new ItemPack("Shield Tank", Item("ShieldOnly", "Transcendence"), Item("DelayedDamage", "Warped Echo", 5), Item("OutOfCombatArmor", "Oddly-shaped Opal", 5)),
            new ItemPack("Quick Fix", true, Item("ScrapWhite", "Item Scrap, White"), Item("AttackSpeedAndMoveSpeed", "Mocha")),
            new ItemPack("Seared Steak", true, Item("FlatHealth", "Bison Steak"), Item("AttackSpeedAndMoveSpeed", "Mocha")),
            new ItemPack("Hearty Stew", true, Item("TPHealingNova", "Lepton Daisy"), Item("Seed", "Leeching Seed")),
            new ItemPack("Ultimate Meal", true, Item("Clover", "57 Leaf Clover"), Item("AlienHead", "Alien Head")),
            new ItemPack("Sauteed Worms", true, Item("FireballsOnHit", "Molten Perforator"), Item("LightningStrikeOnHit", "Charged Perforator"))
        });

        // Relative sorting points, not an in-game currency or a claim about build strength.
        internal static int RarityValue(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1: case ItemTier.VoidTier1: return 1;
                case ItemTier.Tier2: case ItemTier.VoidTier2: case ItemTier.Lunar: return 3;
                case ItemTier.Tier3: case ItemTier.VoidTier3: case ItemTier.Boss: case ItemTier.VoidBoss: return 9;
                default: return 3;
            }
        }

        public static bool TryResolve(ItemPack pack, out List<ResolvedPackPickup> contents, out string error)
        {
            contents = new List<ResolvedPackPickup>();
            error = null;
            if (pack == null) { error = "Choose a pack first."; return false; }
            foreach (PackPickup pickup in pack.Pickups)
            {
                var entry = new ResolvedPackPickup { Source = pickup };
                string token;
                if (pickup.IsEquipment)
                {
                    entry.Equipment = EquipmentCatalog.FindEquipmentIndex(pickup.CatalogName);
                    EquipmentDef def = EquipmentCatalog.GetEquipmentDef(entry.Equipment);
                    if (entry.Equipment == EquipmentIndex.None || def == null) { error = "Unavailable: " + pickup.DisplayName; return false; }
                    token = def.nameToken;
                    entry.Icon = () => CatalogIcons.Get(def);
                    entry.Value = 6;
                }
                else
                {
                    entry.Item = ItemCatalog.FindItemIndex(pickup.CatalogName);
                    ItemDef def = ItemCatalog.GetItemDef(entry.Item);
                    if (entry.Item == ItemIndex.None || def == null || def.hidden) { error = "Unavailable: " + pickup.DisplayName; return false; }
                    token = def.nameToken;
                    entry.Icon = () => CatalogIcons.Get(def);
                    entry.Value = RarityValue(def.tier) * pickup.Quantity;
                }
                string name = Language.GetString(token);
                entry.Name = string.IsNullOrEmpty(name) || name == token ? pickup.DisplayName : name;
                contents.Add(entry);
            }
            return true;
        }

        public static IEnumerable<ItemPack> Sorted()
        {
            return All.OrderBy(pack => pack.IsChefRecipe)
                .ThenBy(pack => TryResolve(pack, out var contents, out _) ? contents.Sum(entry => entry.Value) : int.MaxValue)
                .ThenBy(pack => pack.Name, StringComparer.CurrentCultureIgnoreCase);
        }

        public static bool Give(ItemPack pack, out string message)
        {
            Inventory inventory = UmbraMenu.LocalPlayerInv;
            if (!UmbraMenu.InGameCheck() || !inventory) { message = "Start a run to give a pack."; return false; }
            // Resolve every entry before changing inventory so missing DLC entries cannot give a partial pack.
            if (!TryResolve(pack, out var contents, out message)) return false;
            foreach (var group in contents.Where(entry => !entry.Source.IsEquipment).GroupBy(entry => entry.Item))
            {
                long quantity = group.Sum(entry => (long)entry.Source.Quantity);
                if (group.Any(entry => entry.Source.Quantity <= 0) || quantity > int.MaxValue
                    || !InventorySafety.CanAdd(inventory.GetItemCountPermanent(group.Key), (int)quantity))
                { message = "Item stack limit reached. No pack was given."; return false; }
            }
            foreach (ResolvedPackPickup entry in contents.Where(value => !value.Source.IsEquipment))
                inventory.GiveItemPermanent(entry.Item, entry.Source.Quantity);
            foreach (ResolvedPackPickup entry in contents.Where(value => value.Source.IsEquipment))
                inventory.SetEquipmentIndex(entry.Equipment, new EquipmentLocation { slot = (uint)inventory.activeEquipmentSlot, set = 0 });
            message = "Gave " + pack.Name + ". Click again to add another pack.";
            return true;
        }
    }
}
