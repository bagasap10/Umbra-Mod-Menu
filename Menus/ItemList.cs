using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public class ItemList : Menu
    {
        private static readonly IMenu itemsList = new ListMenu(12, new Rect(1503, 10, 20, 20), "ITEMS MENU");

        public ItemList() : base(itemsList)
        {
            if (UmbraMenu.characterCollected)
            {
                int buttonPlacement = 1;
                List<Button> buttons = new();

                for (int i = 0; i < UmbraMenu.items.Count; i++)
                {
                    ItemIndex itemIndex = UmbraMenu.items[i];

                    ItemDef def = ItemCatalog.GetItemDef(itemIndex);
                    if (def == null)
                        continue;

                    // Get localized name
                    string displayName = Language.GetString(def.nameToken);

                    // If the name is just the token (ITEM_XXX_NAME), skip it
                    if (string.IsNullOrEmpty(displayName) || displayName == def.nameToken)
                        continue;

                    void ButtonAction() => GiveItem(itemIndex);

                    var tierDef = ItemTierCatalog.GetItemTierDef(def.tier);
                    Color32 itemColor = ColorCatalog.GetColor(tierDef.colorIndex);
                    string itemName;

                    if (itemColor.r <= 105 && itemColor.g <= 105 && itemColor.b <= 105)
                    {
                        itemName = Util.GenerateColoredString(displayName, new Color32(255, 255, 255, 255));
                    }
                    else
                    {
                        itemName = Util.GenerateColoredString(displayName, itemColor);
                    }

                    Button button = new Button(new NormalButton(this, buttonPlacement, itemName, ButtonAction));
                    buttons.Add(button);
                    buttonPlacement++;
                }

                //Original
                //for (int i = 0; i < UmbraMenu.items.Count; i++)
                //{
                //    ItemIndex itemIndex = UmbraMenu.items[i];
                //    void ButtonAction() => GiveItem(itemIndex);
                //    Color32 itemColor = ColorCatalog.GetColor(ItemCatalog.GetItemDef(itemIndex).colorIndex);
                //    if (itemColor.r <= 105 && itemColor.g <= 105 && itemColor.b <= 105)
                //    {
                //        string itemName = Util.GenerateColoredString(Language.GetString(ItemCatalog.GetItemDef(itemIndex).nameToken), new Color32(255, 255, 255, 255));
                //        Button button = new Button(new NormalButton(this, buttonPlacement, itemName, ButtonAction));
                //        buttons.Add(button);
                //        buttonPlacement++;
                //    }
                //    else
                //    {
                //        string itemName = Util.GenerateColoredString(Language.GetString(ItemCatalog.GetItemDef(itemIndex).nameToken), itemColor);
                //        Button button = new Button(new NormalButton(this, buttonPlacement, itemName, ButtonAction));
                //        buttons.Add(button);
                //        buttonPlacement++;
                //    }
                //}
                AddButtons(buttons);
                SetActivatingButton(Utility.FindButtonById(3, 3));
                SetPrevMenuId(3);
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

        public static void GiveItem(ItemIndex itemIndex)
        {
            var localUser = LocalUserManager.GetFirstLocalUser();
            if (localUser.cachedMasterController && localUser.cachedMasterController.master)
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
                    UmbraMenu.LocalPlayerInv.GiveItemPermanent(itemIndex, 1);
                }
            }
        }
    }
}