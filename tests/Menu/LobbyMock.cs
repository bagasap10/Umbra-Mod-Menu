using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.SceneManagement { public struct Scene { public string name; } }

namespace UmbraMenu
{
    public static partial class Loader { public static bool upToDate = true; public static void Unload() { } }
    public partial class Keybind
    {
        public string Name;
        public Keybind() { }
        public Keybind(string name, KeyCode key) { Name = name; KeyCode = key; }
    }
    public static partial class UmbraMenu
    {
        public static bool InGame, msgSent = true;
        public static UnityEngine.SceneManagement.Scene currentScene;
        public static int GodVersion;
        public static Menu playerMenu, movementMenu, teleporterMenu, cheatsMenu, renderMenu, settingsMenu, keybindListMenu,
            buffListMenu, characterListMenu, chestItemListMenu, equipmentListMenu, itemListMenu, spawnListMenu, inventoryItemListMenu;
        public static List<string> keyBindNames = new List<string>();
        public static List<string> Settings = new List<string> { "350", "True", "0" };
        public static bool InGameCheck() => InGame;
        public static Dictionary<string, Keybind> BuildKeybinds()
        {
            var result = new Dictionary<string, Keybind>();
            foreach (string name in keyBindNames) result.Add(name, new Keybind(name, KeyCode.None));
            result["MAIN MENU"].KeyCode = KeyCode.Insert;
            return result;
        }
    }
    public static partial class Utility
    {
        public static int Rebuilds;
        public static void StubbedFunction() { }
        public static void SoftResetMenu(bool preserve) { Rebuilds++; }
        public static void ResetMenu() { Rebuilds++; }
        public static void CreateDefaultSettingsFile() { }
        public static List<string> ReadSettings() => new List<string> { "350", "True", "0" };
        public static float ReadUiSetting(string name, float fallback) => fallback;
    }
}
namespace UmbraMenu.Menus
{
    public sealed class Player : Menu
    {
        public static bool GodToggle;
        public static int DisableCalls;
        public Button toggleGod;
        public Player() : base(new NormalMenu(1, new Rect(), "Player")) { }
        public static void DisabledGodMode() { DisableCalls++; }
    }
    public sealed class CheatsMenu : Menu
    {
        public const int MainButtonId = 9;
        public CheatsMenu() : base(new NormalMenu(20, new Rect(), "Cheats")) { AddButtons(new List<Button>()); }
        public void RefreshControls() { }
    }
    public partial class Render : Menu
    {
        public static bool scenePopulateToggle = true;
        public static int Refreshes;
        public static void EnableInteractables() { scenePopulateToggle = false; }
        public static void DisableInteractables() { scenePopulateToggle = true; }
        public static void RefreshInteractables() { Refreshes++; }
    }
}
