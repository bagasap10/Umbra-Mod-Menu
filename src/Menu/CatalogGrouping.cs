using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace UmbraMenu
{
    internal static class CatalogGrouping
    {
        private sealed class Group
        {
            public readonly int Order;
            public readonly string Title;
            public readonly Color32 Color;

            public Group(int order, string title, Color32 color)
            {
                Order = order;
                Title = title;
                Color = color;
            }
        }

        private sealed class Entry
        {
            public string Name;
            public Group Rarity;
            public Group Expansion;
            public Color32 Color;
            public Action Action;
            public int? Count;
            public Action<Button> OnButtonCreated;
            public Func<Sprite> Icon;
        }

        private static readonly Color32 White = new Color32(255, 255, 255, 255);
        private static readonly Color32 Green = new Color32(119, 255, 102, 255);
        private static readonly Color32 Red = new Color32(255, 85, 85, 255);
        private static readonly Color32 Yellow = new Color32(255, 211, 63, 255);
        private static readonly Color32 Blue = new Color32(102, 153, 255, 255);
        private static readonly Color32 Purple = new Color32(199, 138, 255, 255);
        private static readonly Color32 Orange = new Color32(255, 159, 64, 255);
        private static readonly Color32 Grey = new Color32(190, 190, 190, 255);

        private static readonly Group Common = new Group(0, "White / Common", White);
        private static readonly Group Uncommon = new Group(1, "Green / Uncommon", Green);
        private static readonly Group Legendary = new Group(2, "Red / Legendary", Red);
        private static readonly Group Boss = new Group(3, "Yellow / Boss & Planet", Yellow);
        private static readonly Group Lunar = new Group(4, "Blue / Lunar", Blue);
        private static readonly Group Void = new Group(5, "Purple / Void", Purple);
        private static readonly Group Meal = new Group(6, "Orange / Meal", Orange);
        private static readonly Group Hallowed = new Group(7, "Purple / Hallowed", Purple);
        private static readonly Group Hex = new Group(8, "Green / Hex", Green);
        private static readonly Group Pages = new Group(9, "Blue / N'tormat Pages", Blue);
        private static readonly Group OtherItems = new Group(10, "Other Items", Grey);

        private static readonly Group Equipment = new Group(0, "Orange / Equipment", Orange);
        private static readonly Group LunarEquipment = new Group(1, "Blue / Lunar Equipment", Blue);
        private static readonly Group EliteEquipment = new Group(2, "Elite Equipment", White);

        private static readonly Group BaseGame = new Group(0, "Normal / Not-DLC", Grey);
        private static readonly Group DLC1 = new Group(1, "DLC#1 / Survivors of the Void", Grey);
        private static readonly Group DLC2 = new Group(2, "DLC#2 / Seekers of the Storm", Grey);
        private static readonly Group DLC3 = new Group(3, "DLC#3 / Alloyed Collective", Grey);
        private static readonly Group DLC4 = new Group(4, "DLC#4 / Hallowed Concepts", Grey);

        public static List<Button> BuildItemButtons(Menu menu, IEnumerable<ItemIndex> itemIndices, Action<ItemIndex> itemAction,
            Func<ItemIndex, int> getCount = null, IDictionary<ItemIndex, Button> itemButtons = null, string searchText = "")
        {
            string query = (searchText ?? "").Trim();
            List<Entry> entries = new List<Entry>();
            foreach (ItemIndex itemIndex in itemIndices)
            {
                ItemDef def = ItemCatalog.GetItemDef(itemIndex);
                if (def == null || def.hidden || def.tier == ItemTier.NoTier)
                    continue;

                string displayName = Language.GetString(def.nameToken);
                if (string.IsNullOrEmpty(displayName) || displayName == def.nameToken)
                    continue;
                if (query.Length > 0 && displayName.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0)
                    continue;

                int? count = getCount == null ? (int?)null : getCount(itemIndex);
                if (count.HasValue && count.Value <= 0)
                    continue;

                Group rarity = GetItemRarity(def.tier);
                entries.Add(new Entry
                {
                    Name = displayName,
                    Icon = () => CatalogIcons.Get(def),
                    Rarity = rarity,
                    Expansion = GetExpansion(def.requiredExpansion),
                    Color = rarity.Color,
                    Action = () => itemAction(itemIndex),
                    Count = count,
                    OnButtonCreated = itemButtons == null ? (Action<Button>)null : button => itemButtons[itemIndex] = button
                });
            }

            return BuildButtons(menu, entries);
        }

        public static List<Button> BuildEquipmentButtons(Menu menu, IEnumerable<EquipmentIndex> equipmentIndices, Action<EquipmentIndex> giveEquipment)
        {
            List<Entry> entries = new List<Entry>();
            foreach (EquipmentIndex equipmentIndex in equipmentIndices)
            {
                EquipmentDef def = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (def == null || equipmentIndex == EquipmentIndex.None)
                    continue;

                string displayName = Language.GetString(def.nameToken);
                if (string.IsNullOrEmpty(displayName) || displayName == def.nameToken)
                    displayName = def.name;

                // Lunar elite aspects belong with the other elite equipment.
                Group rarity = EliteCatalog.GetEliteDefFromEquipmentIndex(equipmentIndex) != null
                    ? EliteEquipment
                    : def.isLunar ? LunarEquipment : Equipment;

                Color32 color = ColorCatalog.GetColor(def.colorIndex);
                if (color.r <= 105 && color.g <= 105 && color.b <= 105)
                    color = White;

                entries.Add(new Entry
                {
                    Name = displayName,
                    Icon = () => CatalogIcons.Get(def),
                    Rarity = rarity,
                    Expansion = GetExpansion(def.requiredExpansion),
                    Color = color,
                    Action = () => giveEquipment(equipmentIndex)
                });
            }

            return BuildButtons(menu, entries);
        }

        private static Group GetItemRarity(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1: return Common;
                case ItemTier.Tier2: return Uncommon;
                case ItemTier.Tier3: return Legendary;
                case ItemTier.Boss: return Boss;
                case ItemTier.Lunar: return Lunar;
                case ItemTier.VoidTier1:
                case ItemTier.VoidTier2:
                case ItemTier.VoidTier3:
                case ItemTier.VoidBoss: return Void;
                case ItemTier.FoodTier: return Meal;
                case ItemTier.Cursed: return Hallowed;
                case ItemTier.Hex: return Hex;
                case ItemTier.Page: return Pages;
                default: return OtherItems;
            }
        }

        private static Group GetExpansion(ExpansionDef expansion)
        {
            if (expansion == null)
                return BaseGame;

            // The token identifies the DLC independently of the game's language.
            switch (expansion.nameToken)
            {
                case "DLC1_NAME": return DLC1;
                case "DLC2_NAME": return DLC2;
                case "DLC3_NAME": return DLC3;
                case "DLC4_NAME": return DLC4;
            }

            string displayName = Language.GetString(expansion.nameToken);
            if (string.IsNullOrEmpty(displayName) || displayName == expansion.nameToken)
                displayName = expansion.name;

            return new Group(5, "Expansion / " + displayName, Grey);
        }

        private static List<Button> BuildButtons(Menu menu, IEnumerable<Entry> entries)
        {
            List<Button> buttons = new List<Button>();
            int position = 1;

            foreach (var rarityEntries in entries.GroupBy(entry => entry.Rarity.Order).OrderBy(group => group.Key))
            {
                Group rarity = rarityEntries.First().Rarity;
                buttons.Add(new Button(new DividerButton(menu, position++, rarity.Title, rarity.Color, true)));

                // Only show populated DLC sections, retaining the same DLC order in every rarity.
                var expansionGroups = rarityEntries.GroupBy(entry => entry.Expansion.Title)
                    .OrderBy(group => group.First().Expansion.Order)
                    .ThenBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase);

                foreach (var expansionEntries in expansionGroups)
                {
                    Group expansion = expansionEntries.First().Expansion;
                    buttons.Add(new Button(new DividerButton(menu, position++, expansion.Title, expansion.Color, false)));

                    foreach (Entry entry in expansionEntries.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
                    {
                        string displayName = entry.Count.HasValue ? $"{entry.Name} (x{entry.Count.Value})" : entry.Name;
                        string label = Util.GenerateColoredString(displayName, entry.Color);
                        Button button = new Button(new PickupButton(menu, position++, label, entry.Action, entry.Icon));
                        buttons.Add(button);
                        entry.OnButtonCreated?.Invoke(button);
                    }
                }
            }

            return buttons;
        }
    }
}
