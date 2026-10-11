using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using RoR2;
using System.IO;
using System.Reflection;
using System.Globalization;

namespace UmbraMenu
{
    public static class Utility
    {
        public static Menu FindMenuById(int id)
        {
            for (int i = 0; i < UmbraMenu.menus.Count; i++)
            {
                Menu currentMenu = UmbraMenu.menus[i];
                if (currentMenu.GetId() == id)
                {
                    return currentMenu;
                }
            }
            throw new NullReferenceException($"Menu with id '{id}' was not found");
        }

        public static Button FindButtonById(int menuId, int buttonId)
        {
            List<Button> menuButtons = UmbraMenu.menus[menuId].GetButtons();
            for (int i = 0; i < menuButtons.Count; i++)
            {
                Button currentButton = menuButtons[i];
                if (currentButton.GetId() == buttonId)
                {
                    return currentButton;
                }
            }
            throw new NullReferenceException($"Button with id '{buttonId}' was not found in menu '{menuId}'");
        }

        public static bool CursorIsVisible()
        {
            for (int i = 0; i < RoR2.UI.MPEventSystem.readOnlyInstancesList.Count; i++)
            {
                var mpeventSystem = RoR2.UI.MPEventSystem.readOnlyInstancesList[i];
                if (mpeventSystem.isCursorVisible)
                {
                    return true;
                }
            }
            return false;
        }

        public static SurvivorIndex GetCurrentCharacter()
        {
            var bodyIndex = BodyCatalog.FindBodyIndex(UmbraMenu.LocalPlayerBody);
            var survivorIndex = SurvivorCatalog.GetSurvivorIndexFromBodyIndex(bodyIndex);
            return survivorIndex;
        }

        public static HashSet<Tuple<int, int, bool>> SaveMenuState()
        {
            HashSet<Tuple<int, int, bool>> enabledButtons = new HashSet<Tuple<int, int, bool>>();
            for (int menuId = 1; menuId < UmbraMenu.menus.Count; menuId++)
            {
                // Inventory rows change as stacks are removed; preserve only whether this menu is open.
                if (menuId == Menus.InventoryItemList.MenuId)
                {
                    enabledButtons.Add(new Tuple<int, int, bool>(menuId, -1, UmbraMenu.menus[menuId].IsEnabled()));
                    continue;
                }

                Tuple<int, int, bool> menuButtonsEnabled = new Tuple<int, int, bool>(-1, -1, false);
                try
                {
                    for (int buttonPos = 1; buttonPos < UmbraMenu.menus[menuId].GetButtons().Count + 1; buttonPos++)
                    {
                        Button currentButton = FindButtonById(menuId, buttonPos);

                        if (currentButton.IsEnabled())
                        {
                            menuButtonsEnabled = new Tuple<int, int, bool>(menuId, buttonPos, UmbraMenu.menus[menuId].IsEnabled());
                        }
                        else if (UmbraMenu.menus[menuId].IsEnabled())
                        {
                            menuButtonsEnabled = new Tuple<int, int, bool>(menuId, -1, UmbraMenu.menus[menuId].IsEnabled());
                        }
                        if (menuButtonsEnabled?.Item1 != -1)
                        {
                            enabledButtons.Add(menuButtonsEnabled);
                        }
                    }
                }
                catch (NullReferenceException e)
                {
                    System.Console.WriteLine(e);
                    continue;
                }
            }
            return enabledButtons;
        }

        public static void ReadMenuState(HashSet<Tuple<int, int, bool>> menuState)
        {
            foreach (var currentTuple in menuState)
            {
                int menuId = currentTuple.Item1;
                int buttonPos = currentTuple.Item2;
                if (menuId == Menus.InventoryItemList.MenuId)
                {
                    if (currentTuple.Item3 && UmbraMenu.characterCollected)
                    {
                        UmbraMenu.inventoryItemListMenu.SetEnabled(true);
                        UmbraMenu.inventoryItemListMenu.RefreshInventory(true);
                    }
                    continue;
                }

                if (menuId != -1 && buttonPos != -1)
                {
                    FindButtonById(menuId, buttonPos).SetEnabled(true);
                }
            }
        }

