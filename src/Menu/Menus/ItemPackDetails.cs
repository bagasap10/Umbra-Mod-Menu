using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public sealed class ItemPackDetails : Menu
    {
        public const int MenuId = 19;
        private static ItemPack selectedPack;
        private Button status;
        public ItemPackDetails() : base(new ListMenu(MenuId, new Rect(), "Pack details"))
        {
            SetPrevMenuId(ItemPackList.MenuId);
            if (selectedPack != null) ShowPack(selectedPack, null);
            else AddButtons(new List<Button>());
        }

        internal void ShowPack(ItemPack pack, Button opener)
        {
            selectedPack = pack;
            SetTitle(pack.Name);
            SetActivatingButton(opener);
            SetScrollPosition(Vector2.zero);
            var rows = new List<Button>();
            if (!ItemPacks.TryResolve(pack, out var contents, out string error))
            {
                rows.Add(new Button(new TextButton(this, 1, error + ". No items will be given.")));
                AddButtons(rows);
                return;
            }
            rows.Add(new Button(new NormalButton(this, 1, "GIVE PACK", GivePack)));
            rows.Add(new Button(new TextButton(this, 2, contents.Sum(value => value.Value) + " value points / " + contents.Sum(value => value.Source.Quantity) + " total pickups")));
            status = new Button(new TextButton(this, 3, contents.Any(value => value.Source.IsEquipment)
                ? "Adds item stacks and replaces your active equipment." : pack.IsChefRecipe
                    ? "Adds CHEF recipe ingredients to your inventory." : "Adds these stacks to your current inventory."));
            rows.Add(status);
            foreach (ResolvedPackPickup entry in contents)
                rows.Add(new Button(new PickupButton(this, rows.Count + 1, entry.Source.Quantity + "x " + entry.Name, null, entry.Icon)));
            AddButtons(rows);
        }

        private void GivePack()
        {
            ItemPacks.Give(selectedPack, out string message);
            status?.SetText(message);
        }
    }
}
