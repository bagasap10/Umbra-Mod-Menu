using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UmbraMenu;
using UmbraMenu.Menus;
using Root = UmbraMenu.UmbraMenu;

internal static class LobbyChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static void Setup()
    {
        Root.characterCollected = Root.forceFullModMenu = Root.InGame = Root.chatOpen = Root.listenForKeybind = Root.navigationToggle = false;
        Root.currentScene = new UnityEngine.SceneManagement.Scene { name = "title" };
        Root.AllowNavigation = true; Root.WindowWidth = 680; Root.WindowHeight = 560; Root.GlassOpacity = .62f;
        Root.keyBindNames = new List<string> { "MAIN MENU", "RENDER MENU", "RENDER ACTIVE MODS", "RENDER INTERACTABLES", "RENDER MOB ESP" };
        Root.keybindDict = Root.BuildKeybinds(); Root.keybindDict["RENDER MENU"].KeyCode = KeyCode.A;
        Root.keybindDict["RENDER ACTIVE MODS"].KeyCode = KeyCode.C; Root.keybindDict["RENDER INTERACTABLES"].KeyCode = KeyCode.I;
        Root.keybindDict["RENDER MOB ESP"].KeyCode = KeyCode.M;
        Root.menus.Clear();
        for (int i = 0; i <= 20; i++) { var menu = new Menu(new NormalMenu(i, new Rect(), "Test")); menu.AddButtons(new List<Button>()); Root.menus.Add(menu); }
        Root.menus[0] = Root.mainMenu = new Main();
        Root.menus[1] = Root.playerMenu = new Player();
        Root.menus[6] = Root.renderMenu = new Render();
        Root.menus[7] = Root.settingsMenu = new Settings();
        Root.menus[16] = Root.keybindListMenu = new KeybindList();
        Root.menus[20] = Root.cheatsMenu = new CheatsMenu();
        Root.teleporterMenu = Root.menus[5]; Root.buffListMenu = Root.menus[11]; Root.characterListMenu = Root.menus[10];
        Root.chestItemListMenu = Root.menus[14]; Root.equipmentListMenu = Root.menus[13]; Root.itemListMenu = Root.menus[12];
        Root.spawnListMenu = Root.menus[15]; Root.inventoryItemListMenu = Root.menus[17];
        Root.mainMenu.SetEnabled(true); MenuUI.ResetWindow(); MenuUI.NavigateTo(0);
        Input.Pressed = KeyCode.None; Input.inputString = ""; Event.current = new Event { type = EventType.Repaint };
    }
    private static void Run()
    {
        Root.characterCollected = false; Root.menus = null;
        Root.mainMenu = new Main(); Root.renderMenu = new Render(); Root.settingsMenu = new Settings(); Root.keybindListMenu = new KeybindList();
        Check(Root.renderMenu.GetActivatingButton().GetParentMenu() == Root.mainMenu
            && Root.settingsMenu.GetActivatingButton().GetParentMenu() == Root.mainMenu
            && Root.keybindListMenu.GetActivatingButton().GetParentMenu() == Root.settingsMenu,
            "Initial menu construction must link pages without accessing the not-yet-created menu registry.");
        Root.menus = new List<Menu>();
        Setup(); var runtime = new LobbyRuntimeDriver();
        Check(Root.mainMenu.GetButtons().Select(row => row.GetId()).SequenceEqual(new[] { 6, 7, 8 }), "The lobby overview must offer Visuals, Settings and Unload without gameplay actions.");
        foreach (int id in new[] { 0, 6, 7, 16 }) Check(MenuUI.CanUsePage(id), "Non-gameplay pages must be accessible without a player.");
        foreach (int id in new[] { 1, 2, 3, 4, 5, 8, 9, 10, 11, 12, 13, 14, 15, 17, 18, 19, 20 }) Check(!MenuUI.CanUsePage(id), "Gameplay pages must still require a player.");
        Check(!MenuUI.CanUsePage(-1) && !MenuUI.CanUsePage(100), "Invalid page IDs must be rejected.");
        MenuUI.NavigateTo(7); MenuUI.SyncPage(); Check(MenuUI.ActiveMenuId == 7 && Root.settingsMenu.GetButtons().Count == 8, "Settings must be constructed and stay selected in the title screen/lobby.");
        GUI.StartCanvas(); MenuUI.Draw(); GUI.Save("artifacts/tests/previews/lobby-settings.png");
        int saves = Utility.Saves; GUI.ClickLabel = "+"; MenuUI.Draw();
        Check(Root.WindowWidth == 700 && Utility.Saves > saves, "A real Settings plus click must resize and save without a player.");
        Navigation.IncreaseValue(7, 7); Navigation.IncreaseValue(7, 8);
        Check(Root.WindowHeight == 580 && Math.Abs(Root.GlassOpacity - .67f) < .001f, "Height and opacity controls must work before a run.");
        Player.GodToggle = true; Player.DisableCalls = 0; int god = Root.GodVersion;
        ((Settings)Root.settingsMenu).ChangeGodVersion();
        Check(Root.GodVersion != god && Player.DisableCalls == 0, "Changing the configured God type must not touch a missing player."); Player.GodToggle = false;
        ((Settings)Root.settingsMenu).ToggleAllowNavigation(); Check(!Root.AllowNavigation, "Keyboard navigation settings must be editable in the lobby.");
        ((Settings)Root.settingsMenu).ToggleAllowNavigation();
        Navigation.PressBtn(7, 4); Check(MenuUI.ActiveMenuId == 16 && Root.keybindListMenu.GetButtons().Count == 4, "Keybinds must open from lobby Settings.");
        runtime.TickKeybindButtons(); Check(Utility.GetEnabledKeybindButton() == null, "An idle keybind page must not require an active capture row.");
        Navigation.PressBtn(16, 1); Input.inputString = "B"; runtime.TickCapture(); Input.inputString = "";
        Check(Root.keybindDict["RENDER MENU"].KeyCode == KeyCode.B && !Root.listenForKeybind, "Production key capture must save a binding without a player.");
        Navigation.PressBtn(16, 1); Input.Pressed = KeyCode.Escape; runtime.TickInputs(); Input.Pressed = KeyCode.None;
        Check(!Root.listenForKeybind, "Escape must cancel key capture before a run.");
        MenuUI.NavigateTo(6); var visuals = (Render)Root.renderMenu;
        visuals.toggleActiveMods.Activate(); visuals.toggleInteractESP.Activate(); visuals.toggleMobESP.Activate();
        Check(!Render.renderMods && Render.renderInteractables && Render.renderMobs && Render.Refreshes == 0, "Lobby visual switches must update preferences without scanning gameplay objects.");
        visuals.Reset(); Root.menus[6] = Root.renderMenu = visuals = new Render();
        Check(!visuals.toggleActiveMods.IsEnabled() && visuals.toggleInteractESP.IsEnabled() && visuals.toggleMobESP.IsEnabled(), "Visual choices must survive scene/menu rebuilds with matching switch states.");
        MenuUI.NavigateTo(6); GUI.StartCanvas(); MenuUI.Draw(); GUI.Save("artifacts/tests/previews/lobby-visuals.png");
        Root.currentScene = new UnityEngine.SceneManagement.Scene { name = "lobby" };
        MenuUI.NavigateTo(7); Input.Pressed = KeyCode.Keypad2; runtime.TickInputs();
        Check(Root.navigationToggle && Navigation.menuIndex == 7 && Navigation.buttonIndex == 1, "Numpad selection must activate in lobby Settings.");
        float width = Root.WindowWidth; Input.Pressed = KeyCode.Keypad6; runtime.TickInputs();
        Check(Root.WindowWidth == width + 20, "Numpad adjustment must execute the real Settings stepper in the lobby.");
        Input.Pressed = KeyCode.Keypad1; runtime.TickInputs(); Check(MenuUI.ActiveMenuId == 0, "Numpad Back must return to the lobby overview.");
        Root.navigationToggle = false; Input.Pressed = KeyCode.B; runtime.TickInputs(); MenuUI.SyncPage();
        Check(MenuUI.ActiveMenuId == 6, "The configured Visuals hotkey must work without an active run.");
        bool previous = Render.renderInteractables; Input.Pressed = KeyCode.I; runtime.TickInputs();
        Check(Render.renderInteractables != previous && Render.Refreshes == 0, "Visual ESP hotkeys must work in the lobby without scene scans.");
        Input.Pressed = KeyCode.Return; runtime.TickInputs();
        Check(Root.chatOpen, "Lobby chat entry must activate the input guard without a run.");
        Input.Pressed = KeyCode.I; previous = Render.renderInteractables; runtime.TickInputs();
        Check(Render.renderInteractables == previous, "Visual hotkeys must not fire while typing into lobby chat.");
        Input.Pressed = KeyCode.Escape; runtime.TickInputs();
        Check(!Root.chatOpen, "Escape must release the lobby chat guard."); Input.Pressed = KeyCode.None;
        Root.characterCollected = Root.InGame = true; MenuUI.NavigateTo(7); Root.characterCollected = Root.InGame = false; MenuUI.SyncPage();
        Check(MenuUI.ActiveMenuId == 7, "Settings must remain selected when the player disappears.");
        Root.characterCollected = Root.InGame = true; MenuUI.NavigateTo(1); Root.characterCollected = Root.InGame = false; MenuUI.SyncPage();
        Check(MenuUI.ActiveMenuId == 0 && Navigation.menuIndex == 0 && !Root.playerMenu.IsEnabled(), "Leaving gameplay must close restricted pages and clear their keyboard target.");
        int actions = 0; Root.playerMenu.AddButtons(new List<Button> { new Button(new MulButton(Root.playerMenu, 1, "Game action", () => actions++, () => actions++, () => actions++)) });
        Navigation.PressBtn(1, 1); Navigation.IncreaseValue(1, 1); Navigation.DecreaseValue(1, 1);
        Check(actions == 0, "Stale keyboard targets must not execute gameplay actions in the lobby.");
        GUI.StartCanvas(); GUI.ClickLabel = "Settings"; MenuUI.Draw(); Check(MenuUI.ActiveMenuId == 7, "The lobby sidebar must enable actual Settings clicks.");
        GUI.ClickLabel = "Visuals"; MenuUI.Draw(); Check(MenuUI.ActiveMenuId == 6, "The lobby sidebar must enable actual Visuals clicks.");
        MenuUI.NavigateTo(0); GUI.StartCanvas(); MenuUI.Draw(); GUI.Save("artifacts/tests/previews/lobby-overview.png");
        Check(GUI.Labels.Keys.Any(label => label.Contains("Adjust Visuals and Settings now.")), "The waiting overview must explain that visual and settings changes are already available.");
        ((Settings)Root.settingsMenu).SetSettingsToDefaults();
        Check(Root.WindowWidth == 680 && Root.WindowHeight == 560 && Root.GlassOpacity == .62f, "Resetting settings must work without a player.");
        Root.InGame = Root.characterCollected = true; Render.renderInteractables = false; Render.ToggleRenderInteractablesKey();
        Check(Render.Refreshes == 1, "The same ESP control must refresh gameplay targets once an active run exists.");
    }
    private static int Main()
    {
        try { Run(); Console.WriteLine("PASS: " + checks + " lobby settings, visuals, key capture, input and access assertions."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
