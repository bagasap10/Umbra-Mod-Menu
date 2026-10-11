using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public sealed class InventoryItemList : Menu
    {
        public const int MenuId = 17;
        public const int ActivatingButtonId = Items.RemoveItemsButtonId;

        private Inventory displayedInventory;
        private Dictionary<ItemIndex, int> displayedCounts;
        private readonly Dictionary<ItemIndex, Button> itemButtons = new Dictionary<ItemIndex, Button>();
        private float nextRefreshTime;

        public InventoryItemList() : base(new SearchableListMenu(MenuId, new Rect(1503, 10, 20, 20), "REMOVE ITEMS (1 PER CLICK)"))
        {
            ConfigureSearch(_ => RefreshInventory(true));
            ShowEmptyMessage("No items to remove.");
            SetPrevMenuId(3);
            if (UmbraMenu.characterCollected)
                SetActivatingButton(Utility.FindButtonById(3, ActivatingButtonId));
        }

        public override void Draw()
        {
            if (IsEnabled())
            {
                RefreshInventory();
                SetWindow();
                base.Draw();
            }
        }

        public void RefreshInventory(bool force = false)
        {
            if (!IsEnabled() && !force)
                return;

            Inventory inventory = UmbraMenu.InGameCheck() ? UmbraMenu.LocalPlayerInv : null;
            bool inventoryChanged = inventory != displayedInventory;
            if (!force && !inventoryChanged && Time.unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = Time.unscaledTime + 0.25f;
            Dictionary<ItemIndex, int> counts = ReadInventory(inventory);
            if (!force && !inventoryChanged && displayedCounts != null && counts.Count == displayedCounts.Count
                && counts.All(pair => displayedCounts.TryGetValue(pair.Key, out int previousCount) && previousCount == pair.Value))
                return;

            bool navigating = UmbraMenu.navigationToggle && Navigation.menuIndex == MenuId;
            int previousPosition = Navigation.buttonIndex;
            ItemIndex? selectedItem = null;
            if (navigating)
            {
                foreach (var pair in itemButtons)
                {
                    if (pair.Value.GetId() == previousPosition)
                    {
                        selectedItem = pair.Key;
                        break;
                    }
                }
            }

            displayedInventory = inventory;
            displayedCounts = counts;
            itemButtons.Clear();
            AddButtons(CatalogGrouping.BuildItemButtons(this, counts.Keys, RemoveSelectedItem, index => counts[index], itemButtons, SearchText));

            if (itemButtons.Count == 0)
            {
                ShowEmptyMessage(inventory == null ? "No player inventory available."
                    : !string.IsNullOrWhiteSpace(SearchText) ? "No owned items match your search." : "No items to remove.");
                SetScrollPosition(Vector2.zero);
            }

            if (navigating)
            {
                // Keep the same item highlighted when pickups insert new rows ahead of it.
                if (selectedItem.HasValue && itemButtons.TryGetValue(selectedItem.Value, out Button selectedButton))
                    Navigation.buttonIndex = selectedButton.GetId();
                else
                    Navigation.buttonIndex = Mathf.Clamp(previousPosition, 1, GetNumberOfButtons());

                Navigation.UpdateIndexValues();
            }
        }

        private static Dictionary<ItemIndex, int> ReadInventory(Inventory inventory)
        {
            Dictionary<ItemIndex, int> counts = new Dictionary<ItemIndex, int>();
            if (inventory == null)
                return counts;

            foreach (ItemIndex itemIndex in ItemCatalog.allItems)
            {
                int count = inventory.GetItemCountPermanent(itemIndex);
                if (count > 0)
                    counts.Add(itemIndex, count);
            }
            return counts;
        }

        public static bool RemoveItem(ItemIndex itemIndex)
        {
            Inventory inventory = UmbraMenu.LocalPlayerInv;
            if (!UmbraMenu.InGameCheck() || inventory == null || ItemCatalog.GetItemDef(itemIndex) == null)
                return false;

            int count = inventory.GetItemCountPermanent(itemIndex);
            if (count <= 0)
                return false;

            // Use the inventory API so its stat updates and item-change notifications still run.
            inventory.RemoveItemPermanent(itemIndex, 1);
            return inventory.GetItemCountPermanent(itemIndex) < count;
        }

        private void RemoveSelectedItem(ItemIndex itemIndex)
        {
            RemoveItem(itemIndex);
            // Rebuild on the next update, after the current GUI button iteration finishes.
            nextRefreshTime = 0;
        }

        private void ShowEmptyMessage(string message)
        {
            AddButtons(new List<Button>
            {
                new Button(new DividerButton(this, 1, message, new Color32(190, 190, 190, 255), false))
            });
        }
    }
}
