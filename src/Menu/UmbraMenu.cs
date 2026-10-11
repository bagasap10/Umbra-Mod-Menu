using System;
using System.Collections.Generic;
using UnityEngine;
using RoR2;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Text.RegularExpressions;

namespace UmbraMenu
{
    public class UmbraMenu : MonoBehaviour
    {
        public const string
            NAME = "U M B R A",
            VERSION = "4.0.1",
            DLC_NAME = "Hallowed Concepts";



        // Keep the existing settings file when the displayed DLC label changes.
        public static string SETTINGSPATH = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UmbraMenu/settings-Alloyed Collective.ini");

        #region Player Variables
        public static CharacterMaster LocalPlayer;
        public static CharacterBody   LocalPlayerBody;
        public static Inventory       LocalPlayerInv;
        public static HealthComponent LocalHealth;
        public static SkillLocator    LocalSkills;
        public static NetworkUser     LocalNetworkUser;
        public static CharacterMotor  LocalMotor;
        #endregion

        #region Init Lists
        public static Dictionary<String, UnlockableDef> unlockables = typeof(UnlockableCatalog).GetField<Dictionary<String, UnlockableDef>>("nameToDefTable");
        public static List<SurvivorDef> survivorDefs = Utility.GetSurvivorDefs();
        public static List<GameObject> bodyPrefabs = Utility.GetBodyPrefabs();
        public static List<EquipmentIndex> equipment = Utility.GetEquipment();
        public static List<ItemIndex> items = Utility.GetItems();
        public static List<BuffDef> buffs = Utility.GetBuffs();
        public static List<SpawnCard> spawnCards = Utility.GetSpawnCards();
        public static List<ItemIndex> bossItems;
        public static List<ItemIndex> unreleasedItems;
        public static List<EquipmentIndex> unreleasedEquipment;
        #endregion

        #region Misc Variabled used in some features
        public static int previousMenuOpen;
        public static bool characterCollected, navigationToggle, devDoOnce = true, devMode, forceFullModMenu, lowResolutionMonitor, chatOpen, msgSent = true, scrolled, listenForKeybind;
        public static Scene currentScene;
        #endregion

        #region Settings Variables
        public static List<string> keyBindNames = new List<string>()
        {
            "MAIN MENU",

            "PLAYER MENU",
            "GIVE MONEY",
            "GIVE COINS",
            "GIVE XP",
            "STATS MENU",
            "VIEW STATS MENU",
            "CHANGE CHARACTER MENU",
            "BUFF MENU",
            "REMOVE BUFFS",
            "AIMBOT",
            "GODMODE",
            "INFINITE SKILLS",

            "MOVEMENT MENU",
            "ALWAYS SPRINT",
            "FLIGHT",
            "JUMP PACK",

            "ITEMS MENU",
            "GIVE ALL",
            "ROLL ITEMS",
            "ITEMS LIST MENU",
            "EQUIPMENT LIST MENU",
            "DROP ITEMS",
            "DROP INVENTORY ITEMS",
            "INFINITE EQUIPMENT",
            "STACK INVENTORY",
            "CLEAR INVENTORY",
            "CHANGE CHEST ITEM MENU",

            "SPAWN MENU",
            "SPAWN LIST MENU",
            "KILL ALL",
            "DESTROY INTERACTABLES",

            "TELEPORTER MENU",
            "SKIP STAGE",
            "INSTANT TELE CHARGE",
            "ADD MOUNTAIN STACK",
            "SPAWN ALL PORTALS",
            "SPAWN BLUE PORTAL",
            "SPAWN CELE PORTAL",
            "SPAWN GOLD PORTAL",

            "RENDER MENU",
            "RENDER ACTIVE MODS",
            "RENDER INTERACTABLES",
            "RENDER MOB ESP"
        };

        public static List<string> Settings = Utility.ReadSettings();

        public static float Width = float.Parse(Settings[0], System.Globalization.CultureInfo.InvariantCulture);
        public static float WindowWidth = Mathf.Clamp(Utility.ReadUiSetting("Window Width", 680), MenuUI.MinWidth, MenuUI.MaxWidth);
        public static float WindowHeight = Mathf.Clamp(Utility.ReadUiSetting("Window Height", 560), MenuUI.MinHeight, MenuUI.MaxHeight);
        public static float GlassOpacity = Mathf.Clamp(Utility.ReadUiSetting("Glass Opacity", .62f), .35f, .9f);
        public static bool AllowNavigation = bool.Parse(Settings[1]);
        public static int GodVersion = int.Parse(Settings[2]);
        public static Dictionary<string, Keybind> keybindDict = BuildKeybinds();
        #endregion

        #region Menus
        public static Menu mainMenu = new Menus.Main();

        public static Menu playerMenu = new Menus.Player();
        public static Menu movementMenu = new Menus.Movement();
        public static Menu itemsMenu = new Menus.Items();
        public static Menu spawnMenu = new Menus.Spawn();
        public static Menu teleporterMenu = new Menus.Teleporter();
        public static Menu renderMenu = new Menus.Render();
        public static Menu settingsMenu = new Menus.Settings();

        public static Menu statsModMenu = new Menus.StatsMod();
        public static Menu viewStatsMenu = new Menus.ViewStats();
        public static Menu characterListMenu = new Menus.CharacterList();
        public static Menu buffListMenu = new Menus.BuffList();

