using System;
using System.Collections.Generic;
using System.Linq;
using UmbraMenu;
using UmbraMenu.Menus;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Root=UmbraMenu.UmbraMenu;
partial class Checks
{
    static readonly string[] PackSpecs={
        "Starbucks|AttackSpeedAndMoveSpeed:10|",
        "Crits|CritGlasses:10,AttackSpeedOnCrit:1,HealOnCrit:1|",
        "Free Red|RegeneratingScrap:5|",
        "Katyusha|Missile:1,AutoCastEquipment:5,EquipmentMagazine:5,MoreMissile:1|CommandMissile",
        "Zeus|AutoCastEquipment:5,EquipmentMagazine:5|Lightning",
        "Masochist|Thorns:10,ArmorReductionOnHit:5,ParentEgg:1|BurnNearby",
        "Drug Addict|AutoCastEquipment:5|Tonic",
        "Bungus|Mushroom:5,Bear:1,Syringe:1,BarrierOnOverHeal:1,Seed:1,BleedOnHit:1,ArmorReductionOnHit:1|",
        "1 Health Shrimp|MissileVoid:1,ShieldOnly:1,MoreMissile:1|",
        "Arsonist|IgniteOnKill:10,StrengthenBurn:1|",
        "DMG Stack|AutoCastEquipment:1,EquipmentMagazine:1,IncreaseDamageOnMultiKill:5,Talisman:1|DeathProjectile",
        "Primary Heresy|Clover:1,LunarBadLuck:1,LunarPrimaryReplacement:1|",
        "Seeker's Flying Object|ParentEgg:1,ArmorReductionOnHit:1,Bear:1,Thorns:1|",
        "Remote Katyusha|Firework:1,MoreMissile:1,Squid:1,Missile:1|VendingMachine",
        "Special Forces|AutoCastEquipment:5|GummyClone",
        "Shield Tank|ShieldOnly:1,DelayedDamage:5,OutOfCombatArmor:5|",
        "Quick Fix|ScrapWhite:1,AttackSpeedAndMoveSpeed:1|",
        "Seared Steak|FlatHealth:1,AttackSpeedAndMoveSpeed:1|",
        "Hearty Stew|TPHealingNova:1,Seed:1|",
        "Ultimate Meal|Clover:1,AlienHead:1|",
        "Sauteed Worms|FireballsOnHit:1,LightningStrikeOnHit:1|"
    };
    static readonly string[] ChefRecipes={"Quick Fix","Seared Steak","Hearty Stew","Ultimate Meal","Sauteed Worms"};
    static void PackChecks()
    {
        Root.InGame=true;Root.characterCollected=true;
        string[] common={"AttackSpeedAndMoveSpeed","CritGlasses","ArmorReductionOnHit","Mushroom","Bear","Syringe","IgniteOnKill","IncreaseDamageOnMultiKill","Firework","DelayedDamage","OutOfCombatArmor","ScrapWhite","FlatHealth","BleedOnHit"};
        string[] red={"MoreMissile","BarrierOnOverHeal","Talisman","Clover","AlienHead"},lunar={"AutoCastEquipment","ShieldOnly","LunarBadLuck","LunarPrimaryReplacement"},boss={"ParentEgg","FireballsOnHit","LightningStrikeOnHit"};
        int next=10000;
        foreach(string name in PackSpecs.SelectMany(spec=>spec.Split('|')[1].Split(',')).Select(value=>value.Split(':')[0]).Distinct())
        {
            var index=(ItemIndex)next++;Language.Names[name]="Name / "+name;
            var tier=common.Contains(name)?ItemTier.Tier1:red.Contains(name)?ItemTier.Tier3:lunar.Contains(name)?ItemTier.Lunar:boss.Contains(name)?ItemTier.Boss:name=="MissileVoid"?ItemTier.VoidTier2:ItemTier.Tier2;
            ItemCatalog.Defs[index]=new ItemDef{name=name,nameToken=name,tier=tier,Icon=new Sprite{texture=new Texture2D(64,64)}};
        }
        next=10000;foreach(string name in PackSpecs.Select(spec=>spec.Split('|')[2]).Where(value=>value!="").Distinct())
            EquipmentCatalog.Defs[(EquipmentIndex)next++]=new EquipmentDef{name=name,nameToken=name,Icon=new Sprite{texture=new Texture2D(64,64)}};
        Assert(ItemPacks.All.Count==21,"All twenty-one requested packs must exist");
        Assert(ItemPacks.All.Where(pack=>pack.IsChefRecipe).Select(pack=>pack.Name).OrderBy(name=>name).SequenceEqual(ChefRecipes.OrderBy(name=>name)),"Only the five requested CHEF recipes must belong to that section");
        foreach(string spec in PackSpecs)
        {
            string[] parts=spec.Split('|');var pack=ItemPacks.All.Single(value=>value.Name==parts[0]);
            var expected=parts[1].Split(',').ToDictionary(value=>ItemCatalog.FindItemIndex(value.Split(':')[0]),value=>int.Parse(value.Split(':')[1]));
            var inventory=new Inventory{activeEquipmentSlot=1,currentEquipmentIndex=(EquipmentIndex)99999};inventory.Counts[(ItemIndex)99999]=41;Root.LocalPlayerInv=inventory;
            Assert(ItemPacks.TryResolve(pack,out var contents,out _),"Every recipe must resolve using exact catalog IDs");
            Assert(contents.Count==expected.Count+(parts[2]==""?0:1),"A pack must contain no unintended pickups");
            Assert(ItemPacks.Give(pack,out _),"A resolved pack must apply");
            foreach(var entry in expected)Assert(inventory.GetItemCountPermanent(entry.Key)==entry.Value,"Pack quantities must match the user's recipe");
            Assert(inventory.Counts[(ItemIndex)99999]==41&&inventory.Counts.Count==expected.Count+1,"Giving a pack must preserve unrelated inventory and add no extra items");
            Assert(inventory.currentEquipmentIndex==(parts[2]==""?(EquipmentIndex)99999:EquipmentCatalog.FindEquipmentIndex(parts[2])),"Only the requested equipment may replace the active equipment");
            if(parts[2]!="")Assert(inventory.LastLocation.slot==1&&inventory.LastLocation.set==0,"Equipment must go in the player's active equipment slot");
            ItemPacks.Give(pack,out _);foreach(var entry in expected)Assert(inventory.GetItemCountPermanent(entry.Key)==entry.Value*2,"A second click must add a second complete pack");
        }
        var ordered=ItemPacks.Sorted().ToArray();
        Assert(ordered.Take(16).All(pack=>!pack.IsChefRecipe)&&ordered.Skip(16).All(pack=>pack.IsChefRecipe),"CHEF recipes must remain together after the general packs");
        foreach(var group in ordered.GroupBy(pack=>pack.IsChefRecipe))
        {
            var scores=group.Select(pack=>{ItemPacks.TryResolve(pack,out var contents,out _);return contents.Sum(value=>value.Value);}).ToArray();
            Assert(scores.SequenceEqual(scores.OrderBy(value=>value)),"Each section must sort by quantity times rarity, lowest first");
        }
        Assert(ordered[0].Name=="Starbucks"&&ordered[15].Name=="Masochist"&&ordered[16].Name=="Quick Fix", "Value sorting must reflect the new quantities and retain section boundaries");
        var missile=ItemCatalog.FindItemIndex("MoreMissile");var definition=ItemCatalog.Defs[missile];ItemCatalog.Defs.Remove(missile);Root.LocalPlayerInv=new Inventory();
        Assert(!ItemPacks.Give(ItemPacks.All.Single(pack=>pack.Name=="Katyusha"),out _)&&Root.LocalPlayerInv.Counts.Count==0&&Root.LocalPlayerInv.currentEquipmentIndex==EquipmentIndex.None,"Missing DLC entries must fail before granting anything or replacing equipment");ItemCatalog.Defs[missile]=definition;
        Root.InGame=false;Assert(!ItemPacks.Give(ItemPacks.All[0],out _)&&Root.LocalPlayerInv.Counts.Count==0,"Packs must not mutate cached inventory outside a run");Root.InGame=true;Root.LocalPlayerInv=null;Assert(!ItemPacks.Give(ItemPacks.All[0],out _),"Missing inventories must be safe");
        IconChecks();MenuOrderChecks();StabilityChecks();
        Console.WriteLine("PASS: "+assertions+" total catalog, pack, icon, menu, portal and ESP assertions.");
    }
    static void IconChecks()
    {
        var atlas=new Texture2D(128,64);var one=new Sprite{texture=atlas,UV=new Vector4(0,0,.5f,1)};var two=new Sprite{texture=atlas,UV=new Vector4(.5f,0,1,1)};
        var a=ItemCatalog.FindItemIndex("CritGlasses");var b=ItemCatalog.FindItemIndex("Bear");ItemCatalog.Defs[a].Icon=one;ItemCatalog.Defs[b].Icon=two;
        var menu=PlainMenu(12);ItemIndex activated=ItemIndex.None;var rows=CatalogGrouping.BuildItemButtons(menu,new[]{a,b},index=>activated=index);
        foreach(var row in rows.Where(row=>row.GetAction()!=null))
        {
            var pickup=(PickupButton)Row(row);Assert(pickup.TryGetIcon(out Texture texture,out Rect uv)&&texture==atlas,"Rows must retain the exact catalog texture");
            row.Activate();Assert(activated==(uv.x==0?a:b),"Sprite atlas UVs must match the item acted on by that row");
        }
        var refDef=new ItemDef{IconReference=new AssetReferenceT<Sprite>{RuntimeKey="ExactDlcSprite"}};int loads=Addressables.IconLoadCalls;
        Assert(CatalogIcons.Get(refDef)==null&&Addressables.IconLoadCalls==loads+1,"New DLC icons must load asynchronously");CatalogIcons.Get(refDef);Assert(Addressables.IconLoadCalls==loads+1,"Repeated draws must share a single owned load");
        Addressables.IconOperations["ExactDlcSprite"].Result=two;Addressables.IconOperations["ExactDlcSprite"].Status=AsyncOperationStatus.Succeeded;
        Assert(CatalogIcons.Get(refDef)==two,"An asynchronously loaded icon must be the exact asset from the definition");
        var delayed=new PickupButton(menu,1,"Delayed",()=>{},()=>CatalogIcons.Get(refDef));Assert(delayed.TryGetIcon(out var delayedTexture,out var delayedUv)&&delayedTexture==atlas&&delayedUv.x==.5f,"The displayed icon must use the loaded sprite's atlas coordinates");
        var equipmentDef=new EquipmentDef{IconReference=refDef.IconReference};Assert(CatalogIcons.Get(equipmentDef)==two&&Addressables.IconLoadCalls==loads+1,"Shared item/equipment icon references must not load twice");
        int releases=Addressables.ReleaseCount;CatalogIcons.Release();Assert(Addressables.ReleaseCount==releases+1,"Unloading must release the icon handles owned by the menu");
    }
    static void MenuOrderChecks()
    {
        Root.menus.Clear();for(int i=0;i<20;i++)Root.menus.Add(new Menu(new NormalMenu(i,new Rect(),"Test")));Root.mainMenu=Root.menus[0];Root.mainMenu.AddButtons(new List<Button>{new Button(new NormalButton(Root.mainMenu,3,"ITEMS",()=>{}))});Root.mainMenu.SetEnabled(true);
        Root.characterCollected=true;Root.navigationToggle=false;Root.LocalPlayerInv=new Inventory();Root.items=ItemCatalog.Defs.Keys.ToList();Root.equipment=EquipmentCatalog.Defs.Keys.ToList();
        var items=new Items();Root.menus[3]=items;Root.inventoryItemListMenu=new InventoryItemList();Root.menus[17]=Root.inventoryItemListMenu;Root.menus[12]=new ItemList();Root.menus[13]=new EquipmentList();Root.menus[18]=new ItemPackList();Root.menus[19]=new ItemPackDetails();
        string[] expected={"GIVE ALL ITEMS","GIVE ALL ITEMS (NO LUNAR/VOID)","ROLL ITEMS","INFINITE EQUIPMENT","","ITEM SPAWN MENU","EQUIPMENT SPAWN MENU","REMOVE ITEMS","DROP ITEMS","DROP FROM INVENTORY","STACK INVENTORY","CHANGE CHEST ITEM","CLEAR INVENTORY","","ITEM PACKS"};
        Assert(items.GetButtons().Select(button=>Row(button).Text.Split(new[]{" : "},StringSplitOptions.None)[0]).SequenceEqual(expected),"The actual Items constructor must use the requested order and unlabelled dividers");
        Assert(items.GetButtons().Select(button=>button.GetId()).SequenceEqual(Enumerable.Range(1,15)),"Row IDs must remain contiguous after reordering");
        Assert(Root.menus[12].GetActivatingButton()==items.toggleItemListMenu&&Root.menus[13].GetActivatingButton()==items.toggleEquipmentListMenu&&Root.menus[17].GetActivatingButton()==items.toggleRemoveItemMenu,"Spawn, equipment and removal pages must link to their new rows");
        Navigation.menuIndex=3;Navigation.buttonIndex=4;Navigation.MoveSelection(1);Assert(Navigation.buttonIndex==6,"Keyboard navigation must skip the divider");
        items.toggleDropItems.Activate();items.toggleDropInvItems.Activate();Assert(!Items.isDropItemForAll&&Items.isDropItemFromInventory&&!items.toggleDropItems.IsEnabled()&&items.toggleDropInvItems.IsEnabled(),"Drop modes must stay mutually exclusive");
        var index=ItemCatalog.FindItemIndex("CritGlasses");Root.LocalPlayerInv.Counts[index]=2;int drops=Items.Dropped.Count;ItemList.GiveItem(index);Assert(Root.LocalPlayerInv.GetItemCountPermanent(index)==1&&Items.Dropped.Count==drops+1,"Drop From Inventory must remove a stack and create a pickup");
        InventoryItemList.RemoveItem(index);Assert(Root.LocalPlayerInv.GetItemCountPermanent(index)==0&&Items.Dropped.Count==drops+1,"Remove Items must delete a stack without creating a pickup");
        MenuUI.NavigateTo(18);var packList=Root.menus[18];
        Assert(packList.GetButtons().Count==23&&packList.GetButtons().Count(button=>button.GetAction()!=null)==21,"The list must have twenty-one packs and exactly two section headings");
        Assert(packList.GetButtons().Select(button=>button.GetId()).SequenceEqual(Enumerable.Range(1,23)),"Pack and section row IDs must stay contiguous");
        var headings=packList.GetButtons().Where(button=>Row(button) is DividerButton).ToArray();
        Assert(headings.Select(button=>Row(button).Text).SequenceEqual(new[]{"General packs","CHEF recipes"})&&headings.All(button=>button.GetAction()==null),"Section headings must be labelled and non-actionable");
        Navigation.menuIndex=18;Navigation.buttonIndex=headings[1].GetId()-1;Navigation.MoveSelection(1);
        Assert(Navigation.buttonIndex==headings[1].GetId()+1,"Keyboard navigation must skip the CHEF heading");
        var selected=packList.GetButtons().Single(button=>Row(button).Text.StartsWith("Katyusha /"));selected.Activate();Assert(MenuUI.ActiveMenuId==19&&Root.menus[19].GetTitle()=="Katyusha","Selecting a pack must open the matching preview");
        Assert(Root.menus[19].GetButtons().Count(button=>Row(button) is PickupButton)==5,"Pack previews must show one exact icon row per ingredient");
        MenuUI.Back();Assert(MenuUI.ActiveMenuId==18&&Navigation.buttonIndex==selected.GetId(),"Back must return to the selected pack");
        ((SearchableListMenu)Layout(packList)).SetSearchText("Mocha");
        Assert(packList.GetButtons().Where(button=>button.GetAction()!=null).Select(button=>Row(button).Text.Split(new[]{" / "},StringSplitOptions.None)[0]).SequenceEqual(new[]{"Starbucks","Quick Fix","Seared Steak"}),"Ingredient search must match packs across both sections");
        Assert(packList.GetButtons().Count(button=>Row(button) is DividerButton)==2,"Filtered results in both sections must keep both headings");
        ((SearchableListMenu)Layout(packList)).SetSearchText("cHeF");
        Assert(packList.GetButtons().Count==6&&Row(packList.GetButtons()[0]).Text=="CHEF recipes","Searching CHEF must show only that section and its five recipes");
        Assert(packList.GetButtons().Skip(1).Select(button=>Row(button).Text.Split(new[]{" / "},StringSplitOptions.None)[0]).OrderBy(name=>name).SequenceEqual(ChefRecipes.OrderBy(name=>name)),"The CHEF search must not include any general packs");
        packList.GetButtons().Single(button=>Row(button).Text.StartsWith("Quick Fix / ")).Activate();
        Assert(Root.menus[19].GetTitle()=="Quick Fix"&&Root.menus[19].GetButtons().Count(button=>Row(button) is PickupButton)==2,"A CHEF preview must show both ingredient icons");
        Assert(Root.menus[19].GetButtons().Any(button=>Row(button).Text=="Adds CHEF recipe ingredients to your inventory."),"CHEF previews must explain that the pack grants the ingredients");MenuUI.Back();
        ((SearchableListMenu)Layout(packList)).SetSearchText("Remote Caffeinator");
        Assert(packList.GetButtons().Count==2&&Row(packList.GetButtons()[0]).Text=="General packs"&&Row(packList.GetButtons()[1]).Text.StartsWith("Remote Katyusha / "),"Equipment search must retain only the relevant section heading");
        ((SearchableListMenu)Layout(packList)).SetSearchText("Not a pack");Assert(packList.GetButtons().Single().GetAction()==null,"No search results must be non-actionable");
        foreach(var pack in ItemPacks.All)foreach(var pickup in pack.Pickups)Language.Names[pickup.CatalogName]=pickup.DisplayName;
        ((SearchableListMenu)Layout(packList)).SetSearchText("");Screen.width=1000;Screen.height=800;Root.WindowWidth=680;Root.WindowHeight=560;Root.navigationToggle=false;Event.current=new Event{type=EventType.Repaint};MenuUI.ResetWindow();
        MenuUI.NavigateTo(3);GUI.StartCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/items-order.png");
        MenuUI.NavigateTo(18);GUI.StartCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/pack-list.png");
        ((SearchableListMenu)Layout(packList)).SetSearchText("CHEF");GUI.StartCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/chef-packs.png");
        packList.GetButtons().Single(button=>Row(button).Text.StartsWith("Quick Fix /")).Activate();GUI.StartCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/chef-details.png");MenuUI.Back();
        ((SearchableListMenu)Layout(packList)).SetSearchText("");
        packList.GetButtons().Single(button=>Row(button).Text.StartsWith("Katyusha /")).Activate();GUI.StartCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/pack-details.png");
        Root.WindowWidth=560;Root.WindowHeight=360;MenuUI.ResetWindow();GUI.StartCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/pack-minimum.png");
    }
}