        public static void StubbedFunction()
        {
            return;
        }

        #region Get Lists
        public static List<SurvivorDef> GetSurvivorDefs()
        {
            List<SurvivorDef> result = SurvivorCatalog.allSurvivorDefs.ToList();
            return result;
        }

        public static List<GameObject> GetBodyPrefabs()
        {
            List<GameObject> bodyPrefabs = new List<GameObject>();

            for (int i = 0; i < BodyCatalog.allBodyPrefabs.Count(); i++)
            {
                var prefab = BodyCatalog.allBodyPrefabs.ElementAt(i);
                if (prefab.name != "ScavSackProjectile")
                {
                    bodyPrefabs.Add(prefab);
                }
            }
            return bodyPrefabs;
        }

        public static List<EquipmentIndex> GetEquipment()
        {
            List<EquipmentIndex> equipment = new List<EquipmentIndex>();

            List<EquipmentIndex> equip = new List<EquipmentIndex>();
            List<EquipmentIndex> lunar = new List<EquipmentIndex>();
            List<EquipmentIndex> other = new List<EquipmentIndex>();

            Color32 equipColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Equipment);
            Color32 lunarColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarItem);

            foreach (EquipmentIndex equipmentIndex in EquipmentCatalog.allEquipment)
            {
                Color32 currentEquipColor = ColorCatalog.GetColor(EquipmentCatalog.GetEquipmentDef(equipmentIndex).colorIndex);
                if (currentEquipColor.Equals(equipColor)) // equipment
                {
                    equip.Add(equipmentIndex);
                }
                else if (currentEquipColor.Equals(lunarColor)) // lunar equipment
                {
                    lunar.Add(equipmentIndex);
                }
                else // other
                {
                    other.Add(equipmentIndex);
                }
            }
            UmbraMenu.unreleasedEquipment = other;
            var result = equipment.Concat(lunar).Concat(equip).Concat(other).ToList();
            return result;
        }

        public static List<ItemIndex> GetItems()
        {
            List<ItemIndex> boss = new List<ItemIndex>();
            List<ItemIndex> tier3 = new List<ItemIndex>();
            List<ItemIndex> tier2 = new List<ItemIndex>();
            List<ItemIndex> tier1 = new List<ItemIndex>();
            List<ItemIndex> lunar = new List<ItemIndex>();
            List<ItemIndex> voidt = new List<ItemIndex>();
            List<ItemIndex> other = new List<ItemIndex>();

            foreach (ItemIndex itemIndex in ItemCatalog.allItems)
            {
                ItemDef def = ItemCatalog.GetItemDef(itemIndex);
                if (def == null)
                    continue;

                // Skip hidden, deprecated, or internal items
                if (def.hidden)
                    continue;
                if (def.tier == ItemTier.NoTier)
                    continue;

                // Skip unlocalized tokens (ITEM_BOOSTDAMAGE_NAME, etc.)
                string localized = Language.GetString(def.nameToken);
                if (localized == def.nameToken)
                    continue;

                // Categorize
                switch (def.tier)
                {
                    case ItemTier.Boss:
                        boss.Add(itemIndex);
                        break;
                    case ItemTier.Tier3:
                        tier3.Add(itemIndex);
                        break;
                    case ItemTier.Tier2:
                        tier2.Add(itemIndex);
                        break;
                    case ItemTier.Tier1:
                        tier1.Add(itemIndex);
                        break;
                    case ItemTier.Lunar:
                        lunar.Add(itemIndex);
                        break;
                    case ItemTier.VoidTier1:
                    case ItemTier.VoidTier2:
                    case ItemTier.VoidTier3:
                    case ItemTier.VoidBoss:
                        voidt.Add(itemIndex);
                        break;

                    default:
                        // Should rarely happen unless modded or weird DLC items
                        other.Add(itemIndex);
                        break;
                }
            }

            // Save for other parts of Umbra
            UmbraMenu.bossItems = boss;
            UmbraMenu.unreleasedItems = other;

            // Final ordering (same as Umbra originally intended)
            return boss
                .Concat(tier3)
                .Concat(tier2)
                .Concat(tier1)
                .Concat(lunar)
                .Concat(voidt)
                .Concat(other)
                .ToList();
        }

        //Original
        //public static List<ItemIndex> GetItems()
        //{
        //    List<ItemIndex> items = new List<ItemIndex>();

        //    List<ItemIndex> boss = new List<ItemIndex>();
        //    List<ItemIndex> tier3 = new List<ItemIndex>();
        //    List<ItemIndex> tier2 = new List<ItemIndex>();
        //    List<ItemIndex> tier1 = new List<ItemIndex>();
        //    List<ItemIndex> lunar = new List<ItemIndex>();
        //    List<ItemIndex> voidt = new List<ItemIndex>();
        //    List<ItemIndex> other = new List<ItemIndex>();

        //    Color32 bossColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.BossItem);
        //    Color32 tier3Color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier3Item);
        //    Color32 tier2Color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier2Item);
        //    Color32 tier1Color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier1Item);
        //    Color32 lunarColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarItem);
        //    Color32 voidColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.VoidItem);

        //    foreach (ItemIndex itemIndex in ItemCatalog.allItems)
        //    {
        //        Color32 itemColor = ColorCatalog.GetColor(ItemCatalog.GetItemDef(itemIndex).colorIndex);
        //        if (itemColor.Equals(bossColor)) // boss
        //        {
        //            boss.Add(itemIndex);
        //        }
        //        else if (itemColor.Equals(tier3Color)) // tier 3
        //        {
        //            tier3.Add(itemIndex);
        //        }
        //        else if (itemColor.Equals(tier2Color)) // tier 2
        //        {
        //            tier2.Add(itemIndex);
        //        }
        //        else if (itemColor.Equals(tier1Color)) // tier 1
        //        {
        //            tier1.Add(itemIndex);
        //        }
        //        else if (itemColor.Equals(lunarColor)) // lunar
        //        {
        //            lunar.Add(itemIndex);
        //        }
        //        else if (itemColor.Equals(voidColor)) // Void
        //        {
        //            voidt.Add(itemIndex);
        //        }
        //        else // Other
        //        {
        //            other.Add(itemIndex);
        //        }
        //    }

        //    UmbraMenu.bossItems = boss;
        //    UmbraMenu.unreleasedItems = other;
        //    var result = items.Concat(boss).Concat(tier3).Concat(tier2).Concat(tier1).Concat(lunar).Concat(voidt).Concat(other).ToList();
        //    return result;
        //}

        public static List<BuffDef> GetBuffs()
        {
            List<BuffDef> buffs = new List<BuffDef>();

            List<BuffDef> eliteBuff = new List<BuffDef>();
            List<BuffDef> nonEliteBuff = new List<BuffDef>();
            List<BuffDef> eliteDebuff = new List<BuffDef>();
            List<BuffDef> nonEliteDebuff = new List<BuffDef>();
            List<BuffDef> other = new List<BuffDef>();


            foreach (BuffDef buffDef in typeof(BuffCatalog).GetField<BuffDef[]>("buffDefs"))
            {
                if (!buffDef.isDebuff && buffDef.isElite)
                {
                    eliteBuff.Add(buffDef);
                }
                else if (!buffDef.isDebuff && !buffDef.isElite)
                {
                    nonEliteBuff.Add(buffDef);
                }
                else if (buffDef.isDebuff && buffDef.isElite)
                {
                    eliteDebuff.Add(buffDef);
                }
                else if (buffDef.isDebuff && !buffDef.isElite)
                {
                    nonEliteDebuff.Add(buffDef);
                }
                else
                {
                    other.Add(buffDef);
                }
            }
            var result = buffs.Concat(eliteBuff).Concat(nonEliteBuff).Concat(eliteDebuff).Concat(nonEliteDebuff).Concat(other).ToList();
            return result;
        }

        public static List<SpawnCard> GetSpawnCards()
        {
            List<SpawnCard> spawnCards = Resources.FindObjectsOfTypeAll<SpawnCard>().ToList();
            return spawnCards;
        }

        public static List<HurtBox> GetHurtBoxes()
        {
            return EspTargets.GetEnemies(UmbraMenu.LocalPlayerBody)
                .Where(body => body.mainHurtBox)
                .Select(body => body.mainHurtBox)
                .ToList();
        }
        #endregion

        #region Menu Resets
        public static void ResetMenu()
        {
            for (int i = 0; i < UmbraMenu.menus.Count; i++)
            {
                Menu currentMenu = UmbraMenu.menus[i];
                currentMenu.Reset();
            }
            UmbraMenu.characterCollected = false;
            SoftResetMenu(false);
            UmbraMenu.scrolled = false;
            UmbraMenu.navigationToggle = false;
            Navigation.menuIndex = 0;
            Navigation.buttonIndex = 0;
        }

        public static void CloseOpenMenus()
        {
            List<Menu> openMenus = GetMenusOpen();
            for (int i = 0; i < openMenus.Count; i++)
            {
                openMenus[i].SetEnabled(false);
            }
        }

        // Soft reset when moving to next stage to keep player stat mods and god mode between stages
        public static void SoftResetMenu(bool preserveState)
        {
            HashSet<Tuple<int, int, bool>> savedMenuState = new HashSet<Tuple<int, int, bool>>();
            if (preserveState)
            {
                savedMenuState = SaveMenuState();
            }
            UmbraMenu.mainMenu = new Menus.Main();
            UmbraMenu.playerMenu = new Menus.Player();
            UmbraMenu.movementMenu = new Menus.Movement();
            UmbraMenu.itemsMenu = new Menus.Items();
            UmbraMenu.spawnMenu = new Menus.Spawn();
            UmbraMenu.teleporterMenu = new Menus.Teleporter();
            UmbraMenu.renderMenu = new Menus.Render();
            UmbraMenu.settingsMenu = new Menus.Settings();

            UmbraMenu.statsModMenu = new Menus.StatsMod();
            UmbraMenu.viewStatsMenu = new Menus.ViewStats();
            UmbraMenu.characterListMenu = new Menus.CharacterList();
            UmbraMenu.buffListMenu = new Menus.BuffList();

            UmbraMenu.itemListMenu = new Menus.ItemList();
            UmbraMenu.equipmentListMenu = new Menus.EquipmentList();
            UmbraMenu.chestItemListMenu = new Menus.ChestItemList();

            UmbraMenu.spawnListMenu = new Menus.SpawnList();

            UmbraMenu.keybindListMenu = new Menus.KeybindList();
            UmbraMenu.inventoryItemListMenu = new Menus.InventoryItemList();
            UmbraMenu.itemPackListMenu = new Menus.ItemPackList();
            UmbraMenu.itemPackDetailsMenu = new Menus.ItemPackDetails();
            UmbraMenu.worldCheatsMenu = new Menus.WorldCheatsMenu();

            UmbraMenu.menus = new List<Menu>()
            {
                UmbraMenu.mainMenu, //0
                UmbraMenu.playerMenu, //1
                UmbraMenu.movementMenu, //2
                UmbraMenu.itemsMenu, //3
                UmbraMenu.spawnMenu, //4
                UmbraMenu.teleporterMenu, //5
                UmbraMenu.renderMenu, //6
                UmbraMenu.settingsMenu, //7
                UmbraMenu.statsModMenu, //8
                UmbraMenu.viewStatsMenu, //9
                UmbraMenu.characterListMenu, //10
                UmbraMenu.buffListMenu, //11
                UmbraMenu.itemListMenu, //12
                UmbraMenu.equipmentListMenu, //13
                UmbraMenu.chestItemListMenu, //14
                UmbraMenu.spawnListMenu, //15
                UmbraMenu.keybindListMenu, //16
                UmbraMenu.inventoryItemListMenu, //17
                UmbraMenu.itemPackListMenu, //18
                UmbraMenu.itemPackDetailsMenu, //19
                UmbraMenu.worldCheatsMenu //20
            };
            if (preserveState)
            {
                ReadMenuState(savedMenuState);
            }


        }
        #endregion

        #region Keybind Utility
        public static Button GetEnabledKeybindButton()
        {
            Menu keybindMenu = UmbraMenu.menus[16];
            for (int i = 0; i < keybindMenu.GetNumberOfButtons(); i++)
            {
                Button button = keybindMenu.GetButtons()[i];
                if (button.IsEnabled())
                {
                    return button;
                }
            }
            throw new NullReferenceException($"No buttons are enabled in the keybind menu");
        }

        public static bool KeyCodeInUse(KeyCode keyCode)
        {
            for (int i = 0; i < UmbraMenu.keybindDict.Count; i++)
            {
                Keybind keybind = UmbraMenu.keybindDict[UmbraMenu.keyBindNames[i]];
                if (keybind.KeyCode == keyCode)
                {
                    return true;
                }
            }
            return false;
        }

        public static Keybind FindKeybindByKeyCode(KeyCode keyCode)
        {
            for (int i = 0; i < UmbraMenu.keybindDict.Count; i++)
            {
                Keybind keybind = UmbraMenu.keybindDict[UmbraMenu.keyBindNames[i]];
                if (keybind.KeyCode == keyCode)
                {
                    return keybind;
                }
            }
            throw new NullReferenceException($"Keybind using keycode '{keyCode}' was not found.");
        }
        #endregion

        public static List<Menu> GetMenusOpen()
        {
            List<Menu> openMenus = new List<Menu>();
            for (int i = 1; i < UmbraMenu.menus.Count; i++)
            {
                if (UmbraMenu.menus[i].IsEnabled() && UmbraMenu.menus[i].GetId() != 9)
                {
                    openMenus.Add(UmbraMenu.menus[i]);
                }
            }
            return openMenus;
        }

        #region Settings Functions
        public static void SaveSettings()
        {
            try
            {
                MenuSettings.Save(UmbraMenu.SETTINGSPATH, UmbraMenu.Width, UmbraMenu.AllowNavigation, UmbraMenu.GodVersion,
                    UmbraMenu.keyBindNames, name => UmbraMenu.keybindDict[name].KeyCode.ToString(),
                    UmbraMenu.WindowWidth, UmbraMenu.WindowHeight, UmbraMenu.GlassOpacity);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            { Debug.LogWarning("Umbra: settings were not saved: " + error.Message); }
        }

        public static List<string> ReadSettings() => MenuSettings.Read(UmbraMenu.SETTINGSPATH, UmbraMenu.keyBindNames);
        public static float ReadUiSetting(string name, float fallback) => MenuSettings.ReadNumber(UmbraMenu.SETTINGSPATH, name, fallback);
        public static void CreateDefaultSettingsFile() => MenuSettings.CreateDefaults(UmbraMenu.SETTINGSPATH, UmbraMenu.keyBindNames);
        public static void WriteToLog(string logContent)
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using (StreamWriter outputFile = new StreamWriter(System.IO.Path.Combine(docPath, "UmbraLog.txt"), true))
            {
                outputFile.WriteLine("[UmbraMenu]: " + logContent);
            }
        }

        public static void WriteFieldsAndPropertiesToLog(object obj)
        {
            List<string> fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(field => field.Name).ToList();
            List<string> properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(property => property.Name).ToList();
            WriteToLog("Fields: ");
            foreach (var field in fields) { WriteToLog($"    {field}"); }
            WriteToLog("Properties: ");
            foreach (var property in properties) { WriteToLog($"    {property}"); }
        }
        #endregion
    }
}