        public static Menu itemListMenu = new Menus.ItemList();
        public static Menu equipmentListMenu = new Menus.EquipmentList();
        public static Menu chestItemListMenu = new Menus.ChestItemList();

        public static Menu spawnListMenu = new Menus.SpawnList();

        public static Menu keybindListMenu = new Menus.KeybindList();
        public static Menus.InventoryItemList inventoryItemListMenu = new Menus.InventoryItemList();
        public static Menus.ItemPackList itemPackListMenu = new Menus.ItemPackList();
        public static Menus.ItemPackDetails itemPackDetailsMenu = new Menus.ItemPackDetails();

        public static Menus.WorldCheatsMenu worldCheatsMenu = new Menus.WorldCheatsMenu();

        public static List<Menu> menus = new List<Menu>()
        {
            mainMenu, //0
            playerMenu, //1
            movementMenu, //2
            itemsMenu, //3
            spawnMenu, //4
            teleporterMenu, //5
            renderMenu, //6
            settingsMenu, //7
            statsModMenu, //8
            viewStatsMenu, //9
            characterListMenu, //10
            buffListMenu, //11
            itemListMenu, //12
            equipmentListMenu, //13
            chestItemListMenu, //14
            spawnListMenu, //15
            keybindListMenu, //16
            inventoryItemListMenu, //17
            itemPackListMenu, //18
            itemPackDetailsMenu, //19
            worldCheatsMenu //20
        };
        #endregion

        #region Main Unity Functions
        private bool ready;
        private SafeRoutine[] routines;
        private SafeRoutine drawRoutine, espRoutine, fixedRoutine;
        private float nextEnemyRefresh, nextPanelRefresh;

        private void OnGUI()
        {
            if (!ready) return;
            drawRoutine.Run();
            if (Event.current.type == EventType.Repaint) espRoutine.Run();
        }

        private void DrawMenu()
        {
            MenuUI.DrawWatermark();
            MenuUI.Draw();
        }

        private static void CleanUp(Action action)
        {
            try { action(); } catch (Exception error) { Debug.LogWarning("Umbra: cleanup failed: " + error.Message); }
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneLoaded;
            CleanUp(Menus.Render.DisableInteractables);
            CleanUp(Menus.SpawnList.DisableSpawnList);
            CleanUp(Menus.ChestItemList.DisableChests);
            CleanUp(WorldCheats.Shutdown);
            Menus.Spawn.spawnedObjects.Clear();
            CleanUp(Menus.Teleporter.ReleaseAssets);
            CleanUp(CatalogIcons.Release);
            CleanUp(PortalIcons.Release);
            CleanUp(MenuUI.Release);
            CleanUp(UITheme.Release);
            CleanUp(Styles.Release);
            CleanUp(Textures.Release);
            Reflections.fields.Clear(); Reflections.properties.Clear();
            Menus.Render.enemyBodies.Clear();
            ClearCharacterReferences();
            if (ready) Loader.Report("unloaded", "Menu unloaded.");
            ready = false;
        }

