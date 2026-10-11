using System;
using System.Collections.Generic;
using System.Linq;
using UmbraMenu;
using UmbraMenu.Menus;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using Root = UmbraMenu.UmbraMenu;

partial class Checks
{
    static void CheatTick(float elapsed = .6f) { Time.unscaledTime += elapsed; WorldCheats.Tick(); }
    static ChestBehavior CheatChest(ItemIndex item, int count = 1)
    {
        var pickup = PickupCatalog.FindPickupIndex(item);
        var chest = new ChestBehavior(pickup) { dropTable = new PickupDropTable { Pickup = pickup } };
        chest.SetField("dropCount", count);
        return chest;
    }
    static SetStateOnHurt HurtFor(CharacterBody body, bool stun = false)
    {
        var hurt = new SetStateOnHurt { canBeStunned = stun };
        hurt.gameObject.Components.Remove(hurt);
        hurt.gameObject = body.gameObject; body.gameObject.Components.Add(hurt);
        return hurt;
    }
    static void WorldCheatsChecks()
    {
        WorldCheats.Shutdown();
        NetworkServer.active = true; Run.instance = new Run(); Root.characterCollected = true;
        Root.LocalPlayer = new CharacterMaster(); Root.LocalPlayer.SetField("<luck>k__BackingField", 2f);
        WorldCheats.Initialize(); WorldCheats.Initialize();
        Assert(SceneDirector.PreSubscribers == 1 && SceneDirector.SpawnSubscribers == 1 && SceneDirector.Subscribers == 1 && Run.DestroySubscribers == 1,
            "World Cheats must subscribe once even when settings or initialization repeat.");

        WorldCheats.StageAbundance = true; WorldCheats.StageMultiplier = 3; WorldCheats.SettingsChanged();
        var stage = new SceneDirector(100); stage.Before();
        Assert(stage.interactableCredit == 300 && stage.monsterCredit == 100, "Stage Abundance must multiply only the interactable budget before population.");
        stage.Before(); Assert(stage.interactableCredit == 300, "Repeated callbacks for one stage must not multiply credits again.");
        var capped = new SceneDirector(2000); capped.Before();
        Assert(capped.interactableCredit == WorldCheats.MaxStageCredits, "Increased stage budgets must be capped.");
        var preexisting = new SceneDirector(int.MaxValue); preexisting.Before();
        Assert(preexisting.interactableCredit == int.MaxValue, "Credit arithmetic must not overflow or reduce a pre-existing higher budget.");
        var empty = new SceneDirector(0); empty.Before(); Assert(empty.interactableCredit == 0, "Zero-budget stages must keep their special-stage behavior.");
        WorldCheats.StageAbundance = false; var normal = new SceneDirector(100); normal.Before();
        Assert(normal.interactableCredit == 100, "Stage generation must stay unchanged while abundance is off.");

        var white = Item(18000, ItemTier.Tier1, "Cheat White"); var red = Item(18001, ItemTier.Tier3, "Cheat Red");
        var red2 = Item(18002, ItemTier.Tier3, "Cheat Red Two"); var lockedRed = Item(18003, ItemTier.Tier3, "Unavailable Red", 4);
        var hidden = Item(18004, ItemTier.Tier3, "Hidden Red", hidden: true);
        var run = Run.instance;
        run.Unavailable.Add(lockedRed);
        run.allAvailableTierDroplists[ItemTier.Tier1] = new List<PickupIndex> { PickupCatalog.FindPickupIndex(white) };
        run.allAvailableTierDroplists[ItemTier.Tier3] = new List<PickupIndex> {
            PickupCatalog.FindPickupIndex(red), PickupCatalog.FindPickupIndex(red2), PickupCatalog.FindPickupIndex(lockedRed),
            PickupCatalog.FindPickupIndex(hidden), PickupCatalog.FindPickupIndex(red) };

        WorldCheats.ChestParadise = true; WorldCheats.ChestMultiplier = 5; WorldCheats.SettingsChanged();
        var paradise = new SceneDirector(); paradise.Before();
        var card = new SpawnCard(); var directorCard = new DirectorCard { spawnCard = card };
        for (int i=0;i<30;i++) paradise.Spawn(CheatChest(white).gameObject,directorCard);
        paradise.Spawn(new GameObject(),directorCard);
        paradise.Spawn(CheatChest(white).gameObject,new DirectorCard {spawnCard=new SpawnCard {sendOverNetwork=false}});
        paradise.After(); paradise.After();
        Assert(WorldCheats.PendingChests == 50, "Chest Paradise must queue bounded extras from actual stage chests, excluding other objects and non-network cards.");
        var spawnedChests = new List<UnityEngine.Object>();
        DirectorCore.instance = new DirectorCore { Factory = request =>
        {
            Assert(request.spawnCard == card && request.placementRule.placementMode == DirectorPlacementRule.PlacementMode.Random,
                "Extra chests must use an available stage card and the director's valid random placement.");
            var chest = CheatChest(white);
            var price = new PurchaseInteraction {costType=CostTypeIndex.Money,cost=25};
            price.gameObject=chest.gameObject; chest.gameObject.Components.Add(price);
            spawnedChests.Add(chest); spawnedChests.Add(price); UnityEngine.Object.SceneObjects=spawnedChests.ToArray();
            return chest.gameObject;
        }};
        CheatTick();
        Assert(WorldCheats.ExtraChestsSpawned == 2 && WorldCheats.PendingChests == 48 && DirectorCore.instance.Attempts == 2,
            "Chest Paradise must spread placement across ticks, with only two attempts per tick.");
        CheatTick(.01f); Assert(DirectorCore.instance.Attempts == 2, "Repeated frames must not bypass the spawn throttle.");
        Assert(spawnedChests.OfType<PurchaseInteraction>().All(p=>p.cost==50), "Extra chest prices must use normal difficulty scaling.");
        WorldCheats.ChestParadise=false; WorldCheats.SettingsChanged();
        Assert(WorldCheats.PendingChests==0 && spawnedChests.Count==4, "Disabling Chest Paradise must cancel pending placement without deleting existing loot.");
        WorldCheats.ChestParadise=true; WorldCheats.SettingsChanged();
        var failedStage=new SceneDirector(); failedStage.Before(); failedStage.Spawn(CheatChest(white).gameObject,directorCard); failedStage.After();
        DirectorCore.instance.Factory=request=>null; CheatTick();
        Assert(WorldCheats.ExtraChestsSpawned==0 && WorldCheats.PendingChests==2,
            "Failed placement must count as an attempt without endless retries or claiming a spawn.");
        WorldCheats.ResetStage(); Assert(WorldCheats.PendingChests==0 && WorldCheats.ChestParadise,
            "Scene cleanup must discard queued old-stage cards while preserving the selected cheat.");
        WorldCheats.ChestParadise=false;

        var money=new PurchaseInteraction {costType=CostTypeIndex.Money,cost=23};
        var health=new PurchaseInteraction {costType=CostTypeIndex.PercentHealth,cost=50};
        var coin=new PurchaseInteraction {costType=CostTypeIndex.LunarCoin,cost=2};
        UnityEngine.Object.SceneObjects=new UnityEngine.Object[] {money,health,coin};
        WorldCheats.FreeShopping=true; WorldCheats.SettingsChanged(); CheatTick();
        Assert(money.cost==0 && health.cost==50 && coin.cost==2, "Free Shopping must waive only money costs.");
        money.Networkcost=55; CheatTick();
        Assert(money.cost==0, "Shrine/discount price changes must still be free while enabled.");
        WorldCheats.FreeShopping=false; WorldCheats.SettingsChanged();
        Assert(money.cost==55 && health.cost==50 && coin.cost==2, "Disabling shopping must restore the latest real money price.");
        WorldCheats.FreeShopping=true; WorldCheats.SettingsChanged(); CheatTick();
        money.Networkcost=77; WorldCheats.FreeShopping=false; WorldCheats.SettingsChanged();
        Assert(money.cost==77, "Restoration must preserve a price replaced by another system.");

        var chestOne=CheatChest(white,3); var chestTwo=CheatChest(white,6);
        var unopened=CheatChest(white); unopened.SetField("<currentPickup>k__BackingField",new UniquePickup(PickupIndex.none));
        var opened=CheatChest(white); opened.isChestOpened=true;
        var equipmentIndex=(EquipmentIndex)18000; EquipmentCatalog.Defs[equipmentIndex]=new EquipmentDef {name="CheatEquip",nameToken="CHEAT_EQUIP"};
        var equipmentPickup=PickupCatalog.FindPickupIndex(equipmentIndex);
        var equipmentChest=new ChestBehavior(equipmentPickup) {dropTable=new PickupDropTable {Pickup=equipmentPickup}};
        UnityEngine.Object.SceneObjects=new UnityEngine.Object[] {chestOne,chestTwo,unopened,opened,equipmentChest};
        WorldCheats.LootMultiplier=true; WorldCheats.DropMultiplier=5; WorldCheats.SettingsChanged(); CheatTick();
        Assert(chestOne.GetField<int>("dropCount")==15 && chestTwo.GetField<int>("dropCount")==20,
            "Loot multiplier must multiply native counts and cap added loot at twenty drops.");
        CheatTick(); Assert(chestOne.GetField<int>("dropCount")==15, "Repeated updates must not compound the multiplier.");
        Assert(unopened.GetField<int>("dropCount")==1 && opened.GetField<int>("dropCount")==1,
            "Uninitialized and already-opened chests must not be modified.");
        WorldCheats.DropMultiplier=2; WorldCheats.SettingsChanged(); CheatTick();
        Assert(chestOne.GetField<int>("dropCount")==6 && chestTwo.GetField<int>("dropCount")==12,
            "Changing the multiplier must use the original count rather than multiplying the applied count.");
        WorldCheats.LootMultiplier=false; WorldCheats.SettingsChanged();
        Assert(chestOne.GetField<int>("dropCount")==3 && chestTwo.GetField<int>("dropCount")==6, "Disabling loot multiplication must restore native counts.");

        var originalTable=chestOne.dropTable; var originalPickup=chestOne.currentPickup;
        WorldCheats.Rarity=LootRarity.Legendary; WorldCheats.SettingsChanged(); CheatTick();
        var owned=chestOne.dropTable as ExplicitPickupDropTable;
        Assert(owned!=null && owned!=originalTable && owned.pickupEntries.Length==2 &&
            owned.pickupEntries.All(e=>e.pickupDef==ItemCatalog.GetItemDef(red)||e.pickupDef==ItemCatalog.GetItemDef(red2)),
            "Rarity Control must build an owned table from available visible items of the selected rarity, with no duplicates or locked DLC.");
        Assert(!owned.canDropBeReplaced && chestOne.currentPickup.pickupIndex==PickupCatalog.FindPickupIndex(red),
            "Rarity Control must reroll the unopened chest and prevent random replacement from changing the forced rarity.");
        Assert(equipmentChest.currentPickup.pickupIndex==equipmentPickup && opened.RollCalls==0,
            "Rarity changes must preserve equipment and opened chests.");
        int rolls=chestOne.RollCalls; CheatTick();
        Assert(chestOne.RollCalls==rolls, "Rarity Control must not reroll every frame.");
        var rng=new Xoroshiro128Plus(); var drops=Enumerable.Range(0,5).Select(i=>owned.GeneratePickup(rng).pickupIndex).ToArray();
        Assert(drops.All(p=>p==PickupCatalog.FindPickupIndex(red)||p==PickupCatalog.FindPickupIndex(red2)) &&
            drops.Distinct().Count()==2, "Subsequent multiplied drops must keep the chosen rarity and retain item variety.");
        WorldCheats.Rarity=LootRarity.Natural; WorldCheats.SettingsChanged();
        Assert(chestOne.dropTable==originalTable && chestOne.currentPickup.pickupIndex==originalPickup.pickupIndex && owned.Destroyed && !originalTable.Destroyed,
            "Disabling rarity must restore original unopened loot and release only the menu-owned table.");
        WorldCheats.Rarity=LootRarity.Legendary; WorldCheats.SettingsChanged(); CheatTick();
        chestOne.SetField("<currentPickup>k__BackingField",new UniquePickup(PickupCatalog.FindPickupIndex(red2)));
        WorldCheats.Rarity=LootRarity.Natural; WorldCheats.SettingsChanged();
        Assert(chestOne.currentPickup.pickupIndex==PickupCatalog.FindPickupIndex(red2),
            "Disabling rarity must preserve a chest pickup subsequently changed by another menu/system.");
        WorldCheats.Rarity=LootRarity.Hallowed; WorldCheats.SettingsChanged(); CheatTick();
        Assert(chestOne.dropTable==originalTable && WorldCheats.Status.Contains("No available items"),
            "Unavailable rarity pools must retain original loot and explain the missing content.");
        int creates=ScriptableObject.Created; CheatTick(); CheatTick();
        Assert(ScriptableObject.Created==creates, "An unavailable DLC rarity must not allocate tables every tick.");
        var hallowed=Item(18005,ItemTier.Cursed,"Available Hallowed",4);
        run.allAvailableTierDroplists[ItemTier.Cursed]=new List<PickupIndex> {PickupCatalog.FindPickupIndex(hallowed)};
        run.PublishPools(); CheatTick();
        Assert(chestOne.currentPickup.pickupIndex==PickupCatalog.FindPickupIndex(hallowed),
            "An availability update must refresh a previously missing rarity pool.");
        WorldCheats.Rarity=LootRarity.Natural; WorldCheats.SettingsChanged();

        WorldCheats.ProcChaos=true; WorldCheats.LuckBonus=10; WorldCheats.SettingsChanged(); CheatTick();
        Assert(Root.LocalPlayer.luck==12, "Proc Chaos must add its bounded bonus to the local player's existing luck.");
        CheatTick(); Assert(Root.LocalPlayer.luck==12, "Luck updates must not compound.");
        Root.LocalPlayer.SetField("<luck>k__BackingField",-1f); CheatTick();
        Assert(Root.LocalPlayer.luck==9, "Changes to natural item luck must be observed while the boost is enabled.");
        WorldCheats.LuckBonus=10000; WorldCheats.SettingsChanged(); CheatTick();
        Assert(Root.LocalPlayer.luck==19, "Luck rerolls must be capped at twenty bonus luck.");
        WorldCheats.ProcChaos=false; WorldCheats.SettingsChanged();
        Assert(Root.LocalPlayer.luck==-1, "Disabling Proc Chaos must restore the latest natural luck.");
        var oldMaster=Root.LocalPlayer; WorldCheats.ProcChaos=true; WorldCheats.LuckBonus=5; WorldCheats.SettingsChanged(); CheatTick();
        Root.LocalPlayer=new CharacterMaster(); Root.LocalPlayer.SetField("<luck>k__BackingField",7f); CheatTick();
        Assert(oldMaster.luck==-1 && Root.LocalPlayer.luck==12, "Character/master replacement must release the old luck owner and boost the new one.");
        WorldCheats.ProcChaos=false; WorldCheats.SettingsChanged(); Assert(Root.LocalPlayer.luck==7,"Replacement master luck must restore correctly.");

        CharacterBody.readOnlyInstancesList.Clear();
        var enemy=Body(TeamIndex.Monster); var boss=Body(TeamIndex.Lunar); var voidEnemy=Body(TeamIndex.Void);
        var ally=Body(TeamIndex.Player); var neutral=Body(TeamIndex.Neutral); var dead=Body(TeamIndex.Monster,false);
        var enemyHurt=HurtFor(enemy,true); var bossHurt=HurtFor(boss); var voidHurt=HurtFor(voidEnemy);
        var allyHurt=HurtFor(ally); var neutralHurt=HurtFor(neutral); var deadHurt=HurtFor(dead);
        WorldCheats.FreezeEnemies=true; WorldCheats.SettingsChanged(); CheatTick();
        Assert(enemyHurt.Stuns==1 && bossHurt.Stuns==1 && voidHurt.Stuns==1 &&
            allyHurt.Stuns==0 && neutralHurt.Stuns==0 && deadHurt.Stuns==0,
            "Freeze must target living hostile teams, including immune bosses, without stopping allies, neutrals, or corpses.");
        Assert(enemyHurt.canBeStunned && !bossHurt.canBeStunned && !voidHurt.canBeStunned,
            "Freeze must restore immunity flags immediately after each networked stun.");
        float releases=enemyHurt.StunnedUntil; WorldCheats.FreezeEnemies=false; WorldCheats.SettingsChanged(); CheatTick(1.1f);
        Assert(enemyHurt.Stuns==1 && Time.unscaledTime>=releases, "Disabling Freeze must stop renewing the effect and allow normal state recovery within one second.");

        WorldCheats.FreeShopping=true; WorldCheats.ProcChaos=true; WorldCheats.LootMultiplier=true;
        UnityEngine.Object.SceneObjects=new UnityEngine.Object[] {money,chestOne}; WorldCheats.SettingsChanged(); CheatTick();
        Assert(money.cost==0 && Root.LocalPlayer.luck>7 && chestOne.GetField<int>("dropCount")>3,
            "Combined cheats must apply independently.");
        NetworkServer.active=false; CheatTick();
        Assert(money.cost==77 && Root.LocalPlayer.luck==7 && chestOne.GetField<int>("dropCount")==3 && WorldCheats.PendingChests==0 && !WorldCheats.FreeShopping && !WorldCheats.ProcChaos && !WorldCheats.LootMultiplier,
            "Losing server authority must release instance changes and cancel spawn work.");
        NetworkServer.active=true; WorldCheats.Reset();

        var manyPrices = Enumerable.Range(0, WorldCheats.MaxPurchases + 3)
            .Select(i => new PurchaseInteraction { costType = CostTypeIndex.Money, cost = 25 }).ToArray();
        var manyChests = Enumerable.Range(0, WorldCheats.MaxTrackedChests + 3).Select(i => CheatChest(white)).ToArray();
        UnityEngine.Object.SceneObjects = manyPrices.Cast<UnityEngine.Object>().Concat(manyChests).ToArray();
        WorldCheats.FreeShopping = WorldCheats.LootMultiplier = true; WorldCheats.SettingsChanged(); CheatTick();
        Assert(manyPrices.Count(p => p.cost == 0) == WorldCheats.MaxPurchases && manyChests.Count(c => c.NativeDropCount == 2) == WorldCheats.MaxTrackedChests,
            "Shopping and chest snapshots must stay within their bounds even in unusually crowded scenes.");
        WorldCheats.Reset();
        Assert(manyPrices.All(p => p.cost == 25) && manyChests.All(c => c.NativeDropCount == 1),
            "Bounded snapshot cleanup must still restore all affected targets.");

        while(Root.menus.Count<=WorldCheatsMenu.MenuId) Root.menus.Add(PlainMenu(Root.menus.Count));
        var parent=Root.menus[4];
        parent.AddButtons(new List<Button> {new Button(new TogglableButton(parent,8,"WORLD CHEATS : OFF","WORLD CHEATS : ON",
            ()=>Root.menus[20].ToggleMenu(),()=>Root.menus[20].ToggleMenu()))});
        var menu=new WorldCheatsMenu(); Root.menus[20]=menu;
        Assert(menu.GetPrevMenuId()==4 && menu.GetActivatingButton()==parent.GetButtons()[0],
            "World Cheats must be an ordinary child page under Spawn.");
        Assert(menu.GetButtons().Select(b=>b.GetId()).SequenceEqual(Enumerable.Range(1,menu.GetButtons().Count)) &&
            menu.GetButtons().Count(b=>b.GetAction()!=null)==8, "The submenu must contain seven cheat controls plus reset, with contiguous row IDs.");
        var freeButton=menu.GetButtons().Single(b=>Row(b).Text.StartsWith("FREE SHOPPING", StringComparison.OrdinalIgnoreCase));
        NetworkServer.active=false; freeButton.Activate();
        Assert(!WorldCheats.FreeShopping && !freeButton.IsEnabled(), "A non-host must not enable cheats or leave a misleading enabled button.");
        NetworkServer.active=true; freeButton.Activate(); Assert(WorldCheats.FreeShopping && freeButton.IsEnabled(),"The menu must toggle the actual backend feature.");
        var stageButton=menu.GetButtons().Single(b=>Row(b).Text.StartsWith("STAGE ABUNDANCE", StringComparison.OrdinalIgnoreCase));
        for(int i=0;i<10;i++) stageButton.GetIncreaseAction()(); Assert(WorldCheats.StageMultiplier==5,"Stage multiplier controls must stop at x5.");
        for(int i=0;i<10;i++) stageButton.GetDecreaseAction()(); Assert(WorldCheats.StageMultiplier==2,"Stage multiplier controls must stop at x2.");
        var rarityButton=menu.GetButtons().Single(b=>Row(b).Text.StartsWith("RARITY CONTROL", StringComparison.OrdinalIgnoreCase));
        rarityButton.GetDecreaseAction()(); Assert(WorldCheats.Rarity==LootRarity.Pages,"Rarity selection must wrap in both directions.");
        rarityButton.GetIncreaseAction()(); Assert(WorldCheats.Rarity==LootRarity.Natural,"Natural rarity must disable forcing.");
        Root.mainMenu.SetEnabled(true); MenuUI.NavigateTo(20); Root.navigationToggle=true; Navigation.menuIndex=20; Navigation.buttonIndex=1; Navigation.UpdateIndexValues();
        Assert(Navigation.buttonIndex==2, "Keyboard navigation must skip headings and help rows.");
        Screen.width=1000; Screen.height=800; Root.WindowWidth=680; Root.WindowHeight=560; MenuUI.ResetWindow();
        Event.current=new Event {type=EventType.Repaint}; Root.navigationToggle=false; GUI.StartCanvas(); MenuUI.Draw();
        GUI.Save("artifacts/tests/previews/world-cheats.png");
        menu.SetScrollPosition(new Vector2(0,1000)); GUI.StartCanvas(); MenuUI.Draw(); GUI.Save("artifacts/tests/previews/world-cheats-combat.png");
        MenuUI.Back(); Assert(MenuUI.ActiveMenuId==4 && WorldCheats.FreeShopping,"Leaving the submenu must preserve the chosen cheats.");
        menu.GetButtons().Single(b=>Row(b).Text=="TURN OFF ALL WORLD CHEATS").Activate();
        Assert(!WorldCheats.FreeShopping && !WorldCheats.StageAbundance && WorldCheats.Rarity==LootRarity.Natural,
            "The reset action must release all cheat settings.");

        WorldCheats.FreeShopping=true; WorldCheats.ProcChaos=true; WorldCheats.FreezeEnemies=true;
        UnityEngine.Object.SceneObjects=new UnityEngine.Object[] {money}; WorldCheats.SettingsChanged(); enemyHurt.Throw=true; CheatTick();
        Assert(!WorldCheats.FreeShopping && !WorldCheats.ProcChaos && !WorldCheats.FreezeEnemies && money.cost==77 &&
            Root.LocalPlayer.luck==7 && enemyHurt.canBeStunned && WorldCheats.Status.Contains("error"),
            "A failing enemy operation must fail closed, restore other features and preserve the enemy's immunity flag.");
        CheatTick(); Assert(WorldCheats.Status.Contains("error"), "A paused cheat failure must retain its status without retrying on subsequent ticks.");
        enemyHurt.Throw=false;
        WorldCheats.FreeShopping=true; WorldCheats.SettingsChanged(); CheatTick(); run.End();
        Assert(!WorldCheats.FreeShopping && money.cost==77, "Ending a run must release effects and clear enabled cheats.");
        WorldCheats.Shutdown(); WorldCheats.Shutdown();
        Assert(SceneDirector.PreSubscribers==0 && SceneDirector.SpawnSubscribers==0 && SceneDirector.Subscribers==0 && Run.DestroySubscribers==0,
            "Unload must remove every owned event subscription exactly once.");
        CharacterBody.readOnlyInstancesList.Clear(); UnityEngine.Object.SceneObjects=new UnityEngine.Object[0]; Root.LocalPlayer=null;
        Console.WriteLine("PASS: "+assertions+" total gameplay assertions including World Cheats budgets, placement, shopping, loot, rarity, proc luck, freezing, navigation and cleanup.");
    }
}
