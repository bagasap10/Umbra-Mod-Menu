using System;
using UnityEngine;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UmbraMenu.monsoon;

namespace UmbraMenu.Menus
{
    public class Render : Menu
    {
        private static readonly IMenu render = new NormalMenu(6, new Rect(10, 795, 20, 20), "RENDER MENU");

        private static Camera camera;

        public static List<PurchaseInteraction> purchaseInteractions = new List<PurchaseInteraction>();
        public static List<BarrelInteraction> barrelInteractions = new List<BarrelInteraction>();
        public static List<PressurePlateController> secretButtons = new List<PressurePlateController>();
        public static List<ScrapperController> scrappers = new List<ScrapperController>();
        public static List<CharacterBody> enemyBodies = new List<CharacterBody>();
        private static List<EspTargets.Interactable> additionalInteractables = new List<EspTargets.Interactable>();
        private static float nextInteractableRefresh;
        public static bool scenePopulateToggle = true, renderMobs, renderInteractables, renderMods = true;

        public Button toggleActiveMods;
        public Button toggleInteractESP;
        public Button toggleMobESP;

        public Render() : base(render)
        {
            if (UmbraMenu.characterCollected)
            {
                if (UmbraMenu.lowResolutionMonitor)
                {
                    toggleActiveMods = new Button(new TogglableButton(this, 1, "ACTIVE MODS : OFF", "ACTIVE MODS : ON", ToggleRenderMods, ToggleRenderMods));
                }
                else
                {
                    toggleActiveMods = new Button(new TogglableButton(this, 1, "ACTIVE MODS : OFF", "ACTIVE MODS : ON", ToggleRenderMods, ToggleRenderMods, true));
                }
                toggleInteractESP = new Button(new TogglableButton(this, 2, "INTERACTABLES ESP : OFF", "INTERACTABLES ESP : ON", ToggleRenderInteractables, ToggleRenderInteractables));
                toggleMobESP = new Button(new TogglableButton(this, 3, "MOB ESP : OFF", "MOB ESP : ON", ToggleRenderMobs, ToggleRenderMobs));

                AddButtons(new List<Button>()
                {
                    toggleActiveMods,
                    toggleInteractESP,
                    toggleMobESP
                });
                SetActivatingButton(Utility.FindButtonById(0, 6));
                SetPrevMenuId(0);
            }
        }

        public override void Draw()
        {
            if (IsEnabled())
            {
                SetWindow();
                base.Draw();
            }
        }

        public override void Reset()
        {
            renderMobs = false;
            renderInteractables = false;
            // Add check here later for low resolution monitors
            renderMods = true;
            DisableInteractables();
            base.Reset();
        }

        public static void RefreshInteractables(SceneDirector obj)
        {
            try { RefreshInteractables(); }
            catch (Exception error) { Debug.LogWarning("Umbra: interactable scene refresh failed: " + error); Loader.Report("degraded", "Interactable scene refresh failed. See Player.log."); }
        }
        public static void RefreshInteractables()
        {
            nextInteractableRefresh = Time.unscaledTime + 1f;
            MonoBehaviour[] components = MonoBehaviour.FindObjectsOfType<MonoBehaviour>();
            barrelInteractions = components.OfType<BarrelInteraction>().ToList();
            purchaseInteractions = components.OfType<PurchaseInteraction>().ToList();
            secretButtons = components.OfType<PressurePlateController>().ToList();
            scrappers = components.OfType<ScrapperController>().ToList();
            additionalInteractables = EspTargets.GetAdditionalInteractables(components);
        }

        public static void RefreshInteractablesIfNeeded()
        {
            if (renderInteractables && UmbraMenu.InGameCheck() && Time.unscaledTime >= nextInteractableRefresh)
                RefreshInteractables();
        }
        private void ToggleRenderInteractables()
        {

            if (renderInteractables)
            {
                DisableInteractables();
            }
            else
            {
                EnableInteractables();
                Render.RefreshInteractables();
            }
            renderInteractables = !renderInteractables;
        }
        public static void ToggleRenderInteractablesKey()
        {

            if (renderInteractables)
            {
                DisableInteractables();
            }
            else
            {
                EnableInteractables();
                Render.RefreshInteractables();
            }
            renderInteractables = !renderInteractables;
        }

        private void ToggleRenderMods()
        {
            renderMods = !renderMods;
        }

        private void ToggleRenderMobs()
        {
            renderMobs = !renderMobs;
        }

