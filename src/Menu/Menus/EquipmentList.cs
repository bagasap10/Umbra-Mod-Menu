using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public class EquipmentList : Menu
    {
        private static readonly IMenu equipmentList = new SearchableListMenu(13, new Rect(1503, 10, 20, 20), "EQUIPMENT MENU", "Search equipment name...");

        public EquipmentList() : base(equipmentList)
        {
            ConfigureSearch(_ => RefreshEquipment());
            if (UmbraMenu.characterCollected)
            {
                RefreshEquipment();
                SetActivatingButton(Utility.FindButtonById(3, Items.SpawnEquipmentButtonId));
                SetPrevMenuId(3);
            }
        }

        private void RefreshEquipment()
        {
            List<Button> buttons = CatalogGrouping.BuildEquipmentButtons(this, UmbraMenu.equipment, GiveEquipment, SearchText);
            if (buttons.Count == 0)
            {
                buttons.Add(new Button(new DividerButton(this, 1, "No equipment matches your search.", new Color32(190, 190, 190, 255), false)));
            }
            AddButtons(buttons);
            if (UmbraMenu.navigationToggle && Navigation.menuIndex == GetId())
                Navigation.UpdateIndexValues();
        }

        public override void Draw()
        {
            if (IsEnabled())
            {
                SetWindow();
                base.Draw();
            }
        }

        private void GiveEquipment(EquipmentIndex equipIndex)
        {
            var localUser = LocalUserManager.GetFirstLocalUser();
            if (localUser != null && localUser.cachedMasterController && localUser.cachedMasterController.master && UmbraMenu.LocalPlayerInv)
            {
                if (Items.isDropItemForAll)
                {
                    Items.DropEquipmentMethod(equipIndex);
                }
                else if (Items.isDropItemFromInventory)
                {
                    if (UmbraMenu.LocalPlayerInv.currentEquipmentIndex == equipIndex)
                    {
                        //UmbraMenu.LocalPlayerInv.SetEquipmentIndex(EquipmentIndex.None, new EquipmentLocation { slot = (uint)UmbraMenu.LocalPlayerInv.activeEquipmentSlot, set = 0 });

                        // Removing equipment → use isRemovingEquipment = true
                        UmbraMenu.LocalPlayerInv.SetEquipmentIndex(EquipmentIndex.None, new EquipmentLocation { slot = (uint)UmbraMenu.LocalPlayerInv.activeEquipmentSlot, set = 0 });
                        Items.DropEquipmentMethod(equipIndex);
                    }
                    else
                    {
                        Chat.AddMessage($"<color=yellow> You do not have that equipment and therefore cannot drop it.</color>");
                        Chat.AddMessage($" ");
                    }
                }
                else
                {
                    //UmbraMenu.LocalPlayerInv.SetEquipmentIndex(equipIndex, new EquipmentLocation { slot = (uint)UmbraMenu.LocalPlayerInv.activeEquipmentSlot, set = 0 });
                    UmbraMenu.LocalPlayerInv.SetEquipmentIndex(equipIndex, new EquipmentLocation { slot = (uint)UmbraMenu.LocalPlayerInv.activeEquipmentSlot, set = 0 });
                }
            }
        }
    }
}
