using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public sealed class ItemPackList : Menu
    {
        public const int MenuId = 18;
        public ItemPackList() : base(new SearchableListMenu(MenuId, new Rect(), "Item packs", "Search packs or items..."))
        {
            SetPrevMenuId(3);
            ConfigureSearch(_ => RefreshPacks());
            if (UmbraMenu.characterCollected) SetActivatingButton(Utility.FindButtonById(3, Items.PacksButtonId));
            RefreshPacks();
        }

        private void RefreshPacks()
        {
            var rows = new List<Button>();
            int position = 1;
            bool? chefSection = null;
            string query = SearchText.Trim();
            foreach (ItemPack pack in ItemPacks.Sorted())
            {
                bool resolved = ItemPacks.TryResolve(pack, out var contents, out string error);
                if (query.Length > 0 && pack.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0
                    && (pack.IsChefRecipe ? "CHEF recipes" : "General packs").IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0
                    && !pack.Pickups.Any(value => value.DisplayName.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    && !contents.Any(value => value.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)) continue;
                if (chefSection != pack.IsChefRecipe)
                {
                    rows.Add(new Button(new DividerButton(this, position++, pack.IsChefRecipe ? "CHEF recipes" : "General packs", UITheme.Accent, true)));
                    chefSection = pack.IsChefRecipe;
                }
                string title = pack.Name + (resolved ? " / " + contents.Sum(value => value.Value) + " pts" : " / " + error);
                Button opener = null;
                opener = new Button(new NormalButton(this, position++, title, () =>
                {
                    ((ItemPackDetails)UmbraMenu.menus[ItemPackDetails.MenuId]).ShowPack(pack, opener);
                    MenuUI.NavigateTo(ItemPackDetails.MenuId);
                }));
                rows.Add(opener);
            }
            if (rows.Count == 0) rows.Add(new Button(new DividerButton(this, 1, "No packs match your search.", UITheme.Muted, false)));
            AddButtons(rows);
        }
    }
}
