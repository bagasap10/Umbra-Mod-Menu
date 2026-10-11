using System;
using System.Collections.Generic;
using System.Linq;
using UmbraMenu;
using UmbraMenu.Menus;
using RoR2;
using UnityEngine;
using Root = UmbraMenu.UmbraMenu;

partial class Checks
{
    static EquipmentIndex SearchEquipment(int id, string name, int dlc = 0, bool lunar = false, bool elite = false)
    {
        var index = (EquipmentIndex)id;
        string token = "SEARCH_EQUIP_" + id;
        Language.Names[token] = name;
        EquipmentCatalog.Defs[index] = new EquipmentDef
        {
            name = "Catalog" + id, nameToken = token, requiredExpansion = Expansion(dlc), isLunar = lunar,
            Icon = new Sprite { texture = new Texture2D(64, 64) }
        };
        if (elite) EliteCatalog.Indices.Add(index);
        return index;
    }

    static void CatalogSearchChecks()
    {
        int previousScreenWidth = Screen.width, previousScreenHeight = Screen.height;
        var capacitor = SearchEquipment(7000, "Royal Capacitor", 2);
        var missile = SearchEquipment(7001, "Disposable Missile Launcher");
        var tonic = SearchEquipment(7002, "Spinel Tonic", lunar: true);
        var aspect = SearchEquipment(7003, "Royal Aspect", 4, lunar: true, elite: true);
        var fallback = (EquipmentIndex)7004;
        EquipmentCatalog.Defs[fallback] = new EquipmentDef { nameToken = "UNTRANSLATED_ASPECT", name = "AffixYellow" };
        var unnamed = (EquipmentIndex)7005;
        EquipmentCatalog.Defs[unnamed] = new EquipmentDef();
        Root.equipment = new List<EquipmentIndex> { aspect, unnamed, tonic, EquipmentIndex.None, capacitor, fallback, missile, (EquipmentIndex)99999 };
        var lens = Item(7000, ItemTier.Tier1, "Lens-Maker's Glasses");
        var dlcLens = Item(7001, ItemTier.Tier1, "Lens Focus", 4);
        var voidLens = Item(7002, ItemTier.VoidTier1, "Lost Seer's Lenses", 1);
        var syringe = Item(7003, ItemTier.Tier1, "Soldier's Syringe");
        Root.items = new List<ItemIndex> { syringe, voidLens, dlcLens, lens };
        ItemCatalog.Defs[lens].Icon = new Sprite { texture = new Texture2D(64, 64) };
        Root.LocalPlayerInv = new Inventory { activeEquipmentSlot = 1 };
        Root.LocalPlayerInv.Counts[syringe] = 4;
        Items.isDropItemForAll = false; Items.isDropItemFromInventory = false;
        Root.characterCollected = true; Root.navigationToggle = false;
        var parent = Root.menus[3];
        parent.AddButtons(new List<Button> {
            new Button(new NormalButton(parent, Items.SpawnEquipmentButtonId, "EQUIPMENT", () => {})),
            new Button(new NormalButton(parent, Items.ChestButtonId, "CHEST", () => {}))
        });
        var equipment = new EquipmentList(); Root.menus[13] = equipment;
        var chest = new ChestItemList(); Root.chestItemListMenu = chest; Root.menus[14] = chest;
        var equipmentSearch = (SearchableListMenu)Layout(equipment);
        var chestSearch = (SearchableListMenu)Layout(chest);
        equipment.SetEnabled(true);
        string[] allEquipment = equipment.GetButtons().Select(button => Row(button).Text).ToArray();
        Assert(equipment.GetButtons().Count(button => button.GetAction() != null) == 5,
            "Invalid/unnamed equipment must be skipped without empty headings; unlocalized elite aspects keep their catalog-name fallback.");
        equipmentSearch.CurrentScrollPosition = new Vector2(0, 900);
        equipmentSearch.SetSearchText("  rOyAl ");
        Assert(equipment.GetButtons().Select(button => Row(button).Text).SequenceEqual(new[] {
            "Orange / Equipment", "DLC#2 / Seekers of the Storm", "Royal Capacitor",
            "Elite Equipment", "DLC#4 / Hallowed Concepts", "Royal Aspect" }),
            "Equipment search must ignore case/outer whitespace and retain the correct equipment/DLC headings.");
        Assert(equipmentSearch.CurrentScrollPosition.y == 0 &&
            equipment.GetButtons().Select(button => button.GetId()).SequenceEqual(Enumerable.Range(1, 6)),
            "Filtering equipment must reset scroll and leave contiguous keyboard row positions.");
        equipmentSearch.SetSearchText("capacitor");
        var capacitorRow = equipment.GetButtons().Single(button => button.GetAction() != null);
        Assert(((PickupButton)Row(capacitorRow)).TryGetIcon(out var equipTexture, out var equipUv) && equipTexture == EquipmentCatalog.Defs[capacitor].Icon.texture,
            "Filtered equipment must retain the chosen equipment's exact catalog icon.");
        capacitorRow.Activate();
        Assert(Root.LocalPlayerInv.currentEquipmentIndex == capacitor && Root.LocalPlayerInv.LastLocation.slot == 1 &&
            Root.LocalPlayerInv.Counts[syringe] == 4 && Root.LocalPlayerInv.Counts.Count == 1,
            "A filtered equipment callback must equip only its own index in the active slot, leaving items unchanged.");
        equipmentSearch.SetSearchText("affixyellow");
        Assert(Row(equipment.GetButtons().Single(button => button.GetAction() != null)).Text == "AffixYellow",
            "Equipment with an untranslated token must remain searchable by its visible fallback name.");
        Root.navigationToggle = true; Navigation.menuIndex = 13; Navigation.buttonIndex = 3;
        equipmentSearch.SetSearchText("no such equipment");
        Assert(equipment.GetButtons().Count == 1 && equipment.GetButtons()[0].GetAction() == null &&
            Row(equipment.GetButtons()[0]).Text == "No equipment matches your search." && Navigation.buttonIndex == 0,
            "An unmatched equipment query must show a non-actionable empty state and clear keyboard selection.");
        Navigation.PressBtn(13, 0);
        Event.current = new Event { type = EventType.Repaint }; GUI.ClickLabel = "Clear"; equipment.Draw();
        Assert(equipmentSearch.SearchText == "" && equipment.GetButtons().Select(button => Row(button).Text).SequenceEqual(allEquipment),
            "Equipment Clear must restore all groups and equipment.");
        Assert(equipment.IsSearchFocused() && MenuUI.IsSearchFocused && equipmentSearch.CurrentScrollPosition.y == 0,
            "Equipment text focus must use the shared shortcut guard.");
        Event.current = new Event { type = EventType.KeyDown, keyCode = KeyCode.Return }; equipment.Draw();
        Assert(!equipment.IsSearchFocused() && Event.current.type == EventType.Used,
            "Enter must leave equipment search without granting another pickup.");
        equipmentSearch.SetSearchText("tonic");
        Assert(equipment.GetButtons().Select(button => Row(button).Text).SequenceEqual(new[] {
            "Blue / Lunar Equipment", "Normal / Not-DLC", "Spinel Tonic" }), "Lunar equipment must remain searchable.");

        Camera.main = new Camera();
        var locked = new ChestBehavior(PickupCatalog.FindPickupIndex(lens));
        locked.transform.position = new Vector3(0, 0, 10);
        UnityEngine.Object.SceneObjects = new UnityEngine.Object[] { locked };
        chest.SetEnabled(true);
        Assert(ChestItemList.selectedChest == locked && !ChestItemList.IsClosestChestEquip && SceneDirector.Subscribers == 1,
            "Opening Change Chest Item must lock the closest item chest and subscribe once.");
        ChestItemList.EnableChests();
        Assert(SceneDirector.Subscribers == 1, "Repeated enabling must not duplicate scene callbacks.");
        Navigation.menuIndex = 14; chestSearch.CurrentScrollPosition = new Vector2(0, 900);
        chestSearch.SetSearchText(" LeNs ");
        Assert(chest.GetButtons().Select(button => Row(button).Text).SequenceEqual(new[] {
            "White / Common", "Normal / Not-DLC", "Lens-Maker's Glasses", "DLC#4 / Hallowed Concepts", "Lens Focus",
            "Purple / Void", "DLC#1 / Survivors of the Void", "Lost Seer's Lenses" }),
            "Chest item search must retain only matching item rarity/DLC sections.");
        Assert(chestSearch.CurrentScrollPosition.y == 0 && Navigation.buttonIndex == 3,
            "Chest search must reset scrolling and skip headings for keyboard selection.");
        Assert(chest.GetButtons().Select(button => button.GetId()).SequenceEqual(Enumerable.Range(1, 8)),
            "Filtered chest item rows must have contiguous positions.");
        var nearer = new ChestBehavior(PickupCatalog.FindPickupIndex(syringe));
        nearer.transform.position = new Vector3(0, 0, 1);
        ChestItemList.chests.Add(nearer);
        var oldPickup = nearer.currentPickup.pickupIndex;
        chest.GetButtons().Single(button => Row(button).Text == "Lens Focus").Activate();
        Assert(locked.currentPickup.pickupIndex == PickupCatalog.FindPickupIndex(dlcLens) && nearer.currentPickup.pickupIndex == oldPickup,
            "Filtering and selecting an item must change the locked chest even if another chest becomes closer.");
        Assert(Root.LocalPlayerInv.currentEquipmentIndex == capacitor && Root.LocalPlayerInv.Counts.Count == 1 &&
            Root.LocalPlayerInv.Counts[syringe] == 4,
            "Changing a chest pickup must not grant, remove, or replace the player's inventory.");
        Assert(equipmentSearch.SearchText == "tonic" && chestSearch.SearchText == " LeNs ",
            "Equipment and chest searches must have independent queries.");
        chestSearch.SetSearchText("lens-maker");
        Assert(((PickupButton)Row(chest.GetButtons().Single(button => button.GetAction() != null))).TryGetIcon(out var itemTexture, out var itemUv) && itemTexture == ItemCatalog.Defs[lens].Icon.texture,
            "Filtered chest item icons must remain paired with the correct item.");
        chestSearch.SetSearchText("no item matches");
        Assert(chest.GetButtons().Single().GetAction() == null && Navigation.buttonIndex == 0,
            "No chest-item results must be safe to navigate.");
        GUI.FocusControl(null); GUI.ClickLabel = "Clear"; Event.current = new Event { type = EventType.Repaint }; chest.Draw();
        Assert(chestSearch.SearchText == "" && chest.GetButtons().Count(button => button.GetAction() != null) == 4 &&
            chest.IsSearchFocused() && MenuUI.IsSearchFocused, "Chest Clear must restore all item groups and guard typing.");
        foreach (var key in new[] { KeyCode.Escape, KeyCode.KeypadEnter })
        {
            GUI.FocusControl("UmbraItemSearch_14"); Event.current = new Event { type = EventType.Repaint }; chest.Draw();
            Event.current = new Event { type = EventType.KeyDown, keyCode = key }; chest.Draw();
            Assert(!chest.IsSearchFocused() && Event.current.type == EventType.Used,
                "Escape/Enter must end chest text entry without changing its pickup.");
        }

        chestSearch.SetSearchText("royal"); chestSearch.CurrentScrollPosition = new Vector2(0, 200);
        var equipmentChest = new ChestBehavior(PickupCatalog.FindPickupIndex(tonic));
        equipmentChest.transform.position = new Vector3(0, 0, 5);
        UnityEngine.Object.SceneObjects = new UnityEngine.Object[] { equipmentChest };
        SceneDirector.Populate();
        Assert(ChestItemList.selectedChest == null && ChestItemList.chests.SequenceEqual(new[] { equipmentChest }),
            "A new populated scene must discard the previous chest selection.");
        ChestItemList.IsClosestChestEquip = ChestItemList.CheckClosestChestEquip();
        Assert(ChestItemList.IsClosestChestEquip && chestSearch.SearchText == "royal" && chestSearch.CurrentScrollPosition.y == 0 &&
            Navigation.buttonIndex == 3, "Switching to an equipment chest must retain the query and normalize scroll/selection.");
        Assert(chest.GetButtons().Where(button => button.GetAction() != null).Select(button => Row(button).Text)
            .SequenceEqual(new[] { "Royal Capacitor", "Royal Aspect" }), "Equipment chests must filter their equipment catalog.");
        chest.GetButtons().Single(button => Row(button).Text == "Royal Aspect").Activate();
        Assert(equipmentChest.currentPickup.pickupIndex == PickupCatalog.FindPickupIndex(aspect) &&
            Root.LocalPlayerInv.currentEquipmentIndex == capacitor,
            "A filtered chest-equipment callback must change the chest pickup without equipping it on the player.");
        var beforeInvalid = equipmentChest.currentPickup.pickupIndex;
        ChestItemList.SetChestItem((ItemIndex)99999); ChestItemList.SetChestEquipment(EquipmentIndex.None);
        Assert(equipmentChest.currentPickup.pickupIndex == beforeInvalid, "Invalid pickups must not erase a selected chest.");
        chestSearch.SetSearchText("nothing");
        Assert(Row(chest.GetButtons().Single()).Text == "No equipment matches your search." && Navigation.buttonIndex == 0,
            "An empty equipment-chest search must show the matching equipment empty state.");
        chestSearch.SetSearchText("lens"); ChestItemList.IsClosestChestEquip = false;
        Assert(chest.GetButtons().Count(button => button.GetAction() != null) == 3 && chestSearch.SearchText == "lens",
            "Returning to item-chest mode must reuse the retained query.");

        equipment.SetEnabled(false); chest.SetEnabled(false);
        Assert(!equipment.IsSearchFocused() && !chest.IsSearchFocused() && SceneDirector.Subscribers == 0 &&
            ChestItemList.selectedChest == null && ChestItemList.chests.Count == 0 && !Items.chestItemList,
            "Closing chest search must release its scene subscription, targets, and shortcut guards.");
        UnityEngine.Object.SceneObjects = new UnityEngine.Object[0];
        chest.SetEnabled(true);
        Assert(ChestItemList.FindClosestChest() == null && !ChestItemList.CheckClosestChestEquip(),
            "Opening Change Chest Item with no chests must remain safe.");
        ChestItemList.SetChestItem(lens); ChestItemList.SetChestEquipment(capacitor); ChestItemList.RenderClosestChest();
        Assert(ChestItemList.selectedChest == null, "Chest actions and rendering must safely skip an empty scene.");
        var opened = new ChestBehavior(PickupIndex.none);
        var destroyed = new ChestBehavior(PickupCatalog.FindPickupIndex(lens)) { Destroyed = true };
        var tie = new ChestBehavior(PickupCatalog.FindPickupIndex(lens)); tie.transform.position = locked.transform.position;
        ChestItemList.chests = new List<ChestBehavior> { null, opened, destroyed, locked, tie };
        ChestItemList.selectedChest = destroyed;
        Assert(ChestItemList.GetSelectedChest() == locked,
            "Destroyed/opened chests must be ignored, and equally close targets must select consistently without exceptions.");
        locked.Destroyed = true;
        Assert(ChestItemList.GetSelectedChest() == tie, "A destroyed locked chest must be replaced by a valid nearby target.");
        Camera.main = null; ChestItemList.selectedChest = null;
        Assert(ChestItemList.FindClosestChest() == null && !ChestItemList.CheckClosestChestEquip(),
            "Missing cameras during transitions must not throw.");
        ChestItemList.RenderClosestChest();
        Camera.main = new Camera();
        var unknown = new ChestBehavior(new PickupIndex(888888));
        ChestItemList.chests = new List<ChestBehavior> { unknown }; ChestItemList.selectedChest = unknown;
        Assert(!ChestItemList.CheckClosestChestEquip(), "An unknown pickup definition must be handled safely.");
        ChestItemList.RenderClosestChest();
        chest.Reset();
        Assert(SceneDirector.Subscribers == 0 && ChestItemList.chests.Count == 0 && ChestItemList.selectedChest == null,
            "Reset must release chest subscriptions and object references.");
        Camera.main = null; UnityEngine.Object.SceneObjects = new UnityEngine.Object[0]; GUI.FocusControl(null);
        Root.navigationToggle = false;

        // Render the actual shared search layout for both menus.
        Root.mainMenu = Root.menus[0]; Root.mainMenu.SetEnabled(true);
        Screen.width = 1000; Screen.height = 800; Root.WindowWidth = 680; Root.WindowHeight = 560; MenuUI.ResetWindow();
        equipmentSearch.SetSearchText("royal"); MenuUI.NavigateTo(13);
        Event.current = new Event { type = EventType.Repaint }; GUI.StartCanvas(); MenuUI.Draw();
        GUI.Save("artifacts/tests/previews/equipment-search.png");
        MenuUI.NavigateTo(14); chestSearch.SetSearchText("lens");
        GUI.StartCanvas(); MenuUI.Draw(); GUI.Save("artifacts/tests/previews/chest-search.png");
        chest.SetEnabled(false); equipment.SetEnabled(false); Root.mainMenu.SetEnabled(false);
        Assert(SceneDirector.Subscribers == 0, "Preview navigation must leave no chest subscriptions behind.");
        Screen.width = previousScreenWidth; Screen.height = previousScreenHeight;
    }
}