        public void Start()
        {
            try
            {
                currentScene = SceneManager.GetActiveScene();
                GetCharacter();
                Utility.SoftResetMenu(false);
                WorldCheats.Initialize();
                routines = new[]
                {
                    new SafeRoutine("Player lookup", CharacterRoutine),
                    new SafeRoutine("Page synchronization", MenuUI.SyncPage),
                    new SafeRoutine("Inputs", CheckInputs),
                    new SafeRoutine("Development shortcuts", DevBuildRoutine),
                    new SafeRoutine("Skills", SkillsRoutine),
                    new SafeRoutine("Aimbot", AimBotRoutine),
                    new SafeRoutine("God mode", GodRoutine),
                    new SafeRoutine("Equipment cooldown", EquipCooldownRoutine),
                    new SafeRoutine("Stats modifiers", ModStatsRoutine),
                    new SafeRoutine("Flight", FlightRoutine),
                    new SafeRoutine("Sprint", SprintRoutine),
                    new SafeRoutine("Jump pack", JumpPackRoutine),
                    new SafeRoutine("World cheats", WorldCheats.Tick),
                    new SafeRoutine("Inventory refresh", () => inventoryItemListMenu.RefreshInventory()),
                    new SafeRoutine("Interactables refresh", Menus.Render.RefreshInteractablesIfNeeded),
                    new SafeRoutine("Navigation", UpdateNavIndexRoutine),
                    new SafeRoutine("Panels", RefreshPanels),
                    new SafeRoutine("Keybind capture", SetKeybindRoutine),
                    new SafeRoutine("Full menu", ForceFullMenuRoutine)
                };
                drawRoutine = new SafeRoutine("Menu drawing", DrawMenu);
                espRoutine = new SafeRoutine("ESP drawing", ESPRoutine);
                fixedRoutine = new SafeRoutine("World target refresh", RefreshWorldTargets);
                SceneManager.activeSceneChanged += OnSceneLoaded;
                mainMenu.SetEnabled(true);
                MenuUI.NavigateTo(0);
                ready = true;
                Loader.Report("ready", "Menu ready.", MenuUI.ToggleShortcut);
            }
            catch (Exception error)
            {
                enabled = false;
                Debug.LogError("Umbra: startup failed: " + error);
                Loader.Report("error", error.GetBaseException().Message);
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        private void RefreshPanels()
        {
            if (!mainMenu.IsEnabled() || Time.unscaledTime < nextPanelRefresh) return;
            nextPanelRefresh = Time.unscaledTime + .1f;
            Menus.ViewStats.UpdateViewStats();
            UpdateMainButtonsRoutine();
            UpdateKeybindButtonRoutine();
        }

        public void Update()
        {
            if (!ready) return;
            currentScene = SceneManager.GetActiveScene();
            foreach (var routine in routines) routine.Run();
        }

        public void FixedUpdate()
        {
            if (ready) fixedRoutine.Run();
        }

        private void RefreshWorldTargets()
        {
            if (!InGameCheck() || !characterCollected) { Menus.Render.enemyBodies.Clear(); return; }
            if (Menus.Render.renderMobs && Time.unscaledTime >= nextEnemyRefresh)
            {
                nextEnemyRefresh = Time.unscaledTime + .1f;
                EspTargets.FillEnemies(LocalPlayerBody, Menus.Render.enemyBodies);
            }
            else if (!Menus.Render.renderMobs) Menus.Render.enemyBodies.Clear();
            if (Menus.Items.chestItemList)
                Menus.ChestItemList.IsClosestChestEquip = Menus.ChestItemList.CheckClosestChestEquip();
        }
        #endregion

        #region Misc Functions
        public void OnSceneLoaded(Scene s1, Scene s2)
        {
            try { ResetForScene(s1, s2); }
            catch (Exception error) { Debug.LogError("Umbra: scene reset failed: " + error); Loader.Report("degraded", "Scene reset failed. See Player.log."); }
        }

        private void ResetForScene(Scene s1, Scene s2)
        {
            WorldCheats.ResetStage();
            ClearCharacterReferences();
            Menus.Render.enemyBodies.Clear();
            Menus.Spawn.spawnedObjects.Clear();
            Menus.Render.DisableInteractables();
            if (Menus.Render.renderInteractables) Menus.Render.EnableInteractables();
            Menus.Teleporter.ReleaseAssets();
            nextEnemyRefresh = nextPanelRefresh = 0;
            if (routines != null) foreach (var routine in routines) routine.Reset();
            drawRoutine?.Reset(); espRoutine?.Reset(); fixedRoutine?.Reset();
            if (s2 != null)
            {
                bool inGame = s2.name != "title" && s2.name != "lobby" && s2.name != "" && s2.name != " ";
                if (!inGame)
                {
                    if (Screen.height < 1080)
                    {
                        lowResolutionMonitor = true;
                        Menus.Render.renderMods = false;
                    }
                    Utility.ResetMenu();
                }
                else
                {
                    if (Menus.Render.renderInteractables)
                    {
                        Menus.Render.RefreshInteractables();
                    }
                }
            }
        }

        public static void ClearCharacterReferences()
        {
            characterCollected = false;
            LocalNetworkUser = null; LocalPlayer = null; LocalPlayerInv = null;
            LocalPlayerBody = null; LocalHealth = null; LocalSkills = null; LocalMotor = null;
        }

        public static void GetCharacter()
        {
            if (!InGameCheck()) { ClearCharacterReferences(); return; }
            NetworkUser user = null;
            foreach (var candidate in NetworkUser.readOnlyLocalPlayersList)
                if (candidate && candidate.isLocalPlayer) { user = candidate; break; }
            if (!user || !user.master) { ClearCharacterReferences(); return; }
            var master = user.master;
            var body = master.GetBody();
            LocalNetworkUser = user; LocalPlayer = master; LocalPlayerInv = master.inventory;
            LocalPlayerBody = body;
            LocalHealth = body ? body.healthComponent : null;
            LocalSkills = body ? body.skillLocator : null;
            LocalMotor = body ? body.characterMotor : null;
            characterCollected = body && LocalHealth && LocalHealth.alive && LocalPlayerInv;
        }

        public static bool InGameCheck()
        {
            return Run.instance && currentScene.IsValid() && currentScene.name != "title" && currentScene.name != "lobby";
        }
        #endregion

        #region Routines
        private void CharacterRoutine()
        {
            bool prevCharCollected = characterCollected;
            GetCharacter();
            if (prevCharCollected != characterCollected && characterCollected && InGameCheck())
            {
                Utility.SoftResetMenu(true);
            }
        }

        private void DevBuildRoutine()
        {
            if (Loader.devBuild)
            {
                if (characterCollected)
                {
                    if (devDoOnce && devMode)
                    {
                        Menus.Player.GodToggle = true;
                        Utility.FindButtonById(1, 9).SetEnabled(true);

                        Menus.Movement.flightToggle = true;
                        Utility.FindButtonById(2, 2).SetEnabled(true);

                        Menus.Movement.alwaysSprintToggle = true;
                        Utility.FindButtonById(2, 1).SetEnabled(true);

                        LocalPlayer.GiveMoney(10000, false);
                        devDoOnce = false;
                    }
                }
            }
        }

        private void ESPRoutine()
        {
            if (characterCollected)
            {
                if (Menus.Render.renderInteractables)
                {
                    Menus.Render.DrawTeleporter();
                    Menus.Render.DrawInteractables();
                    Menus.Render.DrawAdditionalInteractables();
                    Menus.Render.DrawBarrels();
                    Menus.Render.DrawPressurePlates();
                    Menus.Render.DrawScrappers();
                    Menus.Render.renderInteractables = true;
                }
                else
                {
                    Menus.Render.renderInteractables = false;
                }

                if (Menus.Render.renderMobs)
                {
                    Menus.Render.Mobs();
                    Menus.Render.renderMobs = true;
                }
                else
                {
                    Menus.Render.renderMobs = false;
                }

                if (Menus.Render.renderMods)
                {
                    Menus.Render.ActiveMods();
                    Menus.Render.renderMods = true;
                }
                else
                {
                    Menus.Render.renderMods = false;
                }

                if (Menus.Items.chestItemList)
                {
                    Menus.ChestItemList.RenderClosestChest();
                }
            }
        }

        private void EquipCooldownRoutine()
        {
            if (Menus.Items.noEquipmentCD)
            {
                Menus.Items.NoEquipmentCooldown();
            }
        }

        private void SkillsRoutine()
        {
            if (characterCollected)
            {
                if (Menus.Player.SkillToggle)
                {
                    LocalSkills.ApplyAmmoPack();
                }
            }
        }

        private void AimBotRoutine()
        {
            if (characterCollected && Menus.Player.AimBotToggle)
            {
                Menus.Player.AimBot();
            }

        }

        private void GodRoutine()
        {
            if (characterCollected)
            {
                if (Menus.Player.GodToggle)
                {
                    Menus.Player.EnableGodMode();
                }
            }
        }

        private void SprintRoutine()
        {
            if (Menus.Movement.alwaysSprintToggle)
            {
                Menus.Movement.AlwaysSprint();
            }
        }

        private void FlightRoutine()
        {
            if (Menus.Movement.flightToggle)
            {
                Menus.Movement.Flight();
            }
        }

        private void JumpPackRoutine()
        {
            if (Menus.Movement.jumpPackToggle)
            {
                Menus.Movement.JumpPack();
            }
        }

        private void ModStatsRoutine()
        {
            if (characterCollected && (Menus.StatsMod.damageToggle || Menus.StatsMod.critToggle || Menus.StatsMod.attackSpeedToggle || Menus.StatsMod.armorToggle || Menus.StatsMod.moveSpeedToggle))
            {
                if (Menus.StatsMod.damageToggle)
                {
                    Menus.StatsMod.LevelPlayersDamage();
                }
                if (Menus.StatsMod.critToggle)
                {
                    Menus.StatsMod.LevelPlayersCrit();
                }
                if (Menus.StatsMod.attackSpeedToggle)
                {
                    Menus.StatsMod.SetplayersAttackSpeed();
                }
                if (Menus.StatsMod.armorToggle)
                {
                    Menus.StatsMod.SetplayersArmor();
                }
                if (Menus.StatsMod.moveSpeedToggle)
                {
                    Menus.StatsMod.SetplayersMoveSpeed();
                }
                LocalPlayerBody.RecalculateStats();
            }
        }

        private void UpdateMainButtonsRoutine()
        {
            if (InGameCheck() && mainMenu.IsEnabled())
            {
                for (int i = 1; i < menus.Count; i++)
                {
                    if (menus[i].IsEnabled())
                    {
                        menus[i].GetActivatingButton()?.SetEnabled(true);
                    }
                    else
                    {
                        menus[i].GetActivatingButton()?.SetEnabled(false);

                    }
                }
            }
        }

        private void UpdateKeybindButtonRoutine()
        {
            if (InGameCheck() && keybindListMenu.IsEnabled())
            {
                // Keep one capture row active; iterate actual rows, not one-based IDs.
                Button active = Utility.GetEnabledKeybindButton();
                if (active != null)
                    foreach (Button row in keybindListMenu.GetButtons())
                        if (row != active) row.SetEnabled(false);
            }
        }

        private void UpdateNavIndexRoutine()
        {
            if (navigationToggle && !IsItemSearchFocused())
            {
                Navigation.UpdateIndexValues();
            }
        }

        private void SetKeybindRoutine()
        {
            if (listenForKeybind && !IsItemSearchFocused())
            {
                Dictionary<string, string> numKeys = new Dictionary<string, string>()
                {
                    { "0", "Alpha0" },
                    { "1", "Alpha1" },
                    { "2", "Alpha2" },
                    { "3", "Alpha3" },
                    { "4", "Alpha4" },
                    { "5", "Alpha5" },
                    { "6", "Alpha6" },
                    { "7", "Alpha7" },
                    { "8", "Alpha8" },
                    { "9", "Alpha9" },
                };
                Regex rgAlpha = new Regex(@"^[A-Z]$");
                Regex rgNum = new Regex(@"^[0-9]$");
                Button button = Utility.GetEnabledKeybindButton();
                string newKeybind = Input.inputString;
                if (newKeybind.Length > 1)
                {
                    newKeybind = newKeybind.First().ToString().ToUpper() + newKeybind.Substring(1);
                }
                else
                {
                    newKeybind = newKeybind.ToUpper();
                }

                bool validKeybind = newKeybind != "V" && newKeybind != "W" && newKeybind != "A" && newKeybind != "S" && newKeybind != "D" && rgAlpha.IsMatch(newKeybind) && newKeybind != "" && newKeybind != " " && newKeybind != null;
                if (validKeybind)
                {
                    KeyCode keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), newKeybind);
                    if (!Utility.KeyCodeInUse(keyCode))
                    {
                        string keybindName = button.GetOffText().Split(new string[] { " :" }, StringSplitOptions.None)[0];
                        keybindDict[keybindName].KeyCode = keyCode;
                        listenForKeybind = false;
                        button.SetEnabled(false);
                        Utility.SaveSettings();
                        Utility.SoftResetMenu(true);
                    }
                    else
                    {
                        Keybind keybind = Utility.FindKeybindByKeyCode(keyCode);
                        keybind.KeyCode = KeyCode.None;

                        string keybindName = button.GetOffText().Split(new string[] { " :" }, StringSplitOptions.None)[0];
                        keybindDict[keybindName].KeyCode = keyCode;
                        listenForKeybind = false;
                        button.SetEnabled(false);
                        Utility.SaveSettings();
                        Utility.SoftResetMenu(true);
                    }
                }
                else if (!validKeybind && rgNum.IsMatch(newKeybind))
                {
                    newKeybind = numKeys[newKeybind];
                    if (newKeybind != "Alpha0")
                    {
                        string keybindName = button.GetOffText().Split(new string[] { " :" }, StringSplitOptions.None)[0];
                        keybindDict[keybindName].KeyCode = (KeyCode)Enum.Parse(typeof(KeyCode), newKeybind);
                        listenForKeybind = false;
                        button.SetEnabled(false);
                        Utility.SaveSettings();
                        Utility.SoftResetMenu(true);
                    }
                    else
                    {
                        string keybindName = button.GetOffText().Split(new string[] { " :" }, StringSplitOptions.None)[0];
                        keybindDict[keybindName].KeyCode = KeyCode.None;
                        listenForKeybind = false;
                        button.SetEnabled(false);
                        Utility.SaveSettings();
                        Utility.SoftResetMenu(true);
                    }
                }
            }
        }

