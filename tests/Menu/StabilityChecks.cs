using System;
using System.Linq;
using UmbraMenu;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Root = UmbraMenu.UmbraMenu;

partial class Checks
{
    static void StabilityChecks()
    {
        CatalogIcons.Release();
        int initial = Addressables.IconLoadCalls;
        var defs = Enumerable.Range(0, CatalogIcons.MaximumLoads).Select(i => new ItemDef { IconReference = new AssetReferenceT<Sprite> { RuntimeKey = "Bounded" + i } }).ToArray();
        foreach (var def in defs) CatalogIcons.Get(def);
        Assert(Addressables.IconLoadCalls == initial + CatalogIcons.MaximumLoads, "The icon cache must fill only to its bound.");
        var extra = new ItemDef { IconReference = new AssetReferenceT<Sprite> { RuntimeKey = "Extra" } };
        CatalogIcons.Get(extra);
        Assert(Addressables.IconLoadCalls == initial + CatalogIcons.MaximumLoads, "Pending loads must neither be evicted nor allow an unbounded queue.");
        Addressables.IconOperations["Bounded0"].Status = AsyncOperationStatus.Succeeded;
        Addressables.IconOperations["Bounded0"].Result = new Sprite();
        int released = Addressables.ReleaseCount;
        CatalogIcons.Get(extra);
        Assert(Addressables.ReleaseCount == released + 1 && Addressables.IconLoadCalls == initial + CatalogIcons.MaximumLoads + 1, "A full cache must release the least recently used completed handle before loading another.");
        released = Addressables.ReleaseCount; CatalogIcons.Release();
        Assert(Addressables.ReleaseCount == released + CatalogIcons.MaximumLoads, "Unload must release every remaining owned icon, including pending loads.");
        CatalogIcons.Release(); Assert(Addressables.ReleaseCount == released + CatalogIcons.MaximumLoads, "Repeated cleanup must be idempotent.");

        var crit = ItemCatalog.FindItemIndex("CritGlasses");
        Root.InGame = true; Root.LocalPlayerInv = new Inventory(); Root.LocalPlayerInv.Counts[crit] = int.MaxValue - 9;
        Assert(!ItemPacks.Give(ItemPacks.All.Single(p => p.Name == "Crits"), out _) && Root.LocalPlayerInv.Counts.Count == 1 && Root.LocalPlayerInv.Counts[crit] == int.MaxValue - 9,
            "An overflowing pack must reject all ingredients before inventory mutation.");
        Root.LocalPlayerInv.Counts[crit] = int.MaxValue - 1;
        var duplicate = new ItemPack("Repeated ingredients", new PackPickup("CritGlasses", "Glasses"), new PackPickup("CritGlasses", "Glasses"));
        Assert(!ItemPacks.Give(duplicate, out _) && Root.LocalPlayerInv.Counts[crit] == int.MaxValue - 1, "Repeated ingredients must be validated together before either grant can overflow.");
        Root.LocalPlayerInv.Counts[crit] = 0;
        Assert(!ItemPacks.Give(new ItemPack("Invalid quantity", new PackPickup("CritGlasses", "Glasses", 0)), out _) && Root.LocalPlayerInv.Counts[crit] == 0, "An invalid ingredient quantity must reject the whole pack.");
        Assert(InventorySafety.CanAdd(int.MaxValue - 10, 10) && !InventorySafety.CanAdd(int.MaxValue - 9, 10), "Stack validation must support the integer boundary without overflowing arithmetic.");
        Assert(!InventorySafety.CanAdd(-1, 1) && !InventorySafety.CanAdd(0, 0) && !InventorySafety.CanAdd(0, -1), "Invalid counts must never be treated as safe grants.");
        Assert(InventorySafety.RollCount(int.MaxValue) == 500 && InventorySafety.BulkQuantity(int.MaxValue) == 1000, "One click must have a bounded amount of work.");
        var items = (UmbraMenu.Menus.Items)Root.menus[3];
        items.ItemsToRoll = int.MaxValue; items.AllItemsQuantity = int.MaxValue;
        Assert(items.ItemsToRoll == 500 && items.AllItemsQuantity == 1000, "The production quantity properties must clamp the values displayed by the menu.");
        items.ItemsToRoll = int.MinValue; items.AllItemsQuantity = int.MinValue;
        Assert(items.ItemsToRoll == 5 && items.AllItemsQuantity == 1, "Production quantity properties must restore positive minimums after invalid inputs.");

        RoR2.Run.instance = new RoR2.Run(); Root.characterCollected = Root.InGame = true;
        Root.LocalPlayerBody = new RoR2.CharacterBody(); UnityEngine.Networking.NetworkServer.active = true;
        Root.mainMenu.AddButtons(new System.Collections.Generic.List<Button> { new Button(new NormalButton(Root.mainMenu, 5, "TELEPORTER", () => { })) });
        var menu = new UmbraMenu.Menus.Teleporter(); Root.menus[5] = menu;
        UmbraMenu.Menus.Teleporter.ReleaseAssets();
        var portal = PortalCatalog.Find("anima"); Time.unscaledTime += 10;
        int spawned = UnityEngine.Networking.NetworkServer.Spawned.Count;
        menu.SpawnPortals("anima"); menu.SpawnPortals("anima");
        UmbraMenu.Menus.Teleporter.ReleaseAssets(); Addressables.Complete(portal.Address, Prefab("Late"));
        Assert(UnityEngine.Networking.NetworkServer.Spawned.Count == spawned, "A load finishing after menu unload must not create a portal.");
        Time.unscaledTime += 2; menu.SpawnPortals("anima"); Addressables.Complete(portal.Address, Prefab("Current"));
        Assert(UnityEngine.Networking.NetworkServer.Spawned.Count == spawned + 1, "A later generation must still be able to spawn its own portal.");
        menu.SpawnPortals("anima"); Assert(UnityEngine.Networking.NetworkServer.Spawned.Count == spawned + 1, "Rapid repeat clicks must not create duplicate portals.");
        UmbraMenu.Menus.Teleporter.ReleaseAssets();

        var actionMenu = new Menu(new NormalMenu(98, new Rect(), "Actions"));
        var failing = new Button(new TogglableButton(actionMenu, 1, "OFF", "ON", () => { }, () => { throw new InvalidOperationException("broken feature"); }));
        failing.Activate(); Assert(!failing.IsEnabled(), "A failing button must restore its state and contain the managed exception.");
        var exit = new Button(new NormalButton(actionMenu, 2, "Exit GUI", () => { throw new ExitGUIException(); }));
        bool propagated = false; try { exit.Activate(); } catch (ExitGUIException) { propagated = true; }
        Assert(propagated, "Unity's ExitGUI signal must never be swallowed as a feature failure.");
    }
}