        public static void EnableInteractables()
        {
            if (scenePopulateToggle)
            {
                SceneDirector.onPostPopulateSceneServer += RefreshInteractables;
                scenePopulateToggle = false;
            }
        }

        public static void DisableInteractables()
        {
            if (!scenePopulateToggle)
            {
                SceneDirector.onPostPopulateSceneServer -= RefreshInteractables;
                scenePopulateToggle = true;
            }
            purchaseInteractions.Clear();
            barrelInteractions.Clear();
            secretButtons.Clear();
            scrappers.Clear();
            additionalInteractables.Clear();
            nextInteractableRefresh = 0;
        }
        public static void DrawTeleporter() {
            camera = Camera.main;
            if (!camera)
                return;

            if (TeleporterInteraction.instance)
            {
                var teleporterInteraction = TeleporterInteraction.instance;
                float distanceToObject = Vector3.Distance(camera.transform.position, teleporterInteraction.transform.position);
                Vector3 Position = camera.WorldToScreenPoint(teleporterInteraction.transform.position);
                var BoundingVector = new Vector3(Position.x, Position.y, Position.z);
                if (BoundingVector.z > 0.01)
                {
                    int distance = (int)distanceToObject;
                    string friendlyName = "Teleporter";
                    string status = "" + (
                        teleporterInteraction.isIdle ? "Idle" :
                        teleporterInteraction.isCharging ? "Charging" :
                        teleporterInteraction.isCharged ? "Charged" :
                        teleporterInteraction.isActiveAndEnabled ? "Idle" :
                        teleporterInteraction.isIdleToCharging ? "Idle-Charging" :
                        teleporterInteraction.isInFinalSequence ? "Final-Sequence" :
                        "???");
                    string boxText = $"{friendlyName}\n{status}\n{distance}m";
                    monsoon.Renderer.DrawString(new Vector2(BoundingVector.x, (float)Screen.height - BoundingVector.y),
                        boxText, Styles.TeleporterStyle);
                }
            }
        }

        public static void DrawInteractables()
        {
            camera = Camera.main;
            if (!camera)
                return;
            for (int i = 0; i < purchaseInteractions.Count; i++)
            {
                PurchaseInteraction purchaseInteraction = purchaseInteractions[i];

                if (purchaseInteraction && purchaseInteraction.isActiveAndEnabled && purchaseInteraction.available)
                {
                    string dropName = null;
                    var chest = purchaseInteraction?.gameObject.GetComponent<ChestBehavior>();
                    if (chest)
                    {
                        var unique = chest.currentPickup;
                        PickupIndex index = unique.pickupIndex;
                        PickupDef def = PickupCatalog.GetPickupDef(index);

                        if (def != null)
                            dropName = Util.GenerateColoredString(Language.GetString(def.nameToken), def.baseColor);
                        //dropName = Util.GenerateColoredString(Language.GetString(chest.dropPickup.GetPickupNameToken()), chest.dropPickup.GetPickupColor());
                    }

                    float distanceToObject = Vector3.Distance(camera.transform.position, purchaseInteraction.transform.position);
                    Vector3 Position = camera.WorldToScreenPoint(purchaseInteraction.transform.position);
                    var BoundingVector = new Vector3(Position.x, Position.y, Position.z);

                    if (BoundingVector.z > 0.01)
                    {
                        int distance = (int)distanceToObject;
                        string friendlyName = purchaseInteraction.GetDisplayName();
                        int cost = purchaseInteraction.cost;

                        // Handle interactables with no cost
                        bool noCost = purchaseInteraction.cost <= 0;

                        // Special case: Void Cradle / Void Potential
                        if (friendlyName.Contains("Void Cradle"))
                        {
                            dropName = "Random Void Item";
                        }
                        if (friendlyName.Contains("Void Potential"))
                        {
                            dropName = "Choose 1 of 3 Void Items";
                        }

                        // Cauldrons (Item exchange) 
                        if (friendlyName.Contains("Cauldron"))
                        {
                            dropName = "Item Conversion";
                        }

                        // Timed Security Chest
                        if (friendlyName.Contains("Timed"))
                        {
                            dropName = "Requires Time Lock";
                        }

                        // Drone Combiner
                        if (friendlyName.Contains("Combiner"))
                        {
                            dropName = "Combine 2 Drones";
                        }

                        // Drone Scrapper
                        if (friendlyName.Contains("Drone Scrapper"))
                        {
                            dropName = "Convert Drone → Scrap";
                        }

                        //string boxText;
                        //if (friendlyName.Contains("Mountain") || friendlyName.Contains("Combat") || friendlyName.Contains("Printer"))
                        //{
                        //    boxText = $"{friendlyName}\n{distance}m";
                        //}
                        //else
                        //{
                        //    boxText = dropName != null 
                        //        ? $"{friendlyName}\n{BuyingUnit(friendlyName, cost)}\n{distance}m\n{dropName}" 
                        //        : $"{friendlyName}\n{BuyingUnit(friendlyName, cost)}\n{distance}m";
                        //}

                        string boxText;

                        if (dropName != null)
                        {
                            boxText = $"{friendlyName}\n{BuyingUnit(friendlyName, cost)}\n{distance}m\n{dropName}";
                        }
                        else if (noCost)
                        {
                            boxText = $"{friendlyName}\n{distance}m";
                        }
                        else
                        {
                            boxText = $"{friendlyName}\n{BuyingUnit(friendlyName, cost)}\n{distance}m";
                        }

                        monsoon.Renderer.DrawString(new Vector2(BoundingVector.x, (float)Screen.height - BoundingVector.y),
                            boxText, ChooseStyle(friendlyName));
                    }
                }

            }
        }