        private void ForceFullMenuRoutine()
        {
            if (forceFullModMenu == true)
            {
                characterCollected = true;
            }
        }
        #endregion

        #region Inputs
        private static bool IsItemSearchFocused()
        {
            return MenuUI.IsSearchFocused;
        }

        private void CheckInputs()
        {
            if (listenForKeybind)
            {
                Cursor.visible = true;
                if (Input.GetKeyDown(KeyCode.Escape)) MenuUI.CancelKeybindCapture();
                return;
            }
            // Text entry must not activate item actions, menu shortcuts, or chat tracking.
            if (IsItemSearchFocused())
            {
                Cursor.visible = true;
                return;
            }

            if (InGameCheck())
            {
                if (chatOpen && Input.GetKeyDown(KeyCode.Escape))
                {
                    chatOpen = false;
                    msgSent = true;
                }
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (msgSent)
                    {
                        chatOpen = true;
                        msgSent = false;
                    }
                    else
                    {
                        chatOpen = false;
                        msgSent = true;
                    }
                }
            }

            //if (mainMenu.IsEnabled())
            //{
            //    Cursor.visible = true;
            //    if (characterCollected)
            //    {
            //        if (AllowNavigation)
            //        {
            //            if (Input.GetKeyDown(KeyCode.DownArrow))
            //            {
            //                if (!navigationToggle)
            //                {
            //                    Utility.CloseOpenMenus();
            //                }

