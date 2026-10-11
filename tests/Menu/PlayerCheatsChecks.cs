using System;
using System.Linq;
using RoR2;
using UmbraMenu;
using UmbraMenu.Menus;
using UnityEngine;
using UnityEngine.Networking;
using Root = UmbraMenu.UmbraMenu;
partial class Checks
{
    static CharacterBody CheatBody(float shield=0)
    {
        var result=new CharacterBody { NativeShield=shield, teamComponent=new TeamComponent{teamIndex=TeamIndex.Monster}, bodyFlags=CharacterBody.BodyFlags.Other };
        result.healthComponent=new HealthComponent { owner=result };
        result.RecalculateStats(); return result;
    }
    static void CharacterCheatsChecks()
    {
        WorldCheats.Shutdown(); PlayerCheats.Shutdown();
        Run.instance=new Run(); NetworkServer.active=true; Root.InGame=Root.characterCollected=true;
        var master=new CharacterMaster { money=100 }; var body=CheatBody();
        Root.LocalPlayer=master; Root.LocalPlayerBody=body;
        PlayerCheats.Initialize(); PlayerCheats.Initialize();
        Assert(GlobalEventManager.Subscribers==1 && Run.DestroySubscribers==1,"Character cheat initialization must subscribe once.");
        Assert(!PlayerCheats.UnlimitedShields && !PlayerCheats.OneHitKills && !PlayerCheats.GoldMultiplier,"New cheats must start off.");
        PlayerCheats.UnlimitedShields=PlayerCheats.NoFallDamage=PlayerCheats.UnlimitedJumps=PlayerCheats.SpeedMultiplier=
            PlayerCheats.FastAttackSpeed=PlayerCheats.MaxCritical=true;
        body.characterMotor.jumpCount=8; PlayerCheats.SettingsChanged();
        Assert(body.moveSpeed==14 && body.attackSpeed==5 && body.crit==100,"Speed, attacks and critical must apply after natural stats.");
        Assert(body.maxShield==100 && body.healthComponent.shield==100,"Unlimited Shields must work on characters without native shields.");
        Assert(body.maxJumpCount==2 && body.characterMotor.jumpCount==0,"Unlimited Jumps must renew air jumps without an enormous jump limit.");
        Assert((body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage)!=0 && (body.bodyFlags & CharacterBody.BodyFlags.Other)!=0,"Fall protection must preserve unrelated flags.");
        int calculations=body.Recalculations;
        for(int i=0;i<100;i++) PlayerCheats.Tick();
        Assert(body.moveSpeed==14 && body.attackSpeed==5 && body.Recalculations==calculations,"Ticking must neither compound multipliers nor recalculate stats every frame.");
        body.NativeSpeed=12;body.NativeAttack=3;body.NativeCrit=17;body.NativeShield=50;body.NativeJumps=4;body.RecalculateStats();
        Assert(body.moveSpeed==24 && body.attackSpeed==15 && body.crit==100 && body.maxShield==50 && body.maxJumpCount==4,"Native item or stat changes must become the new baseline.");
        PlayerCheats.Tick(); Assert(body.healthComponent.shield==50,"Shield capacity reductions must clamp the renewed shield to the native capacity.");
        body.healthComponent.shield=0;PlayerCheats.Tick();
        Assert(body.healthComponent.shield==50,"Damaged shields must refill to the current shield capacity.");
        PlayerCheats.SpeedFactor=3;PlayerCheats.SettingsChanged();
        Assert(body.moveSpeed==36,"Changing the speed factor must use the natural value.");
        body.SetField("<moveSpeed>k__BackingField",999f);PlayerCheats.Reset();
        Assert(body.moveSpeed==999 && body.attackSpeed==3 && body.crit==17 && body.maxJumpCount==4,"Cleanup must restore owned derived values and preserve foreign writes.");
        Assert((body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage)==0 && body.StatSubscribers==0,"Cleanup must restore fall flags and remove stat subscriptions.");
        body.RecalculateStats();
        PlayerCheats.SpeedMultiplier=PlayerCheats.FastAttackSpeed=true;
        PlayerCheats.SpeedFactor=float.NaN;PlayerCheats.AttackFactor=float.PositiveInfinity;PlayerCheats.SettingsChanged();
        Assert(PlayerCheats.SpeedFactor==2 && PlayerCheats.AttackFactor==5 && body.moveSpeed==24 && body.attackSpeed==15,"Invalid factors must recover to finite defaults.");
        PlayerCheats.SpeedFactor=10;PlayerCheats.AttackFactor=20;body.NativeSpeed=80;body.NativeAttack=80;PlayerCheats.SettingsChanged();
        Assert(body.moveSpeed==100 && body.attackSpeed==100,"Cheat increases must respect speed and attack caps.");
        body.NativeSpeed=150;body.NativeAttack=200;body.RecalculateStats();
        Assert(body.moveSpeed==150 && body.attackSpeed==200,"Existing native values above a cap must not be reduced.");
        PlayerCheats.Reset(); body.NativeSpeed=7;body.NativeAttack=1;body.RecalculateStats();
        Movement.flightToggle=true;PlayerCheats.Tick();PlayerCheats.NoFallDamage=true;PlayerCheats.SettingsChanged();
        PlayerCheats.NoFallDamage=false;PlayerCheats.SettingsChanged();
        Assert((body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage)!=0,"Flight must retain fall protection when No Fall Damage is disabled.");
        Movement.flightToggle=false;PlayerCheats.Tick();
        Assert((body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage)==0,"Ending the last fall protection source must restore the flag.");
        body.bodyFlags|=CharacterBody.BodyFlags.IgnoreFallDamage;PlayerCheats.NoFallDamage=true;PlayerCheats.SettingsChanged();PlayerCheats.Reset();
        Assert((body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage)!=0,"A native fall immunity flag must survive cleanup.");

        PlayerCheats.GoldMultiplier=true;PlayerCheats.GoldFactor=4;PlayerCheats.SettingsChanged();
        master.GiveMoney(10,false);
        Assert(master.money==140 && master.Grants==2,"Gold rewards must get one bonus without recursive grants.");
        master.GiveMoney(10,false);Assert(master.money==180,"Later rewards must multiply only the collected amount.");
        PlayerCheats.GiveManualMoney(50);Assert(master.money==230,"Manual Give Money must grant the exact chosen amount.");
        master.money=uint.MaxValue-7;PlayerCheats.GoldFactor=10;master.GiveMoney(1,false);
        Assert(master.money==uint.MaxValue,"Gold bonuses must saturate instead of overflowing.");
        master.money=uint.MaxValue-3;PlayerCheats.GiveManualMoney(100);Assert(master.money==uint.MaxValue,"Manual money grants must also saturate.");
        master.money=uint.MaxValue-1;master.GiveMoney(5,false);
        Assert(master.money==uint.MaxValue,"Native reward overflow must saturate while gold multiplication is enabled.");
        master.money=10;master.PublishGold(float.NaN);master.PublishGold(float.PositiveInfinity);master.PublishGold(-2);
        Assert(master.money==10,"Invalid gold event amounts must not grant a bonus.");
        var nextMaster=new CharacterMaster {money=20};Root.LocalPlayer=nextMaster;PlayerCheats.Tick();
        Assert(master.GoldSubscribers==0 && master.MoneySubscribers==0 && nextMaster.GoldSubscribers==1 && nextMaster.MoneySubscribers==1,"Replacing the local master must transfer gold subscriptions.");
        master.money=10;master.GiveMoney(1,false);nextMaster.GiveMoney(1,false);
        Assert(master.money==11 && nextMaster.money==30,"Old masters must stop receiving multiplier bonuses.");
        PlayerCheats.GoldMultiplier=false;PlayerCheats.SettingsChanged();nextMaster.GiveMoney(2,false);
        Assert(nextMaster.money==32 && nextMaster.GoldSubscribers==0,"Disabling gold must remove its subscription and retain earned money.");

        PlayerCheats.OneHitKills=true;PlayerCheats.SettingsChanged();
        foreach(TeamIndex team in new[]{TeamIndex.Monster,TeamIndex.Lunar,TeamIndex.Void})
        {
            var enemy=CheatBody();
            GlobalEventManager.Damage(new DamageReport {attackerMaster=nextMaster,victimBody=enemy,victimTeamIndex=team,damageDealt=1});
            Assert(enemy.healthComponent.alive,"Damage callbacks must defer native death handling until the cheat update."); PlayerCheats.Tick();
            Assert(!enemy.healthComponent.alive && enemy.healthComponent.Suicides==1,"A positive local hit must kill a living hostile target.");
        }
        foreach(TeamIndex team in new[]{TeamIndex.Player,TeamIndex.Neutral,TeamIndex.None})
        {
            var ally=CheatBody();
            GlobalEventManager.Damage(new DamageReport {attackerMaster=nextMaster,victimBody=ally,victimTeamIndex=team,damageDealt=5});
            Assert(ally.healthComponent.alive,"One-Hit Kills must exclude allies and neutral bodies.");
        }
        var spared=CheatBody();
        GlobalEventManager.Damage(new DamageReport {attackerMaster=master,victimBody=spared,victimTeamIndex=TeamIndex.Monster,damageDealt=5});
        GlobalEventManager.Damage(new DamageReport {attackerMaster=nextMaster,victimBody=spared,victimTeamIndex=TeamIndex.Monster,damageDealt=0});
        GlobalEventManager.Damage(new DamageReport {attackerMaster=nextMaster,victimBody=body,victimTeamIndex=TeamIndex.Monster,damageDealt=5});
        GlobalEventManager.Damage(new DamageReport {attackerMaster=nextMaster,victimBody=spared,victimTeamIndex=TeamIndex.Monster,damageDealt=float.NaN});
        GlobalEventManager.Damage(null);
        Assert(spared.healthComponent.alive && body.healthComponent.alive,"Other attackers, zero damage, the local body and null reports must be ignored.");
        var primary=CheatBody();var secondary=CheatBody();
        primary.healthComponent.AfterSuicide=()=>GlobalEventManager.Damage(new DamageReport {attackerMaster=nextMaster,victimBody=secondary,victimTeamIndex=TeamIndex.Monster,damageDealt=1});
        GlobalEventManager.Damage(new DamageReport {attackerMaster=nextMaster,victimBody=primary,victimTeamIndex=TeamIndex.Monster,damageDealt=1});
        PlayerCheats.Tick();
        Assert(primary.healthComponent.Suicides==1 && secondary.healthComponent.alive,"Nested kill callbacks must not cause recursive kill chains.");

        var crowded=Enumerable.Range(0,140).Select(i=>CheatBody()).ToArray();
        foreach(var target in crowded) for(int n=0;n<2;n++) GlobalEventManager.Damage(new DamageReport {attackerMaster=nextMaster,victimBody=target,victimTeamIndex=TeamIndex.Monster,damageDealt=1});
        PlayerCheats.Tick();Assert(crowded.Count(b=>!b.healthComponent.alive)==32,"One-Hit Kills must bound work per update and deduplicate repeated hits.");
        for(int n=0;n<5;n++)PlayerCheats.Tick();Assert(crowded.Count(b=>!b.healthComponent.alive)==128,"Queued kill references must remain within their fixed capacity.");
        var converted=CheatBody();GlobalEventManager.Damage(new DamageReport{attackerMaster=nextMaster,victimBody=converted,victimTeamIndex=TeamIndex.Monster,damageDealt=1}); converted.teamComponent.teamIndex=TeamIndex.Player;PlayerCheats.Tick();
        Assert(converted.healthComponent.alive,"Queued targets that become allies must be spared.");
        var cancelled=CheatBody();GlobalEventManager.Damage(new DamageReport{attackerMaster=nextMaster,victimBody=cancelled,victimTeamIndex=TeamIndex.Monster,damageDealt=1});PlayerCheats.OneHitKills=false;PlayerCheats.SettingsChanged();PlayerCheats.OneHitKills=true;PlayerCheats.SettingsChanged();PlayerCheats.Tick();
        Assert(cancelled.healthComponent.alive,"Turning One-Hit Kills off must cancel queued work.");
        var departing=CheatBody();departing.healthComponent.AfterSuicide=()=>{PlayerCheats.ResetStage();Root.LocalPlayerBody=null;};
        GlobalEventManager.Damage(new DamageReport{attackerMaster=nextMaster,victimBody=departing,victimTeamIndex=TeamIndex.Monster,damageDealt=1});PlayerCheats.Tick();
        Assert(!departing.healthComponent.alive && !PlayerCheats.Status.Contains("error"),"A death callback that changes scenes must stop further local-body access safely.");
        Root.LocalPlayerBody=body;PlayerCheats.Tick();
        PlayerCheats.SpeedMultiplier=true;PlayerCheats.SpeedFactor=2;PlayerCheats.SettingsChanged();
        var oldBody=body;body=CheatBody(30);Root.LocalPlayerBody=body;PlayerCheats.Tick();
        Assert(oldBody.StatSubscribers==0 && oldBody.moveSpeed==7 && body.StatSubscribers==1 && body.moveSpeed==14,"Body replacement must restore the old body and apply to the new one.");
        PlayerCheats.UnlimitedShields=true;PlayerCheats.SettingsChanged();
        Assert(body.maxShield==30 && body.healthComponent.shield==30,"Existing native shields must retain their capacity.");
        var zone=new HoldoutZoneController {baseChargeDuration=90};PlayerCheats.InstantCharge(zone);PlayerCheats.InstantCharge(zone);
        Assert(zone.baseChargeDuration==1,"Instant charging must preserve the original duration across repeated clicks.");
        PlayerCheats.ResetStage();Assert(zone.baseChargeDuration==90 && body.moveSpeed==7 && body.StatSubscribers==0,"Stage transitions must restore durations and detach the old body.");
        PlayerCheats.Tick();Assert(body.moveSpeed==14,"Enabled stat cheats must resume after a stage transition.");
        PlayerCheats.InstantCharge(zone);zone.baseChargeDuration=10;PlayerCheats.ResetStage();
        Assert(zone.baseChargeDuration==10,"Cleanup must preserve a charge duration changed by another system.");
        NetworkServer.active=false;PlayerCheats.Tick();
        Assert(!PlayerCheats.UnlimitedShields && !PlayerCheats.SpeedMultiplier && !PlayerCheats.OneHitKills && body.moveSpeed==7,"Authority loss must clear new cheats and restore owned stats.");
        NetworkServer.active=true;PlayerCheats.UnlimitedShields=true;body.healthComponent.ThrowRecharge=true;body.healthComponent.shield=0;PlayerCheats.SettingsChanged();
        Assert(!PlayerCheats.UnlimitedShields && PlayerCheats.Status.Contains("error") && body.StatSubscribers==0,"A managed cheat failure must disable controls and release owned state.");
        PlayerCheats.Tick();Assert(PlayerCheats.Status.Contains("error"),"A paused error must not retry every frame.");
        Movement.flightToggle=true;PlayerCheats.Tick(); Assert((body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage)!=0 && PlayerCheats.Status.Contains("error"),"Paused character cheats must preserve Flight fall protection."); Movement.flightToggle=false;PlayerCheats.Tick(); Assert((body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage)==0,"Flight fall protection must still restore while new cheats are paused.");
        body.healthComponent.ThrowRecharge=false;PlayerCheats.SpeedMultiplier=true;PlayerCheats.SettingsChanged();
        Assert(body.moveSpeed==14,"An explicit settings change must allow recovery after a failure.");
        body.ThrowRecalculation=true;PlayerCheats.SettingsChanged(); Assert(!PlayerCheats.SpeedMultiplier && PlayerCheats.Status.Contains("error"),"A settings callback failure must stop its cheats without escaping into UI actions."); body.ThrowRecalculation=false; PlayerCheats.SpeedMultiplier=true;PlayerCheats.SettingsChanged();
        Run.instance.End();Assert(!PlayerCheats.SpeedMultiplier && body.moveSpeed==7,"Ending a run must reset character cheats.");
        PlayerCheats.Shutdown();PlayerCheats.Shutdown();
        Assert(GlobalEventManager.Subscribers==0 && Run.DestroySubscribers==0 && nextMaster.GoldSubscribers==0,"Shutdown must release every event subscription.");
        Root.LocalPlayer=null;Root.LocalPlayerBody=null;Movement.flightToggle=false;
        Console.WriteLine("PASS: "+assertions+" total gameplay assertions including character cheats.");
    }
}
