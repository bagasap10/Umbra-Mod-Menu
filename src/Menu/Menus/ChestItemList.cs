using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public class ChestItemList : Menu
    {
        private static readonly SearchableListMenu chestItemsList = new SearchableListMenu(14, new Rect(1503, 10, 20, 20),
            "CHEST ITEMS MENU", "Search item or equipment name...");

        public static List<ChestBehavior> chests = new();
        public static bool onChestsEnable = true;
        private static bool isClosestChestEquip;
        public static ChestBehavior selectedChest;

        public static bool IsClosestChestEquip
        {
            get => isClosestChestEquip;
            set
            {
                if (isClosestChestEquip == value)
                    return;

                isClosestChestEquip = value;
                // Retain the query when switching between item and equipment chests.
                chestItemsList.CurrentScrollPosition = Vector2.zero;
                if (UmbraMenu.navigationToggle && Navigation.menuIndex == chestItemsList.Id)
                {
                    Navigation.buttonIndex = 1;
                    UmbraMenu.scrolled = false;
                }
                UpdateChestListMenu();
            }
        }

        public ChestItemList() : base(chestItemsList)
        {
            ConfigureSearch(_ => RefreshPickups());
            if (UmbraMenu.characterCollected)
            {
                RefreshPickups();
                SetActivatingButton(Utility.FindButtonById(3, Items.ChestButtonId));
                SetPrevMenuId(3);
            }
        }

        private void RefreshPickups()
        {
            List<Button> buttons = isClosestChestEquip
                ? CatalogGrouping.BuildEquipmentButtons(this, UmbraMenu.equipment, SetChestEquipment, SearchText)
                : CatalogGrouping.BuildItemButtons(this, UmbraMenu.items, SetChestItem, searchText: SearchText);
            if (buttons.Count == 0)
            {
                string message = isClosestChestEquip ? "No equipment matches your search." : "No items match your search.";
                buttons.Add(new Button(new DividerButton(this, 1, message, new Color32(190, 190, 190, 255), false)));
            }
            AddButtons(buttons);
            if (UmbraMenu.navigationToggle && Navigation.menuIndex == GetId())
                Navigation.UpdateIndexValues();
        }

        public static void UpdateChestListMenu()
        {
            (UmbraMenu.chestItemListMenu as ChestItemList)?.RefreshPickups();
        }

        public override void SetEnabled(bool value)
        {
            if (value != IsEnabled())
            {
                if (value)
                {
                    EnableChests();
                    LockClosestChest();
                    IsClosestChestEquip = CheckClosestChestEquip();
                    RefreshPickups();
                }
                else
                {
                    DisableChests();
                }
                Items.chestItemList = value;
            }
            base.SetEnabled(value);
        }

        public static void LockClosestChest()
        {
            selectedChest = FindClosestChest();
        }

        public static ChestBehavior GetSelectedChest()
        {
            if (selectedChest && selectedChest.currentPickup.pickupIndex != PickupIndex.none)
                return selectedChest;

            selectedChest = FindClosestChest();
            return selectedChest;
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
            isClosestChestEquip = false;
            DisableChests();
            Items.chestItemList = false;
            base.Reset();
        }

        public static void EnableChests()
        {
            if (!onChestsEnable)
                return;

            DumpInteractables(null);
            SceneDirector.onPostPopulateSceneServer += DumpInteractables;
            onChestsEnable = false;
        }

        public static void DisableChests()
        {
            if (!onChestsEnable)
                SceneDirector.onPostPopulateSceneServer -= DumpInteractables;
            onChestsEnable = true;
            selectedChest = null;
            chests.Clear();
        }

        private static void DumpInteractables(SceneDirector obj)
        {
            selectedChest = null;
            chests = Object.FindObjectsOfType<ChestBehavior>().ToList();
        }

        public static ChestBehavior FindClosestChest()
        {
            Camera camera = Camera.main;
            if (!camera)
                return null;

            ChestBehavior closest = null;
            float leastDistance = float.PositiveInfinity;
            Vector3 origin = camera.transform.position;
            foreach (ChestBehavior chest in chests)
            {
                if (!chest || chest.currentPickup.pickupIndex == PickupIndex.none)
                    continue;

                float distance = (origin - chest.transform.position).sqrMagnitude;
                if (distance < leastDistance)
                {
                    leastDistance = distance;
                    closest = chest;
                }
            }
            return closest;
        }

        public static void RenderClosestChest()
        {
            ChestBehavior chest = GetSelectedChest();
            Camera camera = Camera.main;
            if (!chest || !camera)
                return;

            var pickupDef = PickupCatalog.GetPickupDef(chest.currentPickup.pickupIndex);
            if (pickupDef == null)
                return;

            Vector3 chestPosition = camera.WorldToScreenPoint(chest.transform.position);
            if (chestPosition.z > 0.01f)
            {
                string dropNameColored = Util.GenerateColoredString(Language.GetString(pickupDef.nameToken), pickupDef.baseColor);
                float distanceToChest = Vector3.Distance(camera.transform.position, chest.transform.position);
                float size = Mathf.Min(distanceToChest, 125f);

                GUI.Label(
                    new Rect(chestPosition.x - 50f, Screen.height - chestPosition.y + 35f, 100f, 50f),
                    Render.renderInteractables ? "Selected Chest" : $"Selected Chest\n{dropNameColored}",
                    Styles.SelectedChestStyle
                );
                ESPHelper.DrawBox(chestPosition.x - size / 2f, Screen.height - chestPosition.y - size / 2f,
                    size, size, new Color32(17, 204, 238, 255));
            }
        }

        public static void SetChestItem(ItemIndex itemIndex)
        {
            SetChestPickup(PickupCatalog.FindPickupIndex(itemIndex));
        }

        public static void SetChestEquipment(EquipmentIndex equipmentIndex)
        {
            SetChestPickup(PickupCatalog.FindPickupIndex(equipmentIndex));
        }

        private static void SetChestPickup(PickupIndex pickupIndex)
        {
            if (pickupIndex == PickupIndex.none)
                return;

            var chest = GetSelectedChest();
            if (!chest)
                return;

            // currentPickup is read-only; use the existing cached reflection path.
            chest.SetField("<currentPickup>k__BackingField", new UniquePickup(pickupIndex));
        }

        public static bool CheckClosestChestEquip()
        {
            var chest = GetSelectedChest();
            if (!chest)
                return false;

            var pickupDef = PickupCatalog.GetPickupDef(chest.currentPickup.pickupIndex);
            return pickupDef != null && pickupDef.equipmentIndex != EquipmentIndex.None;
        }
    }
}
