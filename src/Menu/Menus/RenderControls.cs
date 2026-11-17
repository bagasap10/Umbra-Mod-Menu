using System.Collections.Generic;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public partial class Render
    {
        private static readonly IMenu render = new NormalMenu(6, new Rect(10, 795, 20, 20), "RENDER MENU");
        public static bool renderMobs, renderInteractables, renderMods = true;
        public Button toggleActiveMods, toggleInteractESP, toggleMobESP;

        public Render() : base(render)
        {
            toggleActiveMods = new Button(new TogglableButton(this, 1, "ACTIVE MODS : OFF", "ACTIVE MODS : ON", ToggleRenderMods, ToggleRenderMods, renderMods));
            toggleInteractESP = new Button(new TogglableButton(this, 2, "INTERACTABLES ESP : OFF", "INTERACTABLES ESP : ON", ToggleRenderInteractables, ToggleRenderInteractables, renderInteractables));
            toggleMobESP = new Button(new TogglableButton(this, 3, "MOB ESP : OFF", "MOB ESP : ON", ToggleRenderMobs, ToggleRenderMobs, renderMobs));
            AddButtons(new List<Button> { toggleActiveMods, toggleInteractESP, toggleMobESP });
            SetActivatingButton(UmbraMenu.mainMenu.GetButtons()?.Find(button => button.GetId() == 6));
            SetPrevMenuId(0);
            if (renderInteractables) EnableInteractables();
        }

        public override void Reset()
        {
            // A scene/menu reset clears scene resources, retaining the user's visual choices.
            DisableInteractables();
            base.Reset();
        }

        private void ToggleRenderInteractables() => ToggleRenderInteractablesKey();

        public static void ToggleRenderInteractablesKey()
        {
            renderInteractables = !renderInteractables;
            if (renderInteractables)
            {
                EnableInteractables();
                if (UmbraMenu.InGameCheck()) RefreshInteractables();
            }
            else DisableInteractables();
        }

        private void ToggleRenderMods() => renderMods = !renderMods;
        private void ToggleRenderMobs() => renderMobs = !renderMobs;
    }
}