        public static void DrawBarrels()
        {
            if (!camera)
                return;
            for (int i = 0; i < barrelInteractions.Count; i++)
            {
                BarrelInteraction barrel = barrelInteractions[i];

                if (barrel && !barrel.Networkopened)
                {
                    string friendlyName = "Barrel";
                    Vector3 Position = camera.WorldToScreenPoint(barrel.transform.position);
                    var BoundingVector = new Vector3(Position.x, Position.y, Position.z);
                    if (BoundingVector.z > 0.01)
                    {
                        float distance = (int)Vector3.Distance(camera.transform.position, barrel.transform.position);
                        string boxText = $"{friendlyName}\n{distance}m";
                        monsoon.Renderer.DrawString(new Vector2(BoundingVector.x, Screen.height - BoundingVector.y),
                            boxText, ChooseStyle(friendlyName));
                    }
                }
            }
        }

        public static void DrawPressurePlates()
        {
            if (!camera)
                return;
            for (int i = 0; i < secretButtons.Count; i++)
            {
                PressurePlateController secretButton = secretButtons[i];
                if (secretButton)
                {
                    string friendlyName = "Secret Button";
                    Vector3 Position = camera.WorldToScreenPoint(secretButton.transform.position);
                    var BoundingVector = new Vector3(Position.x, Position.y, Position.z);
                    if (BoundingVector.z > 0.01)
                    {
                        float distance = (int)Vector3.Distance(camera.transform.position, secretButton.transform.position);
                        string boxText = $"{friendlyName}\n{distance}m";
                        monsoon.Renderer.DrawString(new Vector2(BoundingVector.x, (float)Screen.height - BoundingVector.y),
                            boxText, ChooseStyle(friendlyName));
                    }
                }
            }
        }

        public static void DrawScrappers()
        {
            if (!camera)
                return;
            for (int i = 0; i < scrappers.Count; i++)
            {
                ScrapperController scrapper = scrappers[i];
                if (scrapper)
                {
                    string friendlyName = "Scrapper";
                    Vector3 Position = camera.WorldToScreenPoint(scrapper.transform.position);
                    var BoundingVector = new Vector3(Position.x, Position.y, Position.z);
                    if (BoundingVector.z > 0.01)
                    {
                        float distance = (int)Vector3.Distance(camera.transform.position, scrapper.transform.position);
                        string boxText = $"{friendlyName}\n{distance}m";
                        monsoon.Renderer.DrawString(new Vector2(BoundingVector.x, (float)Screen.height - BoundingVector.y),
                            boxText, ChooseStyle(friendlyName));
                    }
                }
            }
        }

        //private static string BuyingUnit(string name, int cost)
        //{
        //    if (name.Contains("Newt") || name.Contains("Lunar") || name.Contains("Order") || name.Contains("Slab"))
        //        return cost + "LC";
        //    else if (name.Contains("Void") || name.Contains("Blood"))
        //        return cost + "%HP";
        //    return "$" + cost;
        //}

