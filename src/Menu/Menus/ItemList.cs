using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public class ItemList : Menu
    {
        private static readonly IMenu itemsList = new SearchableListMenu(12, new Rect(1503, 10, 20, 20), "ITEMS MENU");

        public ItemList() : base(itemsList)
        {
            ConfigureSearch(_ => RefreshItems());
            if (UmbraMenu.characterCollected)
            {
                RefreshItems();
                SetActivatingButton(Utility.FindButtonById(3, Items.SpawnItemsButtonId));
                SetPrevMenuId(3);
            }
        }

        private void RefreshItems()
        {
            List<Button> buttons = CatalogGrouping.BuildItemButtons(this, UmbraMenu.items, GiveItem, searchText: SearchText);
            if (buttons.Count == 0)
            {
                buttons.Add(new Button(new DividerButton(this, 1, "No items match your search.", new Color32(190, 190, 190, 255), false)));
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

        public static void GiveItem(ItemIndex itemIndex)
        {
            var localUser = LocalUserManager.GetFirstLocalUser();
            if (localUser != null && localUser.cachedMasterController && localUser.cachedMasterController.master && UmbraMenu.LocalPlayerInv)
            {
                if (Items.isDropItemForAll)
                {
                    Items.DropItemMethod(itemIndex);
                }
                else if (Items.isDropItemFromInventory)
                {
                    if (Items.CurrentInventory().Contains(itemIndex))
                    {
                        //UmbraMenu.LocalPlayerInv.RemoveItem(itemIndex, 1);
                        UmbraMenu.LocalPlayerInv.RemoveItemPermanent(itemIndex, 1);
                        Items.DropItemMethod(itemIndex);
                    }
                    else
                    {
                        Chat.AddMessage($"<color=yellow> You do not have that item and therefore cannot drop it from your inventory.</color>");
                        Chat.AddMessage($" ");
                    }
                }
                else
                {
                    //UmbraMenu.LocalPlayerInv.GiveItem(itemIndex, 1);
                    Items.TryGive(itemIndex, 1);
                }
            }
        }
    }
}