            //                navigationToggle = true;
            //                Navigation.buttonIndex++;
            //            }
            //            if (Input.GetKeyDown(KeyCode.UpArrow))
            //            {
            //                if (!navigationToggle)
            //                {
            //                    Utility.CloseOpenMenus();
            //                }

            //                navigationToggle = true;
            //                Navigation.buttonIndex--;
            //            }
            //        }
            //    }
            //    if (navigationToggle && AllowNavigation)
            //    {
            //        if (Input.GetKeyDown(KeyCode.V))
            //        {
            //            int oldMenuIndex = Navigation.menuIndex;
            //            Navigation.PressBtn(Navigation.menuIndex, Navigation.buttonIndex);
            //            int newMenuIndex = Navigation.menuIndex;

            //            if (oldMenuIndex != newMenuIndex)
            //            {
            //                Navigation.buttonIndex = 1;
            //            }
            //        }
            //        if (Input.GetKeyDown(KeyCode.RightArrow))
            //        {
            //            bool playerPlusMinusBtn = Navigation.menuIndex == 1 && Enumerable.Range(1, 3).Contains(Navigation.buttonIndex);
            //            bool statsPlusMinusBtn = Navigation.menuIndex == 8 && Enumerable.Range(1, 6).Contains(Navigation.buttonIndex);
            //            bool itemPlusMinusBtn = Navigation.menuIndex == 3 && Enumerable.Range(1, 2).Contains(Navigation.buttonIndex);
            //            bool spawnPlusMinusBtn = Navigation.menuIndex == 4 && Enumerable.Range(1, 3).Contains(Navigation.buttonIndex);
            //            bool settingsPlusMinusBtn = Navigation.menuIndex == 7 && Navigation.buttonIndex == 1;
            //            if (playerPlusMinusBtn || itemPlusMinusBtn || statsPlusMinusBtn || spawnPlusMinusBtn || settingsPlusMinusBtn)
            //            {
            //                Navigation.IncreaseValue(Navigation.menuIndex, Navigation.buttonIndex);
            //            }
            //            else
            //            {
            //                float oldMenuIndex = Navigation.menuIndex;
            //                Navigation.PressBtn(Navigation.menuIndex, Navigation.buttonIndex);
            //                float newMenuIndex = Navigation.menuIndex;

