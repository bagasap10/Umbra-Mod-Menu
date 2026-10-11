using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UmbraMenu;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace RoR2.ExpansionManagement { public class ExpansionDef { public string nameToken,name; } }
namespace RoR2
{
    public struct EquipmentLocation { public uint slot, set; }
    public sealed class WeightedSelection<T> { }
    public enum ItemTier { Tier1,Tier2,Tier3,Lunar,Boss,NoTier,VoidTier1,VoidTier2,VoidTier3,VoidBoss,FoodTier,Cursed,Page,Hex,AssignedAtRuntime=1000 }
    public enum ItemIndex { None=-1 }
    public enum EquipmentIndex { None=-1 }
    public class ItemDef : UnityEngine.Object { public string name; public Sprite Icon; public UnityEngine.AddressableAssets.AssetReferenceT<Sprite> IconReference;public Sprite GetIcon()=>Icon;public UnityEngine.AddressableAssets.AssetReferenceT<Sprite> GetIconReference()=>IconReference; public bool hidden; public ItemTier tier; public string nameToken; public ExpansionDef requiredExpansion; }
    public class EquipmentDef { public Sprite Icon;public UnityEngine.AddressableAssets.AssetReferenceT<Sprite> IconReference;public Sprite GetIcon()=>Icon;public UnityEngine.AddressableAssets.AssetReferenceT<Sprite> GetIconReference()=>IconReference; public bool isLunar; public string nameToken,name; public ExpansionDef requiredExpansion; public ColorCatalog.ColorIndex colorIndex; }
    public static class Language { public static readonly Dictionary<string,string> Names=new Dictionary<string,string>(); public static string GetString(string token) { string value; return token!=null && Names.TryGetValue(token,out value) ? value : token; } }
    public static class ItemCatalog {public static ItemIndex FindItemIndex(string name)=>Defs.Where(pair=>pair.Value.name==name).Select(pair=>pair.Key).DefaultIfEmpty(ItemIndex.None).First(); public static readonly Dictionary<ItemIndex,ItemDef> Defs=new Dictionary<ItemIndex,ItemDef>(); public static IEnumerable<ItemIndex> allItems { get { return Defs.Keys; } } public static ItemDef GetItemDef(ItemIndex index) { ItemDef value; return Defs.TryGetValue(index,out value) ? value : null; } }
    public class Inventory
    {
        public static implicit operator bool(Inventory value)=>value!=null;public int activeEquipmentSlot;public EquipmentIndex currentEquipmentIndex=EquipmentIndex.None;public EquipmentLocation LastLocation;public void SetEquipmentIndex(EquipmentIndex index,EquipmentLocation location){currentEquipmentIndex=index;LastLocation=location;}public void SetEquipmentIndex(EquipmentIndex index,bool removing){currentEquipmentIndex=index;}public Dictionary<ItemIndex,int> Counts=new Dictionary<ItemIndex,int>(),Temporary=new Dictionary<ItemIndex,int>(); public int ReadCalls,RemovalCalls; public bool AllowRemoval=true;
        public int GetItemCountPermanent(ItemIndex index) { ReadCalls++; int value; return Counts.TryGetValue(index,out value) ? value : 0; }
        public void RemoveItemPermanent(ItemIndex index,int amount) { RemovalCalls++; if(AllowRemoval) Counts[index]=Math.Max(0,GetItemCountPermanent(index)-amount); }
        public void GiveItemPermanent(ItemIndex index,int amount) { Counts[index]=GetItemCountPermanent(index)+amount; }
    }
    public class Master { public static implicit operator bool(Master value) { return value!=null; } }
    public class MasterController { public Master master=new Master(); public static implicit operator bool(MasterController value) { return value!=null; } }
    public class LocalUser { public MasterController cachedMasterController=new MasterController(); }
    public static class LocalUserManager { public static LocalUser GetFirstLocalUser() { return new LocalUser(); } }
    public static class Chat { public static void AddMessage(string message) {} }
    public static class EquipmentCatalog {public static EquipmentIndex FindEquipmentIndex(string name)=>Defs.Where(pair=>pair.Value.name==name).Select(pair=>pair.Key).DefaultIfEmpty(EquipmentIndex.None).First(); public static readonly Dictionary<EquipmentIndex,EquipmentDef> Defs=new Dictionary<EquipmentIndex,EquipmentDef>(); public static EquipmentDef GetEquipmentDef(EquipmentIndex index) { EquipmentDef value; return Defs.TryGetValue(index,out value) ? value : null; } }
    public static class EliteCatalog { public static readonly HashSet<EquipmentIndex> Indices=new HashSet<EquipmentIndex>(); public static object GetEliteDefFromEquipmentIndex(EquipmentIndex index) { return Indices.Contains(index) ? new object() : null; } }
    public static class ColorCatalog { public enum ColorIndex { Equipment,LunarItem } public static Color32 GetColor(ColorIndex index) { return new Color32(255,200,100,255); } }
    public static class Util { public static string GenerateColoredString(string text,Color32 color) { return text; } }
}
namespace UmbraMenu
{
    public static class UmbraMenu { public static List<Menu> menus=new List<Menu>(); public static List<ItemIndex> items=new List<ItemIndex>();public static List<EquipmentIndex> equipment=new List<EquipmentIndex>();public static global::UmbraMenu.Menus.InventoryItemList inventoryItemListMenu; public static Menu chestItemListMenu; public const string VERSION="4.1.1"; public const string DLC_NAME="Hallowed Concepts"; public static bool AllowNavigation=true; public static bool forceFullModMenu,listenForKeybind,chatOpen; public static Menu mainMenu,playerMenu,movementMenu,teleporterMenu,cheatsMenu; public static Dictionary<string,Keybind> keybindDict=new Dictionary<string,Keybind>(); public static bool navigationToggle,lowResolutionMonitor,scrolled,characterCollected,InGame=true; public static int previousMenuOpen; public static float Width=350,WindowWidth=680,WindowHeight=560,GlassOpacity=.62f; public static CharacterMaster LocalPlayer; public static Inventory LocalPlayerInv; public static CharacterBody LocalPlayerBody; public static bool InGameCheck() { return InGame; } public static void GetCharacter() {} }
    public static class Utility { public static void StubbedFunction(){} public static void SaveSettings(){} public static Button FindButtonById(int menuId,int buttonId) { return UmbraMenu.menus[menuId].GetButtons().First(button=>button.GetId()==buttonId); } }
    public static class Styles { public static GUIStyle OnStyle=new GUIStyle(),OffStyle=new GUIStyle(); public static GUIStyle SelectedChestStyle=new GUIStyle(); public static GUIStyle LabelStyle=new GUIStyle(),MainBgStyle=new GUIStyle(),BtnStyle=new GUIStyle(),HighlightBtnStyle=new GUIStyle(),TitleStyle=new GUIStyle(),CornerStyle=new GUIStyle(); }
}

