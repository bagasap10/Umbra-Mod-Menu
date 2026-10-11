using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace UmbraMenu
{
    // Derived stats are reapplied after the game's calculation, never multiplied in-place each tick.
    internal static class PlayerCheats
    {
        public static bool UnlimitedShields, NoFallDamage, UnlimitedJumps, SpeedMultiplier, GoldMultiplier, OneHitKills, FastAttackSpeed, MaxCritical;
        public static float SpeedFactor = 2f, AttackFactor = 5f;
        public static int GoldFactor = 2;
        public static string Status { get; private set; } = "Character cheats are off.";
        private static bool initialized, applying, grantingGold, killing, ownsFallFlag, originalFallFlag, faulted;
        private static CharacterBody body;
        private static CharacterMaster goldOwner;
        private static uint trackedMoney, moneyBeforeChange;
        private static float naturalSpeed, naturalAttack, naturalCrit, naturalShield, appliedSpeed, appliedAttack, appliedCrit, appliedShield;
        private static int naturalJumps, appliedJumps;
        private static bool statsApplied;
        private const int MaxPendingKills = 128, KillsPerTick = 32;
        private sealed class KillRequest { public HealthComponent Health; public CharacterBody Victim; public CharacterMaster Owner; public Run Run; }
        private static readonly System.Collections.Generic.Queue<KillRequest> pendingKills = new System.Collections.Generic.Queue<KillRequest>();
        private static readonly System.Collections.Generic.HashSet<HealthComponent> queuedKills = new System.Collections.Generic.HashSet<HealthComponent>();
        private static bool HasStats => SpeedMultiplier || FastAttackSpeed || MaxCritical || UnlimitedShields || UnlimitedJumps;
        public static void Initialize()
        {
            if (initialized) return;
            GlobalEventManager.onServerDamageDealt += DamageDealt;
            Run.onRunDestroyGlobal += RunEnded;
            initialized = true;
        }
        public static void SettingsChanged()
        {
            Initialize(); faulted = false;
            try
            {
                if (!OneHitKills) ClearKills();
                RestoreStatsIfOwned();
                SpeedFactor = Finite(SpeedFactor) ? Mathf.Clamp(SpeedFactor, 1.5f, 10f) : 2f;
                AttackFactor = Finite(AttackFactor) ? Mathf.Clamp(AttackFactor, 2f, 20f) : 5f;
                GoldFactor = Mathf.Clamp(GoldFactor, 2, 10);
                AttachBody(UmbraMenu.LocalPlayerBody);
                AttachGold(GoldMultiplier ? UmbraMenu.LocalPlayer : null);
                if (body) body.RecalculateStats();
                Tick();
            }
            catch (Exception error) { Fail(error); }
        }
        public static void Tick()
        {
            if (!initialized) return;
            try
            {
                if (!WorldCheats.IsHost && (HasStats || NoFallDamage || GoldMultiplier || OneHitKills)) Reset();
                AttachBody(UmbraMenu.InGameCheck() ? UmbraMenu.LocalPlayerBody : null);
                if (body) UpdateFallFlag();
                if (faulted) return;
                AttachGold(GoldMultiplier && WorldCheats.IsHost ? UmbraMenu.LocalPlayer : null);
                if (!body) return;
                FinishKills();
                if (!body || !WorldCheats.IsHost) return;
                if (UnlimitedJumps && body.characterMotor) body.characterMotor.jumpCount = 0;
                if (UnlimitedShields && WorldCheats.IsHost && body.healthComponent && body.healthComponent.alive)
                {
                    float full = body.healthComponent.fullShield;
                    if (Finite(full) && full > 0) { if (body.healthComponent.shield < full) body.healthComponent.RechargeShieldFull(); else if (body.healthComponent.shield > full) body.healthComponent.Networkshield = full; }
                }
                Status = HasStats || NoFallDamage || GoldMultiplier || OneHitKills ? "Character cheats active." : "Character cheats are off.";
            }
            catch (Exception error) { Fail(error); }
        }
        private static void AttachBody(CharacterBody next)
        {
            if (body == next) return;
            DetachBody();
            body = next;
            if (!body) return;
            body.onRecalculateStats += Recalculated;
            if (HasStats) body.RecalculateStats();
        }
        private static void DetachBody()
        {
            var old = body; body = null;
            if (!old) { ownsFallFlag = statsApplied = false; return; }
            old.onRecalculateStats -= Recalculated;
            RestoreStats(old);
            if (ownsFallFlag && !originalFallFlag) old.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            ownsFallFlag = false;
        }
        private static void Recalculated(CharacterBody current)
        {
            if (current != body || applying || !WorldCheats.IsHost) return;
            applying = true;
            try
            {
                // The event runs after all native derived values have been computed.
                naturalSpeed = current.moveSpeed; naturalAttack = current.attackSpeed; naturalCrit = current.crit;
                naturalShield = current.maxShield; naturalJumps = current.maxJumpCount;
                appliedSpeed = SpeedMultiplier ? Scale(naturalSpeed, SpeedFactor, 100f) : naturalSpeed;
                appliedAttack = FastAttackSpeed ? Scale(naturalAttack, AttackFactor, 100f) : naturalAttack;
                appliedCrit = MaxCritical ? Math.Max(100f, naturalCrit) : naturalCrit;
                appliedShield = UnlimitedShields && naturalShield <= 0 ? (Finite(current.maxHealth) ? Math.Max(1f, current.maxHealth) : 1f) : naturalShield;
                appliedJumps = UnlimitedJumps ? Math.Max(2, naturalJumps) : naturalJumps;
                statsApplied = HasStats;
                if (HasStats)
                {
                    current.SetField("<moveSpeed>k__BackingField", appliedSpeed);
                    current.SetField("<attackSpeed>k__BackingField", appliedAttack);
                    current.SetField("<crit>k__BackingField", appliedCrit);
                    current.SetField("<maxShield>k__BackingField", appliedShield);
                    current.SetField("<maxJumpCount>k__BackingField", appliedJumps);
                }
                statsApplied = HasStats;
            }
            catch (Exception error) { Fail(error); }
            finally { applying = false; }
        }
        private static float Scale(float value, float factor, float cap)
        {
            if (!Finite(value) || value < 0) return value;
            return Math.Max(value, Math.Min(cap, value * factor));
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static void RestoreStats(CharacterBody current)
        {
            if (!statsApplied) return;
            if (current.moveSpeed == appliedSpeed) current.SetField("<moveSpeed>k__BackingField", naturalSpeed);
            if (current.attackSpeed == appliedAttack) current.SetField("<attackSpeed>k__BackingField", naturalAttack);
            if (current.crit == appliedCrit) current.SetField("<crit>k__BackingField", naturalCrit);
            if (current.maxShield == appliedShield)
            {
                current.SetField("<maxShield>k__BackingField", naturalShield);
                if (current.healthComponent && current.healthComponent.shield > Math.Max(0, naturalShield))
                    current.healthComponent.Networkshield = Math.Max(0, naturalShield);
            }
            if (current.maxJumpCount == appliedJumps) current.SetField("<maxJumpCount>k__BackingField", naturalJumps);
            statsApplied = false;
        }
        private static void RestoreStatsIfOwned() { if (body) RestoreStats(body); }
        private static readonly System.Collections.Generic.Dictionary<HoldoutZoneController, float> instantZones = new System.Collections.Generic.Dictionary<HoldoutZoneController, float>();
        public static void InstantCharge(HoldoutZoneController zone)
        {
            if (!WorldCheats.IsHost || !zone || instantZones.Count >= 128 || !Finite(zone.baseChargeDuration) || zone.baseChargeDuration <= 1) return;
            if (!instantZones.ContainsKey(zone)) instantZones.Add(zone, zone.baseChargeDuration);
            zone.baseChargeDuration = 1;
        }
        private static void RestoreCharges()
        {
            foreach (var entry in instantZones) if (entry.Key && entry.Key.baseChargeDuration == 1) entry.Key.baseChargeDuration = entry.Value;
            instantZones.Clear();
        }
        private static void UpdateFallFlag()
        {
            bool required = NoFallDamage || Menus.Movement.flightToggle || Menus.Movement.jumpPackToggle;
            if (required)
            {
                if (!ownsFallFlag) { originalFallFlag = (body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) != 0; ownsFallFlag = true; }
                body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            }
            else if (ownsFallFlag)
            {
                if (!originalFallFlag) body.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
                ownsFallFlag = false;
            }
        }
        private static void AttachGold(CharacterMaster next)
        {
            if (goldOwner == next) return;
            if (goldOwner) { goldOwner.OnGoldCollected -= GoldCollected; goldOwner.OnMoneyValueChanged -= MoneyChanged; }
            goldOwner = next;
            if (goldOwner) { trackedMoney = moneyBeforeChange = goldOwner.money; goldOwner.OnMoneyValueChanged += MoneyChanged; goldOwner.OnGoldCollected += GoldCollected; }
        }
        private static void MoneyChanged(CharacterMaster current) { if (current != goldOwner) return; moneyBeforeChange = trackedMoney; trackedMoney = current.money; }
        private static void GoldCollected(float amount)
        {
            if (grantingGold || !GoldMultiplier || !WorldCheats.IsHost || !goldOwner || goldOwner != UmbraMenu.LocalPlayer || !Finite(amount) || amount <= 0) return;
            try
            {
                // GiveMoney raises this event again; a scoped guard prevents multiplying our bonus.
                ulong earned = (ulong)Math.Min(uint.MaxValue, Math.Floor((double)amount));
                if ((ulong)moneyBeforeChange + earned > uint.MaxValue && goldOwner.money < moneyBeforeChange) goldOwner.money = uint.MaxValue;
                ulong extra = earned * (ulong)(Mathf.Clamp(GoldFactor, 2, 10) - 1);
                uint bonus = (uint)Math.Min(extra, (ulong)uint.MaxValue - goldOwner.money);
                if (bonus == 0) return;
                grantingGold = true;
                try { goldOwner.GiveMoney(bonus, true); }
                finally { grantingGold = false; }
            }
            catch (Exception error) { Fail(error); }
        }
        public static void GiveManualMoney(uint quantity)
        {
            if (!UmbraMenu.LocalPlayer || !NetworkServer.active) return;
            bool previous = grantingGold; grantingGold = true;
            try
            {
                uint bounded = (uint)Math.Min(quantity, (ulong)uint.MaxValue - UmbraMenu.LocalPlayer.money);
                UmbraMenu.LocalPlayer.GiveMoney(bounded, false);
            }
            finally { grantingGold = previous; }
        }
        private static void DamageDealt(DamageReport report)
        {
            if (!OneHitKills || killing || !WorldCheats.IsHost || report == null || !Finite(report.damageDealt) || report.damageDealt <= 0 ||
                !UmbraMenu.LocalPlayer || report.attackerMaster != UmbraMenu.LocalPlayer || !report.victimBody ||
                report.victimBody == UmbraMenu.LocalPlayerBody || !Hostile(report.victimTeamIndex)) return;
            var health = report.victimBody.healthComponent;
            if (!health || !health.alive || !UmbraMenu.LocalPlayerBody) return;
            if (pendingKills.Count >= MaxPendingKills || !queuedKills.Add(health)) return;
            pendingKills.Enqueue(new KillRequest { Health=health, Victim=report.victimBody, Owner=report.attackerMaster, Run=Run.instance });
        }
        private static void ClearKills() { pendingKills.Clear(); queuedKills.Clear(); }
        private static void FinishKills()
        {
            if (!OneHitKills) { ClearKills(); return; }
            for (int i=0; i<KillsPerTick && pendingKills.Count>0 && OneHitKills && WorldCheats.IsHost && UmbraMenu.LocalPlayerBody; i++)
            {
                KillRequest request=pendingKills.Dequeue(); queuedKills.Remove(request.Health);
                if (request.Run != Run.instance || request.Owner != UmbraMenu.LocalPlayer || !request.Health || !request.Health.alive ||
                    !request.Victim || !request.Victim.teamComponent || !Hostile(request.Victim.teamComponent.teamIndex)) continue;
                killing=true;
                try { request.Health.Suicide(UmbraMenu.LocalPlayerBody.gameObject, UmbraMenu.LocalPlayerBody.gameObject, default(DamageTypeCombo)); }
                finally { killing=false; }
            }
        }
        private static bool Hostile(TeamIndex team) => team == TeamIndex.Monster || team == TeamIndex.Lunar || team == TeamIndex.Void;
        private static void RunEnded(Run run) { try { Reset(); } catch (Exception error) { Fail(error); } }
        private static void Fail(Exception error)
        {
            Debug.LogWarning("Umbra: character cheats paused: " + error);
            try { Reset(); } catch (Exception cleanup) { Debug.LogWarning("Umbra: character cheat cleanup failed: " + cleanup); }
            faulted = true; Status = "Character cheats paused after an error; see Player.log.";
        }
        public static void ResetStage() { ClearKills(); AttachGold(null); DetachBody(); RestoreCharges(); }
        public static void Reset()
        {
            UnlimitedShields = NoFallDamage = UnlimitedJumps = SpeedMultiplier = GoldMultiplier = OneHitKills = FastAttackSpeed = MaxCritical = false;
            ResetStage(); SpeedFactor = 2f; AttackFactor = 5f; GoldFactor = 2; faulted = false; Status = "Character cheats are off.";
        }
        public static void Shutdown()
        {
            if (initialized) { GlobalEventManager.onServerDamageDealt -= DamageDealt; Run.onRunDestroyGlobal -= RunEnded; initialized = false; }
            Reset();
        }
    }
}