            //                if (oldMenuIndex != newMenuIndex)
            //                {
            //                    Navigation.buttonIndex = 1;
            //                }
            //            }
            //        }
            //        if (Input.GetKeyDown(KeyCode.LeftArrow))
            //        {
            //            bool playerPlusMinusBtn = Navigation.menuIndex == 1 && Enumerable.Range(1, 3).Contains(Navigation.buttonIndex);
            //            bool statsPlusMinusBtn = Navigation.menuIndex == 8 && Enumerable.Range(1, 6).Contains(Navigation.buttonIndex);
            //            bool itemPlusMinusBtn = Navigation.menuIndex == 3 && Enumerable.Range(1, 2).Contains(Navigation.buttonIndex);
            //            bool spawnPlusMinusBtn = Navigation.menuIndex == 4 && Enumerable.Range(1, 3).Contains(Navigation.buttonIndex);
            //            bool settingsPlusMinusBtn = Navigation.menuIndex == 7 && Navigation.buttonIndex == 1;
            //            if (playerPlusMinusBtn || itemPlusMinusBtn || statsPlusMinusBtn || spawnPlusMinusBtn || settingsPlusMinusBtn)
            //            {
            //                Navigation.DecreaseValue(Navigation.menuIndex, Navigation.buttonIndex);
            //            }
            //            else
            //            {
            //                Navigation.GoBackAMenu();
            //            }
            //        }
            //        if (Input.GetKeyDown(KeyCode.Backspace))
            //        {
            //            Navigation.GoBackAMenu();
            //        }

            //        if (buffListMenu.IsEnabled() || characterListMenu.IsEnabled() || chestItemListMenu.IsEnabled() || equipmentListMenu.IsEnabled() || itemListMenu.IsEnabled() || spawnListMenu.IsEnabled() || keybindListMenu.IsEnabled())
            //        {
            //            if (!scrolled)
            //            {
            //                if (Input.GetAxis("Mouse ScrollWheel") != 0f) // Scrolled
            //                {
            //                    scrolled = true;
            //                }
            //            }
            //            else
            //            {
            //                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow))
            //                {
            //                    scrolled = false;
            //                }
            //            }
            //        }
            //    }
            //}

            if (MenuUI.HandleBackShortcut()) return;