partial class Checks
{
    static int assertions;
    static readonly FieldInfo Inner=typeof(Button).GetField("_button",BindingFlags.Instance|BindingFlags.NonPublic);
    static IMenu Layout(Menu menu) { return (IMenu)typeof(Menu).GetField("_menu",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(menu); }
    static Menu PlainMenu(int id) { return new Menu(new ListMenu(id,new Rect(0,0,20,20),"Test")); }
    static IButton Row(Button button) { return (IButton)Inner.GetValue(button); }
    static void Assert(bool condition,string message) { assertions++; if(!condition) throw new Exception(message); }
    static ExpansionDef Expansion(int dlc) { return dlc==0 ? null : new ExpansionDef { nameToken="DLC"+dlc+"_NAME",name="DLC"+dlc }; }
    static ItemIndex Item(int id,ItemTier tier,string name,int dlc=0,bool hidden=false)
    {
        var index=(ItemIndex)id; string token="ITEM_"+id; Language.Names[token]=name;
        ItemCatalog.Defs[index]=new ItemDef { tier=tier,nameToken=token,requiredExpansion=Expansion(dlc),hidden=hidden }; return index;
    }
    static int Main() { try { RunChecks(); PackChecks(); WorldCheatsChecks(); CharacterCheatsChecks(); return 0; } catch(Exception e) { Console.Error.WriteLine(e); return 1; } }
    static void RunChecks()
    {
        typeof(MenuUI).GetField("body",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new Rect(0,0,600,500));
        string[] rarityTitles={ "White / Common","Green / Uncommon","Red / Legendary","Yellow / Boss & Planet","Blue / Lunar","Purple / Void","Orange / Meal","Purple / Hallowed","Green / Hex","Blue / N'tormat Pages" };
        ItemTier[] tiers={ ItemTier.Tier1,ItemTier.Tier2,ItemTier.Tier3,ItemTier.Boss,ItemTier.Lunar,ItemTier.VoidTier1,ItemTier.FoodTier,ItemTier.Cursed,ItemTier.Hex,ItemTier.Page };
        string[] expansionTitles={ "Normal / Not-DLC","DLC#1 / Survivors of the Void","DLC#2 / Seekers of the Storm","DLC#3 / Alloyed Collective","DLC#4 / Hallowed Concepts" };
        List<ItemIndex> indices=new List<ItemIndex>(); List<string> expected=new List<string>(); int id=0;
        for(int rarity=0;rarity<tiers.Length;rarity++)
        {
            expected.Add(rarityTitles[rarity]);
            for(int dlc=0;dlc<=4;dlc++)
            {
                string alpha="Alpha "+rarity+" "+dlc, zebra="Zebra "+rarity+" "+dlc;
                indices.Add(Item(id++,tiers[rarity],zebra,dlc)); indices.Add(Item(id++,tiers[rarity],alpha,dlc));
                expected.Add(expansionTitles[dlc]); expected.Add(alpha); expected.Add(zebra);
            }
        }
        indices.Reverse(); List<ItemIndex> given=new List<ItemIndex>(); Menu menu=PlainMenu(12);
        List<Button> rows=CatalogGrouping.BuildItemButtons(menu,indices,index=>given.Add(index)); menu.AddButtons(rows);
        Assert(rows.Select(button=>Row(button).Text).SequenceEqual(expected),"All ten rarities must contain alphabetical base-game and DLC1-DLC4 sections in order.");
        Assert(rows.Select(button=>button.GetId()).SequenceEqual(Enumerable.Range(1,rows.Count)),"Divider and item row positions must remain contiguous.");
        foreach(Button button in rows.Where(button=>button.GetAction()!=null)) button.GetAction()();
        Assert(given.Count==100 && given.Distinct().Count()==100,"Every item action must still grant its own catalog item after sorting.");
        foreach(Button button in rows.Where(button=>button.GetAction()==null)) { int before=GUI.ButtonCalls; button.Draw(); Assert(GUI.ButtonCalls==before,"Divider rows must render without clickable GUI buttons."); }
        menu.SetNumberOfButtons(rows.Count);
        var voidIndices=new[] { Item(id++,ItemTier.VoidBoss,"Void Boss",1),Item(id++,ItemTier.VoidTier3,"Void Red",1),Item(id++,ItemTier.VoidTier2,"Void Green",1),Item(id++,ItemTier.VoidTier1,"Void White",1) };
        var voidRows=CatalogGrouping.BuildItemButtons(menu,voidIndices,index=>{});
        Assert(voidRows.Count==6 && Row(voidRows[0]).Text=="Purple / Void","All four void tiers must share one rarity and one DLC divider.");
        var hidden=Item(id++,ItemTier.Tier1,"Hidden",0,true); var noTier=Item(id++,ItemTier.NoTier,"Internal"); var missing=Item(id++,ItemTier.Tier1,"Missing"); Language.Names.Remove(ItemCatalog.Defs[missing].nameToken);
        Assert(CatalogGrouping.BuildItemButtons(menu,new[] { hidden,noTier,missing,(ItemIndex)9999 },index=>{}).Count==0,"Invalid, hidden, internal and unlocalized items must not create empty headings.");
        var unknown=Item(id++,ItemTier.AssignedAtRuntime,"Mod Item"); ItemCatalog.Defs[unknown].requiredExpansion=new ExpansionDef { nameToken="MOD_NAME",name="Mod" }; Language.Names["MOD_NAME"]="Mod Expansion";
        var unknownRows=CatalogGrouping.BuildItemButtons(menu,new[] { unknown },index=>{});
        Assert(Row(unknownRows[0]).Text=="Other Items" && Row(unknownRows[1]).Text=="Expansion / Mod Expansion","Unknown tiers and expansions must remain visible rather than become base-game content.");

        List<EquipmentIndex> equipment=new List<EquipmentIndex>(); List<string> equipmentExpected=new List<string>(); int equipId=0;
        string[] equipmentTitles={ "Orange / Equipment","Blue / Lunar Equipment","Elite Equipment" };
        for(int group=0;group<3;group++)
        {
            equipmentExpected.Add(equipmentTitles[group]);
            for(int dlc=0;dlc<=4;dlc++)
            {
                equipmentExpected.Add(expansionTitles[dlc]);
                foreach(string prefix in new[] { "Zebra","Alpha" })
                {
                    var index=(EquipmentIndex)equipId++; string name=prefix+" Equip "+group+" "+dlc; string token="EQUIP_"+equipId; Language.Names[token]=name;
                    EquipmentCatalog.Defs[index]=new EquipmentDef { nameToken=token,name=name,requiredExpansion=Expansion(dlc),isLunar=group!=0 };
                    if(group==2) EliteCatalog.Indices.Add(index); equipment.Add(index);
                }
                equipmentExpected.Add("Alpha Equip "+group+" "+dlc); equipmentExpected.Add("Zebra Equip "+group+" "+dlc);
            }
        }
        equipment.Reverse(); var equipped=new HashSet<EquipmentIndex>(); var equipmentRows=CatalogGrouping.BuildEquipmentButtons(menu,equipment.Concat(new[] { EquipmentIndex.None,(EquipmentIndex)9999 }),index=>equipped.Add(index));
        Assert(equipmentRows.Select(button=>Row(button).Text).SequenceEqual(equipmentExpected),"Equipment must use normal, lunar and elite groups with DLC sections; lunar elite aspects must stay in Elite Equipment.");
        foreach(var button in equipmentRows.Where(button=>button.GetAction()!=null)) button.GetAction()();
        Assert(equipped.Count==30,"Every equipment callback must retain its own equipment index.");

        for(int i=0;i<17;i++) UmbraMenu.UmbraMenu.menus.Add(PlainMenu(i)); UmbraMenu.UmbraMenu.menus[12]=menu;
        Navigation.menuIndex=12; Navigation.buttonIndex=1; UmbraMenu.UmbraMenu.scrolled=false;
        Navigation.UpdateIndexValues(); Assert(Navigation.buttonIndex==3 && Layout(menu).CurrentScrollPosition.y==0,"Entering a grouped list must skip two headings while keeping them visible.");
        int first=Navigation.buttonIndex; var traversed=new List<int>();
        do { Assert(Utility.FindButtonById(12,Navigation.buttonIndex).GetAction()!=null,"Forward navigation must skip every divider."); traversed.Add(Navigation.buttonIndex); Navigation.MoveSelection(1); } while(Navigation.buttonIndex!=first);
        Assert(traversed.Count==100,"Forward navigation must visit every item once and wrap to the first item.");
        Navigation.MoveSelection(-1); Assert(Navigation.buttonIndex==rows.Last(button=>button.GetAction()!=null).GetId(),"Backward navigation must wrap past leading headings.");
        for(int i=0;i<100;i++) { Assert(Utility.FindButtonById(12,Navigation.buttonIndex).GetAction()!=null,"Backward navigation must skip every divider."); Navigation.MoveSelection(-1); }
        int grantedBefore=given.Count; Navigation.PressBtn(12,1); Navigation.PressBtn(12,0);
        Assert(given.Count==grantedBefore && !rows[0].IsEnabled(),"Selecting a heading or empty selection must do nothing.");
        Navigation.buttonIndex=4; Navigation.UpdateIndexValues(); Assert(Navigation.buttonIndex==4,"Updating selection must preserve a valid actionable index.");
        menu.SetScrollPosition(new Vector2(0,123)); UmbraMenu.UmbraMenu.scrolled=true; Navigation.UpdateIndexValues(); Assert(Layout(menu).CurrentScrollPosition.y==123,"Manual scrolling must be preserved.");
        menu.AddButtons(new List<Button>()); Navigation.MoveSelection(1); Navigation.UpdateIndexValues(); Navigation.PressBtn(12,0); Assert(Navigation.buttonIndex==0,"Empty menus must remain safe to navigate.");
        InventoryChecks();
        SearchChecks();
        PortalChecks(); EspChecks(); PortalSpawnChecks();
        CatalogSearchChecks();
        Console.WriteLine("PASS: "+assertions+" assertions covering grouped portals, verified asset keys, async/network spawning, enemy discovery, projected ESP boxes, DLC interactables, and previous item menus.");
    }

    static Button InventoryRow(UmbraMenu.Menus.InventoryItemList menu,string label)
    {
        return menu.GetButtons().First(button=>Row(button).Text==label);
    }

    static void SearchChecks()
    {
        ItemCatalog.Defs.Clear();
        var syringe=Item(3000,ItemTier.Tier1,"Soldier's Syringe");
        var lens=Item(3001,ItemTier.Tier1,"Lens-Maker's Glasses");
        var dlcLens=Item(3002,ItemTier.Tier1,"Lens Focus",1);
        var voidLens=Item(3003,ItemTier.VoidTier1,"Lost Seer's Lenses",1);
        var feather=Item(3004,ItemTier.Tier2,"Hopoo Feather");
        UmbraMenu.UmbraMenu.items=ItemCatalog.allItems.ToList();
        var inventory=new Inventory(); inventory.Counts[lens]=2; inventory.Counts[syringe]=3; inventory.Counts[feather]=1;
        UmbraMenu.UmbraMenu.LocalPlayerInv=inventory;
        UmbraMenu.UmbraMenu.navigationToggle=false; UmbraMenu.UmbraMenu.scrolled=false;
        var parent=UmbraMenu.UmbraMenu.menus[3];
        parent.AddButtons(new List<Button> { new Button(new NormalButton(parent,6,"SPAWN",()=>{})),new Button(new NormalButton(parent,8,"REMOVE",()=>{})) });
        var spawn=new UmbraMenu.Menus.ItemList(); UmbraMenu.UmbraMenu.menus[12]=spawn;
        var spawnSearch=(SearchableListMenu)Layout(spawn); spawn.SetEnabled(true);
        string[] all=spawn.GetButtons().Select(button=>Row(button).Text).ToArray();
        spawnSearch.CurrentScrollPosition=new Vector2(0,500);
        spawnSearch.SetSearchText(" LeNs ");
        string[] matched={ "White / Common","Normal / Not-DLC","Lens-Maker's Glasses","DLC#1 / Survivors of the Void","Lens Focus","Purple / Void","DLC#1 / Survivors of the Void","Lost Seer's Lenses" };
        Assert(spawn.GetButtons().Select(button=>Row(button).Text).SequenceEqual(matched),"A partial name search must ignore case/outer whitespace, retain only matching rarity/DLC groups, and keep their order.");
        Assert(spawnSearch.CurrentScrollPosition.y==0,"Changing a query must return a scrolled list to the first match.");
        Assert(spawn.GetButtons().Select(button=>button.GetId()).SequenceEqual(Enumerable.Range(1,matched.Length)),"Filtered rows must have contiguous positions for keyboard navigation.");
        spawnSearch.SetSearchText("lens-maker");
        var lensButton=spawn.GetButtons().Single(button=>button.GetAction()!=null); lensButton.GetAction()();
        Assert(inventory.Counts[lens]==3 && inventory.Counts[syringe]==3 && inventory.Counts[feather]==1,"A filtered spawn callback must give only the chosen item.");
        UmbraMenu.UmbraMenu.navigationToggle=true; Navigation.menuIndex=12; Navigation.buttonIndex=3;
        spawnSearch.SetSearchText("nothing matches");
        Assert(spawn.GetButtons().Count==1 && Row(spawn.GetButtons()[0]).Text=="No items match your search." && spawn.GetButtons()[0].GetAction()==null && Navigation.buttonIndex==0,"An unmatched query must provide a safe, nonclickable message with no keyboard selection.");
        Navigation.PressBtn(12,0);
        Event.current=new Event { type=EventType.Repaint }; GUI.ClickLabel="Clear"; spawn.Draw();
        Assert(spawnSearch.SearchText=="" && spawn.GetButtons().Select(button=>Row(button).Text).SequenceEqual(all),"The Clear button must restore every spawn item and section.");
        Assert(Navigation.buttonIndex==spawn.GetButtons().First(button=>button.GetAction()!=null).GetId() && spawnSearch.CurrentScrollPosition.y==0,"Changing a query in keyboard navigation must select the first result and retain the leading dividers.");
        Assert(spawn.IsSearchFocused(),"Clear must return focus to the search field for continued typing.");
        Assert(GUI.SearchRect.y+GUI.SearchRect.height<=GUI.Viewport.y && GUI.DrawnRows["White / Common"].y==0,"The fixed search control must sit above, and never overlap, the first scrolling divider.");
        float searchY=GUI.SearchRect.y; spawnSearch.CurrentScrollPosition=new Vector2(0,90); spawn.Draw();
        Assert(GUI.SearchRect.y==searchY && spawnSearch.CurrentScrollPosition.y==90,"Scrolling the results must keep the search field stationary.");
        Event.current=new Event { type=EventType.KeyDown,keyCode=KeyCode.Escape }; spawn.Draw();
        Assert(!spawn.IsSearchFocused() && Event.current.type==EventType.Used,"Escape must leave text entry and consume that GUI key event.");
        foreach(var key in new[] { KeyCode.Return,KeyCode.KeypadEnter })
        {
            GUI.FocusControl("UmbraItemSearch_12"); Event.current=new Event { type=EventType.Repaint }; spawn.Draw();
            Event.current=new Event { type=EventType.KeyDown,keyCode=key }; spawn.Draw();
            Assert(!spawn.IsSearchFocused() && Event.current.type==EventType.Used,"Enter must leave text entry without executing an item action.");
        }
        Event.current=new Event { type=EventType.MouseDown,mousePosition=new Vector2(GUI.SearchRect.x+10,GUI.SearchRect.y+10) }; spawn.Draw();
        Assert(spawn.IsSearchFocused(),"Clicking the search field must enable the text-entry guard.");
        Event.current=new Event { type=EventType.MouseDown,mousePosition=new Vector2(900,900) }; spawn.Draw();
        Assert(!spawn.IsSearchFocused(),"Clicking outside the search field must restore shortcut input.");
        Event.current=new Event { type=EventType.Repaint }; GUI.FocusControl("UmbraItemSearch_12"); spawn.Draw(); spawn.SetEnabled(false);
        Assert(!spawn.IsSearchFocused(),"Closing a focused search menu must immediately release its shortcut guard.");
        spawn.SetEnabled(true); GUI.FocusControl(null); spawnSearch.SetSearchText("   ");
        Assert(spawn.GetButtons().Select(button=>Row(button).Text).SequenceEqual(all),"A whitespace-only query must behave like an empty search.");
        spawnSearch.SetSearchText("syringe");

        var removal=new UmbraMenu.Menus.InventoryItemList(); UmbraMenu.UmbraMenu.menus[17]=removal; removal.SetEnabled(true); removal.RefreshInventory(true);
        var removeSearch=(SearchableListMenu)Layout(removal); Navigation.menuIndex=17;
        removeSearch.SetSearchText("lens");
        Assert(removal.GetButtons().Select(button=>Row(button).Text).SequenceEqual(new[] { "White / Common","Normal / Not-DLC","Lens-Maker's Glasses (x3)" }),"Removal searches must contain only matching owned permanent stacks, excluding unowned catalog matches.");
        Assert(spawnSearch.SearchText=="syringe" && removeSearch.SearchText=="lens","The two search menus must have independent queries.");
        int remaining=3;
        while(remaining>0)
        {
            var rows=removal.GetButtons(); rows.Single(button=>button.GetAction()!=null).GetAction()(); remaining--;
            Assert(ReferenceEquals(rows,removal.GetButtons()),"Removing a filtered stack must defer the list rebuild until GUI iteration is complete.");
            removal.RefreshInventory();
            Assert(inventory.Counts[lens]==remaining && inventory.Counts[syringe]==3,"A filtered removal must decrement the correct item exactly once.");
            if(remaining>0) Assert(InventoryRow(removal,"Lens-Maker's Glasses (x"+remaining+")")!=null,"Counts must refresh without clearing an active query.");
        }
        Assert(removeSearch.SearchText=="lens" && Row(removal.GetButtons().Single()).Text=="No owned items match your search." && Navigation.buttonIndex==0,"Removing the last matching item must preserve the query and show a nonclickable no-match state.");
        inventory.Counts[dlcLens]=2; Time.unscaledTime+=0.3f; removal.RefreshInventory();
        Assert(removal.GetButtons().Select(button=>Row(button).Text).SequenceEqual(new[] { "White / Common","DLC#1 / Survivors of the Void","Lens Focus (x2)" }),"A newly acquired matching item must appear automatically with its DLC divider while the filter remains active.");
        removeSearch.SetSearchText("x2");
        Assert(Row(removal.GetButtons().Single()).Text=="No owned items match your search.","Search must match item names rather than the appended count label.");
        GUI.FocusControl(null); GUI.ClickLabel="Clear"; Event.current=new Event { type=EventType.Repaint }; removal.Draw();
        Assert(removeSearch.SearchText=="" && removal.GetButtons().Count(button=>button.GetAction()!=null)==3,"Clearing removal search must restore all owned items without showing unowned entries.");
        Assert(!removal.GetButtons().Any(button=>Row(button).Text.Contains("Lost Seer")),"Clearing removal search must still respect ownership.");
        Assert(removal.IsSearchFocused(),"Removal search must track its own focused text control.");
        spawn.Draw(); Assert(!spawn.IsSearchFocused() && removal.IsSearchFocused(),"Focusing one search control must not focus the other menu.");
        removeSearch.SetSearchText("lens"); UmbraMenu.UmbraMenu.LocalPlayerInv=null; removal.RefreshInventory(true);
        Assert(Row(removal.GetButtons().Single()).Text=="No player inventory available.","A lost player inventory must override filtered results and clear stale callbacks.");
        removal.SetEnabled(false); spawn.SetEnabled(false);
        var normal=PlainMenu(11); var normalButton=new NormalButton(normal,1,"Normal row",()=>{}); normalButton.Draw();
        Assert(normal.GetContentOffset()==0 && GUI.DrawnRows["Normal row"].y==0,"Adding search offsets must preserve ordinary list row positions.");
        UmbraMenu.UmbraMenu.navigationToggle=false;
    }

    static void InventoryChecks()
    {
        var alpha=Item(2000,ItemTier.Tier1,"Alpha White"); var zebra=Item(2001,ItemTier.Tier1,"Zebra White"); var beta=Item(2002,ItemTier.Tier2,"Beta Green",4);
        var temporary=Item(2003,ItemTier.Tier3,"Temporary Grant"); var aardvark=Item(2004,ItemTier.Tier1,"Aardvark White");
        var inventory=new Inventory(); inventory.Counts[alpha]=2; inventory.Counts[zebra]=3; inventory.Counts[beta]=1; inventory.Temporary[temporary]=7;
        UmbraMenu.UmbraMenu.LocalPlayerInv=inventory; UmbraMenu.UmbraMenu.characterCollected=true; UmbraMenu.UmbraMenu.InGame=true;
        UmbraMenu.UmbraMenu.menus[3].AddButtons(new List<Button> { new Button(new NormalButton(UmbraMenu.UmbraMenu.menus[3],8,"REMOVE ITEMS",()=>{})) });
        var removalMenu=new UmbraMenu.Menus.InventoryItemList(); UmbraMenu.UmbraMenu.menus.Add(removalMenu);
        UmbraMenu.UmbraMenu.navigationToggle=false; removalMenu.RefreshInventory(); Assert(inventory.ReadCalls==0,"Closed removal menus must not poll inventory.");
        removalMenu.SetEnabled(true); Time.unscaledTime=10; removalMenu.RefreshInventory(true);
        string[] expected={ "White / Common","Normal / Not-DLC","Alpha White (x2)","Zebra White (x3)","Green / Uncommon","DLC#4 / Hallowed Concepts","Beta Green (x1)" };
        Assert(removalMenu.GetButtons().Select(button=>Row(button).Text).SequenceEqual(expected),"Removal menu must show only owned stacks, with counts and rarity/DLC sections.");
        Assert(!removalMenu.GetButtons().Any(button=>Row(button).Text.Contains("Temporary Grant")),"Temporary grants must not be offered for permanent-item removal.");
        var originalRows=removalMenu.GetButtons(); int reads=inventory.ReadCalls; removalMenu.RefreshInventory();
        Assert(inventory.ReadCalls==reads,"Inventory polling must be throttled between refreshes.");
        Time.unscaledTime+=0.3f; removalMenu.RefreshInventory(); Assert(ReferenceEquals(originalRows,removalMenu.GetButtons()),"Unchanged inventory must not rebuild its button list.");

        Navigation.menuIndex=17; Navigation.buttonIndex=InventoryRow(removalMenu,"Zebra White (x3)").GetId(); UmbraMenu.UmbraMenu.navigationToggle=true; UmbraMenu.UmbraMenu.scrolled=false;
        inventory.Counts[aardvark]=1; Time.unscaledTime+=0.3f; removalMenu.RefreshInventory();
        Assert(Row(Utility.FindButtonById(17,Navigation.buttonIndex)).Text=="Zebra White (x3)","New pickups before the selection must keep the same item highlighted.");
        inventory.Counts[zebra]=4; Time.unscaledTime+=0.3f; removalMenu.RefreshInventory();
        Assert(Row(Utility.FindButtonById(17,Navigation.buttonIndex)).Text=="Zebra White (x4)","Stack changes must refresh the count without changing the selected item.");

        var oldAlphaButton=InventoryRow(removalMenu,"Alpha White (x2)"); var beforeClick=removalMenu.GetButtons(); oldAlphaButton.GetAction()();
        Assert(inventory.GetItemCountPermanent(alpha)==1 && inventory.GetItemCountPermanent(zebra)==4,"A click must remove exactly one copy of the chosen item.");
        Assert(ReferenceEquals(beforeClick,removalMenu.GetButtons()),"A removal callback must defer rebuilding until after GUI iteration.");
        removalMenu.RefreshInventory(); Assert(InventoryRow(removalMenu,"Alpha White (x1)")!=null,"The next refresh must update the remaining stack count.");
        oldAlphaButton.GetAction()(); removalMenu.RefreshInventory();
        Assert(!removalMenu.GetButtons().Any(button=>Row(button).Text.StartsWith("Alpha White")),"Items must disappear from the list after their last copy is removed.");
        int attempts=inventory.RemovalCalls; oldAlphaButton.GetAction()();
        Assert(inventory.RemovalCalls==attempts && inventory.GetItemCountPermanent(alpha)==0,"Stale callbacks must not remove missing items or create negative stacks.");
        Assert(inventory.GetItemCountPermanent(beta)==1 && inventory.GetItemCountPermanent(zebra)==4,"Removing an item must leave unrelated stacks unchanged.");
        Assert(!UmbraMenu.Menus.InventoryItemList.RemoveItem((ItemIndex)9999),"Invalid catalog indices must be rejected.");
        inventory.Counts[alpha]=-2; Assert(!UmbraMenu.Menus.InventoryItemList.RemoveItem(alpha),"Nonpositive stacks must never be decremented."); inventory.Counts[alpha]=0;
        inventory.AllowRemoval=false; Assert(!UmbraMenu.Menus.InventoryItemList.RemoveItem(beta) && inventory.Counts[beta]==1,"Removal must report failure when the game does not apply the inventory change."); inventory.AllowRemoval=true;
        UmbraMenu.UmbraMenu.InGame=false; Assert(!UmbraMenu.Menus.InventoryItemList.RemoveItem(beta),"Removal must not operate on cached inventory outside a run."); UmbraMenu.UmbraMenu.InGame=true;

        var replacement=new Inventory(); replacement.Counts[beta]=2; UmbraMenu.UmbraMenu.LocalPlayerInv=replacement; removalMenu.RefreshInventory();
        Assert(removalMenu.GetButtons().Any(button=>Row(button).Text=="Beta Green (x2)") && !removalMenu.GetButtons().Any(button=>Row(button).Text.StartsWith("Zebra")),"Changing player inventory must refresh immediately, even within the polling interval.");
        InventoryRow(removalMenu,"Beta Green (x2)").GetAction()(); removalMenu.RefreshInventory();
        InventoryRow(removalMenu,"Beta Green (x1)").GetAction()(); removalMenu.RefreshInventory();
        Assert(removalMenu.GetButtons().Count==1 && Row(removalMenu.GetButtons()[0]).Text=="No items to remove.","An emptied inventory must show a nonclickable empty message.");
        Assert(Navigation.buttonIndex==0 && removalMenu.GetButtons()[0].GetAction()==null,"An empty inventory must have no actionable keyboard selection.");
        Navigation.PressBtn(17,0); Navigation.MoveSelection(1); Assert(Navigation.buttonIndex==0,"Keyboard navigation must remain safe after deleting the last item.");
        UmbraMenu.UmbraMenu.LocalPlayerInv=null; Assert(!UmbraMenu.Menus.InventoryItemList.RemoveItem(beta),"Removal must tolerate an unavailable player inventory."); removalMenu.RefreshInventory(true);
        Assert(Row(removalMenu.GetButtons()[0]).Text=="No player inventory available.","Unavailable inventory must clear stale item rows.");
        removalMenu.SetEnabled(false); UmbraMenu.UmbraMenu.LocalPlayerInv=inventory; reads=inventory.ReadCalls; removalMenu.RefreshInventory();
        Assert(inventory.ReadCalls==reads,"Closing the menu must stop inventory polling.");
        removalMenu.SetEnabled(true); removalMenu.RefreshInventory(true); Assert(InventoryRow(removalMenu,"Zebra White (x4)")!=null,"Reopening the removal menu must read the current inventory immediately.");
    }
}

namespace UmbraMenu { public class Keybind { public UnityEngine.KeyCode KeyCode; } public static class Loader { public static bool devBuild,updateAvailable,upToDate; public static void Unload(){} public static void Report(string state,string message,string shortcut="") {} } }





