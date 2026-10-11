using System.Collections.Generic;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public class Settings : Menu
    {
        private static readonly IMenu settings = new NormalMenu(7, new Rect(374, 750, 20, 20), "SETTINGS MENU");

        public static string[] GodVerion = { "NORMAL", "INTANGIBLE", "REGEN", "NEGATIVE", "REVIVE" };
        public static string enableNavigationBtnText = UmbraMenu.AllowNavigation ? "TRUE" : "FALSE";
        public string EnableNavigationBtnText
        {
            get
            {
                return UmbraMenu.AllowNavigation ? "TRUE" : "FALSE";
            }
        }

        public Button changeWidth;
        public Button allowNavigation;
        public Button changeGodModeVersion;
        public Button toggleKeybindMenu;
        public Button reloadMenu;
        public Button resetSettings;
        public Button changeHeight;
        public Button changeOpacity;

        public Settings() : base(settings)
        {
            if (UmbraMenu.characterCollected)
            {
                void DoNothing() => Utility.StubbedFunction();
                void ToggleKeybindMenu() => UmbraMenu.menus[16].ToggleMenu();
                changeWidth = new Button(new MulButton(this, 1, $"WINDOW WIDTH : {MenuUI.ConfiguredWidth:0}", DoNothing, IncreaseWidth, DecreaseWidth));
                allowNavigation = new Button(new TogglableButton(this, 2, "KEYBOARD NAVIGATION : OFF", "KEYBOARD NAVIGATION : ON", ToggleAllowNavigation, ToggleAllowNavigation, UmbraMenu.AllowNavigation));
                changeGodModeVersion = new Button(new NormalButton(this, 3, $"GOD TYPE : {GodVerion[UmbraMenu.GodVersion]}", ChangeGodVersion));
                toggleKeybindMenu = new Button(new TogglableButton(this, 4, "KEYBIND MENU : OFF", "KEYBIND MENU : ON", ToggleKeybindMenu, ToggleKeybindMenu));
                reloadMenu = new Button(new NormalButton(this, 5, $"RELOAD MENU", ReloadMenus));
                resetSettings = new Button(new NormalButton(this, 6, $"RESET SETTINGS", SetSettingsToDefaults));
                changeHeight = new Button(new MulButton(this, 7, $"WINDOW HEIGHT : {MenuUI.ConfiguredHeight:0}", DoNothing, () => MenuUI.ChangeSize(0, 20), () => MenuUI.ChangeSize(0, -20)));
                changeOpacity = new Button(new MulButton(this, 8, $"GLASS OPACITY : {UmbraMenu.GlassOpacity:P0}", DoNothing, () => ChangeOpacity(.05f), () => ChangeOpacity(-.05f)));

                AddButtons(new List<Button>()
                {
                    changeWidth,
                    allowNavigation,
                    changeGodModeVersion,
                    toggleKeybindMenu,
                    reloadMenu,
                    resetSettings,
                    changeHeight,
                    changeOpacity
                });
                SetActivatingButton(Utility.FindButtonById(0, 7));
                SetPrevMenuId(0);
            }
        }

        public override void Draw()
        {
            if (IsEnabled())
            {
                changeWidth?.SetText($"WINDOW WIDTH : {MenuUI.ConfiguredWidth:0}");
                changeHeight?.SetText($"WINDOW HEIGHT : {MenuUI.ConfiguredHeight:0}");
                changeOpacity?.SetText($"GLASS OPACITY : {UmbraMenu.GlassOpacity:P0}");
                SetWindow();
                base.Draw();
            }
        }

        public override void Reset()
        {
            base.Reset();
        }

        public void IncreaseWidth()
        {
            MenuUI.ChangeSize(20, 0);
        }

        public void DecreaseWidth()
        {
            MenuUI.ChangeSize(-20, 0);
        }

        private void ChangeOpacity(float delta)
        {
            UmbraMenu.GlassOpacity = Mathf.Clamp(UmbraMenu.GlassOpacity + delta, .35f, .9f);
            Utility.SaveSettings();
        }

        public void ToggleAllowNavigation()
        {
            UmbraMenu.AllowNavigation = !UmbraMenu.AllowNavigation;
            if (!UmbraMenu.AllowNavigation)
            {
                UmbraMenu.navigationToggle = false;
                Navigation.buttonIndex = 0;
                Navigation.menuIndex = 0;
            }

            Utility.SaveSettings();
            allowNavigation.SetText($"ENABLE NAVIGATION : {EnableNavigationBtnText}");
            Utility.SoftResetMenu(true);
        }

        public void ChangeGodVersion()
        {
            if (Player.GodToggle)
            {
                Player.GodToggle = false;
                ((Player)UmbraMenu.playerMenu).toggleGod.SetEnabled(false);
                Player.DisabledGodMode();
                UmbraMenu.GodVersion++;
                Player.GodToggle = true;
                ((Player)UmbraMenu.playerMenu).toggleGod.SetEnabled(true);
            }
            else
            {
                UmbraMenu.GodVersion++;
            }

            if (UmbraMenu.GodVersion > 4)
            {
                UmbraMenu.GodVersion = 0;
            }

            Utility.SaveSettings();
            Utility.SoftResetMenu(true);
        }

        public void UpdateMenuWidths()
        {
            for (int i = 0; i < UmbraMenu.menus.Count; i++)
            {
                UmbraMenu.menus[i].SetWidthSize(UmbraMenu.Width);
            }
        }

        public void ReloadMenus()
        {
            Utility.ResetMenu();
        }

        public void SetSettingsToDefaults()
        {
            Utility.CreateDefaultSettingsFile();
            UmbraMenu.Settings = Utility.ReadSettings();
            UmbraMenu.Width = float.Parse(UmbraMenu.Settings[0]);
            UmbraMenu.WindowWidth = Utility.ReadUiSetting("Window Width", 680);
            UmbraMenu.WindowHeight = Utility.ReadUiSetting("Window Height", 560);
            UmbraMenu.GlassOpacity = Utility.ReadUiSetting("Glass Opacity", .62f);
            MenuUI.ResetWindow();
            UmbraMenu.AllowNavigation = bool.Parse(UmbraMenu.Settings[1]);
            UmbraMenu.GodVersion = int.Parse(UmbraMenu.Settings[2]);
            UmbraMenu.keybindDict = UmbraMenu.BuildKeybinds();
            UpdateMenuWidths();
            Utility.SoftResetMenu(true);
        }
    }
}