        private static string BuyingUnit(string name, int cost)
        {
            if (cost <= 0) return "-";

            if (name.Contains("Lunar") ||
                name.Contains("Newt") ||
                name.Contains("Order") ||
                name.Contains("Cleansing"))
                return $"{cost} LC";

            if (name.Contains("Void Cradle") ||
                name.Contains("Void Potential") ||
                name.Contains("Blood"))
                return $"{cost}% HP";

            if (name.Contains("Rusted") || name.Contains("Lockbox"))
                return "Rusted Key";

            if (name.Contains("Encrusted"))
                return "Encrusted Key";

            if (name.Contains("Fuel Array"))
                return "Fuel Array";

            if (name.Contains("Max HP"))
                return $"{cost}% HP";

            return $"${cost}";
        }

        private static bool IsVoidInteractable(string name)
        {
            return name.Contains("Void") ||
                   name.Contains("Cradle") ||
                   name.Contains("Potential") ||
                   name.Contains("Deep Void") ||
                   name.Contains("Cell Vent") ||
                   name.Contains("Assessment Focus");
        }

        private static bool IsPrinter(string name)
        {
            return name.Contains("Printer") ||
                   name.Contains("3D") ||
                   name.Contains("Overgrown") ||
                   name.Contains("Mili-Tech");
        }

        private static bool IsDrone(string name)
        {
            return name.Contains("Drone") ||
                   name.Contains("Turret") ||
                   name.Contains("TC-280") ||
                   name.Contains("Vulture Nest") ||
                   name.Contains("Jailer");
        }

        private static bool IsShrine(string name)
        {
            return name.Contains("Shrine") ||
                   name.Contains("Cleansing Pool") ||
                   name.Contains("Altar") ||
                   name.Contains("Beacon") ||
                   name.Contains("Halcyon Shrine");
        }

        private static bool IsPortal(string name)
        {
            return name.Contains("Portal") ||
                   name.Contains("Seer") ||
                   name.Contains("Quantum Tunnel") ||
                   name.Contains("Gateway");
        }

        private static bool IsEnvironment(string name)
        {
            return name.Contains("Access Node") ||
                   name.Contains("Pressure Plate") ||
                   name.Contains("Fan") ||
                   name.Contains("Cauldron") ||
                   name.Contains("Lunar Bud") ||
                   name.Contains("Generator") ||
                   name.Contains("Aurelionite") ||
                   name.Contains("Stalk") ||
                   name.Contains("Timed Security");
        }

        //private static GUIStyle ChooseStyle(string name)
        //{
        //    if (name.Contains("Newt"))
        //        return Styles.NewtStyle;
        //    else if (name.Contains("Chest") || name.Contains("Multishop") || name.Contains("Printer"))
        //        return Styles.ChestStyle;
        //    else if (name.Contains("Equipment"))
        //        return Styles.EquipmentStyle;
        //    else if (name.Contains("Lunar"))
        //        return Styles.LunarStyle;
        //    else if (name.Contains("Void"))
        //        return Styles.VoidStyle;
        //    else if (name.Contains("Gold") || name.Contains("Chance"))
        //        return Styles.ChanceStyle;
        //    else if (name.Contains("Blood"))
        //        return Styles.BloodStyle;
        //    else if (name.Contains("Combat") || name.Contains("Order"))
        //        return Styles.CombatStyle;
        //    else if (name.Contains("Mountain"))
        //        return Styles.MountainStyle;
        //    else if (name.Contains("Woods"))
        //        return Styles.WoodsStyle;
        //    return Styles.DefaultStyle;
        //}

        private static GUIStyle ChooseStyle(string name)
        {
            if (IsPortal(name))
                return Styles.PortalStyle;

            if (IsShrine(name))
                return Styles.ShrineStyle;

            if (IsVoidInteractable(name))
                return Styles.VoidStyle;

            if (IsPrinter(name))
                return Styles.PrinterStyle;

            if (IsDrone(name))
                return Styles.DroneStyle;

            if (IsEnvironment(name))
                return Styles.EnvironmentStyle;

            if (name.Contains("Chest") ||
                name.Contains("Lockbox") ||
                name.Contains("Cache") ||
                name.Contains("Terminal"))
                return Styles.ChestStyle;

            if (name.Contains("Equipment"))
                return Styles.EquipmentStyle;

            if (name.Contains("Lunar"))
                return Styles.LunarStyle;

            if (name.Contains("Chance"))
                return Styles.ChanceStyle;

            return Styles.DefaultStyle;
        }