            if (mainMenu.IsEnabled())
            {
                Cursor.visible = true;
                if (characterCollected)
                {
                    if (AllowNavigation)
                    {
                        // num2 = move down
                        if (Input.GetKeyDown(KeyCode.Keypad2))
                        {
                            if (!navigationToggle)
                            {
                                Navigation.menuIndex = MenuUI.ActiveMenuId;
                                Navigation.buttonIndex = 0;
                            }

                            navigationToggle = true;
                            Navigation.MoveSelection(1);
                        }

                        // num8 = move up
                        if (Input.GetKeyDown(KeyCode.Keypad8))
                        {
                            if (!navigationToggle)
                            {
                                Navigation.menuIndex = MenuUI.ActiveMenuId;
                                Navigation.buttonIndex = 0;
                            }

                            navigationToggle = true;
                            Navigation.MoveSelection(-1);
                        }
                    }
                }
                if (navigationToggle && AllowNavigation)
                {
                    // num5 = OK / select
                    if (Input.GetKeyDown(KeyCode.Keypad5))
                    {
                        int oldMenuIndex = Navigation.menuIndex;
                        Navigation.PressBtn(Navigation.menuIndex, Navigation.buttonIndex);
                        int newMenuIndex = Navigation.menuIndex;

                        if (oldMenuIndex != newMenuIndex)
                        {
                            Navigation.buttonIndex = 1;
                        }
                    }

                    // Numeric controls advertise their own actions; all steppers work here.
                    Button selected = menus[Navigation.menuIndex].GetButtons()?.FirstOrDefault(row => row.GetId() == Navigation.buttonIndex);
                    if (Input.GetKeyDown(KeyCode.Keypad6))
                    {
                        if (selected?.GetIncreaseAction() != null) Navigation.IncreaseValue(Navigation.menuIndex, Navigation.buttonIndex);
                        else Navigation.PressBtn(Navigation.menuIndex, Navigation.buttonIndex);
                    }
                    if (Input.GetKeyDown(KeyCode.Keypad4))
                    {
                        if (selected?.GetDecreaseAction() != null) Navigation.DecreaseValue(Navigation.menuIndex, Navigation.buttonIndex);
                        else Navigation.GoBackAMenu();
                    }

                    // scroll-list reset using num2/num8 instead of arrows
                    if (teleporterMenu.IsEnabled() || buffListMenu.IsEnabled() || characterListMenu.IsEnabled() || chestItemListMenu.IsEnabled() ||
                        equipmentListMenu.IsEnabled() || itemListMenu.IsEnabled() || spawnListMenu.IsEnabled() ||
                        keybindListMenu.IsEnabled() || inventoryItemListMenu.IsEnabled())
                    {
                        if (!scrolled)
                        {
                            if (Input.GetAxis("Mouse ScrollWheel") != 0f) // Scrolled
                            {
                                scrolled = true;
                            }
                        }
                        else
                        {
                            if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Keypad8))
                            {
                                scrolled = false;
                            }
                        }
                    }
                }
            }


            else if (!mainMenu.IsEnabled())
            {
                navigationToggle = false;
                Navigation.buttonIndex = 0;
                Navigation.menuIndex = 0;
                Cursor.visible = false;
            }

            Keybinds();
        }

        private void Keybinds()
        {
            #region Dev Mode Keybinds
            if (Input.GetKeyDown(KeyCode.F2))
            {
                devMode = !devMode;
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                forceFullModMenu = !forceFullModMenu;
            }
            #endregion

            #region Main Menu Keybind
            if (Input.GetKeyDown(keybindDict["MAIN MENU"].KeyCode) || Input.GetKeyDown(KeyCode.Keypad0))
            {
                mainMenu.ToggleMenu();
                GetCharacter();
            }
            #endregion

            if (!chatOpen && InGameCheck())
            {
                #region Player Menu Keybinds
                if (Input.GetKeyDown(keybindDict["PLAYER MENU"].KeyCode))
                {
                    playerMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 1;
                    }
                }

                if (Input.GetKeyDown(keybindDict["GIVE MONEY"].KeyCode))
                {
                    Menus.Player.GiveMoney();
                }

                if (Input.GetKeyDown(keybindDict["GIVE COINS"].KeyCode))
                {
                    Menus.Player.GiveLunarCoins();
                }

                if (Input.GetKeyDown(keybindDict["GIVE XP"].KeyCode))
                {
                    Menus.Player.GiveXP();
                }

                if (Input.GetKeyDown(keybindDict["STATS MENU"].KeyCode))
                {
                    statsModMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 8;
                    }
                }

                if (Input.GetKeyDown(keybindDict["VIEW STATS MENU"].KeyCode))
                {
                    viewStatsMenu.ToggleMenu();
                }

                if (Input.GetKeyDown(keybindDict["CHANGE CHARACTER MENU"].KeyCode))
                {
                    characterListMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 10;
                    }
                }

                if (Input.GetKeyDown(keybindDict["BUFF MENU"].KeyCode))
                {
                    buffListMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 11;
                    }
                }

                if (Input.GetKeyDown(keybindDict["REMOVE BUFFS"].KeyCode))
                {
                    Utility.FindButtonById(1, 7).GetAction().Invoke();
                }

                if (Input.GetKeyDown(keybindDict["AIMBOT"].KeyCode))
                {
                    Menus.Player.AimBotToggle = !Menus.Player.AimBotToggle;
                    Utility.FindButtonById(1, 8).SetEnabled(Menus.Player.AimBotToggle);
                }

                if (Input.GetKeyDown(keybindDict["GODMODE"].KeyCode))
                {
                    Menus.Player.GodToggle = !Menus.Player.GodToggle;
                    Utility.FindButtonById(1, 9).SetEnabled(Menus.Player.GodToggle);
                }

                if (Input.GetKeyDown(keybindDict["INFINITE SKILLS"].KeyCode))
                {
                    Menus.Player.SkillToggle = !Menus.Player.SkillToggle;
                    Utility.FindButtonById(1, 10).SetEnabled(Menus.Player.SkillToggle);
                }
                #endregion

                #region Movement Menu Keybinds
                if (Input.GetKeyDown(keybindDict["MOVEMENT MENU"].KeyCode))
                {
                    movementMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 2;
                    }
                }

                if (Input.GetKeyDown(keybindDict["ALWAYS SPRINT"].KeyCode))
                {
                    Menus.Movement.alwaysSprintToggle = !Menus.Movement.alwaysSprintToggle;
                    Utility.FindButtonById(2, 1).SetEnabled(Menus.Movement.alwaysSprintToggle);
                }

                if (Input.GetKeyDown(keybindDict["FLIGHT"].KeyCode))
                {
                    Menus.Movement.flightToggle = !Menus.Movement.flightToggle;
                    Utility.FindButtonById(2, 2).SetEnabled(Menus.Movement.flightToggle);
                }

                if (Input.GetKeyDown(keybindDict["JUMP PACK"].KeyCode))
                {
                    Menus.Movement.jumpPackToggle = !Menus.Movement.jumpPackToggle;
                    Utility.FindButtonById(2, 3).SetEnabled(Menus.Movement.jumpPackToggle);
                }
                #endregion

                #region Item Menu Keybinds
                if (Input.GetKeyDown(keybindDict["ITEMS MENU"].KeyCode))
                {
                    itemsMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 3;
                    }
                }

                if (Input.GetKeyDown(keybindDict["GIVE ALL"].KeyCode))
                {
                    Utility.FindButtonById(3, 1).GetAction().Invoke();
                }

                if (Input.GetKeyDown(keybindDict["ROLL ITEMS"].KeyCode))
                {
                    Utility.FindButtonById(3, 3).GetAction().Invoke();
                }

                if (Input.GetKeyDown(keybindDict["ITEMS LIST MENU"].KeyCode))
                {
                    itemListMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 12;
                    }
                }

                if (Input.GetKeyDown(keybindDict["EQUIPMENT LIST MENU"].KeyCode))
                {
                    equipmentListMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 13;
                    }
                }

                if (Input.GetKeyDown(keybindDict["DROP ITEMS"].KeyCode))
                {
                    Utility.FindButtonById(3, Menus.Items.DropItemsButtonId).Activate();
                }

                if (Input.GetKeyDown(keybindDict["DROP INVENTORY ITEMS"].KeyCode))
                {
                    Utility.FindButtonById(3, Menus.Items.DropInventoryButtonId).Activate();
                }

                if (Input.GetKeyDown(keybindDict["INFINITE EQUIPMENT"].KeyCode))
                {
                    Utility.FindButtonById(3, Menus.Items.InfiniteEquipmentButtonId).Activate();
                }

                if (Input.GetKeyDown(keybindDict["STACK INVENTORY"].KeyCode))
                {
                    Utility.FindButtonById(3, Menus.Items.StackButtonId).Activate();
                }

                if (Input.GetKeyDown(keybindDict["CLEAR INVENTORY"].KeyCode))
                {
                    Utility.FindButtonById(3, Menus.Items.ClearButtonId).Activate();
                }

                if (Input.GetKeyDown(keybindDict["CHANGE CHEST ITEM MENU"].KeyCode))
                {
                    chestItemListMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 14;
                    }
                }
                #endregion

                #region Spawn Menu Keybinds
                if (Input.GetKeyDown(keybindDict["SPAWN MENU"].KeyCode))
                {
                    spawnMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 4;
                    }
                }

                if (Input.GetKeyDown(keybindDict["SPAWN LIST MENU"].KeyCode))
                {
                    spawnListMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 15;
                    }
                }

                if (Input.GetKeyDown(keybindDict["KILL ALL"].KeyCode))
                {
                    Utility.FindButtonById(4, 5).GetAction().Invoke();
                }

                if (Input.GetKeyDown(keybindDict["DESTROY INTERACTABLES"].KeyCode))
                {
                    Utility.FindButtonById(4, 6).GetAction().Invoke();
                }
                #endregion

                #region Teleport Menu Keybinds 
                if (Input.GetKeyDown(keybindDict["TELEPORTER MENU"].KeyCode))
                {
                    teleporterMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 5;
                    }
                }

                if (Input.GetKeyDown(keybindDict["SKIP STAGE"].KeyCode))
                {
                    Utility.FindButtonById(5, 1).GetAction().Invoke();
                }

                if (Input.GetKeyDown(keybindDict["INSTANT TELE CHARGE"].KeyCode))
                {
                    Utility.FindButtonById(5, 2).GetAction().Invoke();
                }

                if (Input.GetKeyDown(keybindDict["ADD MOUNTAIN STACK"].KeyCode))
                {
                    Utility.FindButtonById(5, 3).GetAction().Invoke();
                }

                if (Input.GetKeyDown(keybindDict["SPAWN ALL PORTALS"].KeyCode))
                {
                    Utility.FindButtonById(5, 4).GetAction().Invoke();
                }

                if (Input.GetKeyDown(keybindDict["SPAWN BLUE PORTAL"].KeyCode))
                {
                    ((Menus.Teleporter)teleporterMenu).SpawnPortals("blue");
                }

                if (Input.GetKeyDown(keybindDict["SPAWN CELE PORTAL"].KeyCode))
                {
                    ((Menus.Teleporter)teleporterMenu).SpawnPortals("cele");
                }

                if (Input.GetKeyDown(keybindDict["SPAWN GOLD PORTAL"].KeyCode))
                {
                    ((Menus.Teleporter)teleporterMenu).SpawnPortals("gold");
                }
                #endregion

                #region Render Menu Keybinds
                if (Input.GetKeyDown(keybindDict["RENDER MENU"].KeyCode))
                {
                    renderMenu.ToggleMenu();
                    if (navigationToggle && AllowNavigation)
                    {
                        Navigation.menuIndex = 6;
                    }
                }

                if (Input.GetKeyDown(keybindDict["RENDER ACTIVE MODS"].KeyCode))
                {
                    Menus.Render.renderMods = !Menus.Render.renderMods;
                    Utility.FindButtonById(6, 1).SetEnabled(Menus.Render.renderMods);
                }

                if (Input.GetKeyDown(keybindDict["RENDER INTERACTABLES"].KeyCode))
                {
                    Menus.Render.ToggleRenderInteractablesKey();
                    Utility.FindButtonById(6, 2).SetEnabled(Menus.Render.renderInteractables);
                }

                if (Input.GetKeyDown(keybindDict["RENDER MOB ESP"].KeyCode))
                {
                    Menus.Render.renderMobs = !Menus.Render.renderMobs;
                    Utility.FindButtonById(6, 3).SetEnabled(Menus.Render.renderMobs);
                }
                #endregion
            }
        }

        public static Dictionary<string, Keybind> BuildKeybinds()
        {
            Dictionary<string, Keybind> keybinds = new Dictionary<string, Keybind>();
            for (int i = 0; i < keyBindNames.Count; i++)
            {
                Keybind keybind = new Keybind(keyBindNames[i], (KeyCode)Enum.Parse(typeof(KeyCode), Settings[i + 3]));
                keybinds.Add(keyBindNames[i], keybind);
            }
            return keybinds;
        }
        #endregion Inputs
    }
}