        public static void Mobs()
        {
            camera = Camera.main;
            if (!camera)
                return;
            foreach (CharacterBody mob in enemyBodies)
            {
                if (!mob || !mob.healthComponent || !mob.healthComponent.alive
                    || !EspTargets.TryGetScreenRect(camera, EspTargets.GetBodyBounds(mob), out Rect box))
                    continue;

                int distance = (int)Vector3.Distance(camera.transform.position, mob.corePosition);
                string name = mob.GetDisplayName();
                GUI.Label(new Rect(box.x, box.yMax + 2, Mathf.Max(150, box.width), 50), $"{name}\n{distance}m", Styles.RenderMobsStyle);
                ESPHelper.DrawBox(box.x, box.y, box.width, box.height, new Color32(255, 0, 0, 255));
            }
        }

        public static void DrawAdditionalInteractables()
        {
            if (!camera)
                return;
            Interactor interactor = UmbraMenu.LocalPlayerBody ? UmbraMenu.LocalPlayerBody.GetComponent<Interactor>() : null;
            if (!interactor)
                return;
            HashSet<int> drawnObjects = new HashSet<int>();
            foreach (EspTargets.Interactable target in additionalInteractables)
            {
                if (!target.Component || !target.Component.isActiveAndEnabled)
                    continue;
                PurchaseInteraction purchase = target.Component.GetComponent<PurchaseInteraction>();
                if (purchase && purchase.isActiveAndEnabled && purchase.available)
                    continue;
                Interactability state = target.Interaction.GetInteractability(interactor);
                if (state == Interactability.Disabled)
                    continue;
                Vector3 position = camera.WorldToScreenPoint(target.Component.transform.position);
                if (position.z <= camera.nearClipPlane)
                    continue;
                if (!drawnObjects.Add(target.Component.gameObject.GetInstanceID()))
                    continue;

                string name = target.NameProvider?.GetDisplayName();
                if (string.IsNullOrWhiteSpace(name))
                    name = target.Interaction.GetContextString(interactor);
                if (string.IsNullOrWhiteSpace(name))
                    name = target.Component.gameObject.name.Replace("(Clone)", "");
                int distance = (int)Vector3.Distance(camera.transform.position, target.Component.transform.position);
                string status = state == Interactability.ConditionsNotMet ? "\nConditions not met" : "";
                monsoon.Renderer.DrawString(new Vector2(position.x, Screen.height - position.y), $"{name}{status}\n{distance}m", ChooseStyle(name));
            }
        }

        public static void ActiveMods()
        {
            List<string> modsActive = new List<string>();
            Dictionary<string, bool> allMods = new Dictionary<string, bool>()
            {
                { "Aimbot", Menus.Player.AimBotToggle },
                { "Always-Sprint", Menus.Movement.alwaysSprintToggle },
                { "Drop-Items", Items.isDropItemForAll },
                { "Drop-Items-from-Inventory", Items.isDropItemFromInventory },
                { "Flight", Menus.Movement.flightToggle },
                { "God-Mode", Menus.Player.GodToggle },
                { "Jump-Pack", Menus.Movement.jumpPackToggle },
                { "Keyboard-Navigation", UmbraMenu.navigationToggle },
                { "Modified-Armor", StatsMod.armorToggle },
                { "Modified-Attack Speed", StatsMod.attackSpeedToggle },
                { "Modified-Crit", StatsMod.critToggle },
                { "Modified-Damage", StatsMod.damageToggle },
                { "Modified-Move-Speed", StatsMod.moveSpeedToggle },
                { "Modified-Regen", StatsMod.regenToggle },
                { "No-Equipment-Cooldown", Items.noEquipmentCD },
                { "No-Skill-Cooldowns", Menus.Player.SkillToggle },
                { "Render-Interactables", renderInteractables },
                { "Render-Mobs", renderMobs }
            };

            string modsBoxText = "";
            Vector2 bottom = new Vector2(0, Screen.height);

            foreach (string modName in allMods.Keys)
            {
                allMods.TryGetValue(modName, out bool active);
                if (active)
                {
                    modsActive.Add(modName);
                }
                else if (modsActive.Contains(modName))
                {
                    modsActive.Remove(modName);
                }
            }

            if (modsActive != null)
            {
                for (int i = 0; i < modsActive.Count; i++)
                {
                    //modsBoxText += $"{modsActive[i]}\n";
                    modsBoxText += $"{modsActive[i]}    ";
                }

                if (modsBoxText != "")
                {
                    GUI.Label(new Rect(Screen.width / 16, bottom.y - 55f, 200, 50f), "Active Mods: ", Styles.ActiveModsStyle);
                    GUI.Label(new Rect((Screen.width / 16) + 124, bottom.y - 55f, Screen.width - (Screen.width / 6), 50f), modsBoxText, Styles.ActiveModsStyle);
                }
            }
        }
    }
}
