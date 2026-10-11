using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace UmbraMenu
{
    internal sealed class WorldRarityDropTable : ExplicitPickupDropTable
    {
        public void InitializeForRun(Run run) { base.Regenerate(run); }
    }
    internal enum LootRarity { Natural, Common, Uncommon, Legendary, Boss, Lunar, Void, Meal, Hallowed, Hex, Pages }
    internal static class WorldCheats
    {
        internal const int MaxExtraChests = 50, MaxTrackedChests = 256, MaxPurchases = 512, MaxDrops = 20, MaxStageCredits = 5000;
        public static bool StageAbundance, ChestParadise, FreeShopping, LootMultiplier, ProcChaos, FreezeEnemies;
        public static int StageMultiplier = 2, ChestMultiplier = 2, DropMultiplier = 2, LuckBonus = 10;
        public static LootRarity Rarity;
        public static int ExtraChestsSpawned { get; private set; }
        public static int PendingChests => extraRequests.Count;
        public static string Status { get; private set; } = "All cheats are off.";
        private static bool initialized, faulted;
        private static Run currentRun;
        private static SceneDirector populatedDirector;
        private static float nextTick, nextScan;
        private static int freezeCursor;
        private static WorldRarityDropTable rarityTable;
        private static LootRarity tableRarity, unavailableRarity;
        private static CharacterMaster luckOwner;
        private static float originalLuck, appliedLuck;
        private static readonly Dictionary<PurchaseInteraction, PriceState> prices = new Dictionary<PurchaseInteraction, PriceState>();
        private static readonly Dictionary<ChestBehavior, ChestState> chests = new Dictionary<ChestBehavior, ChestState>();
        private static readonly List<SpawnCard> stageChests = new List<SpawnCard>();
        private static readonly Queue<SpawnCard> extraRequests = new Queue<SpawnCard>();
        private static readonly List<PurchaseInteraction> stalePrices = new List<PurchaseInteraction>();
        private static readonly List<ChestBehavior> staleChests = new List<ChestBehavior>();
        private sealed class PriceState { public int Original; }
        private sealed class ChestState
        {
            public int OriginalCount, AppliedCount;
            public bool CountApplied, RarityApplied;
            public PickupDropTable OriginalTable;
            public UniquePickup OriginalPickup, AppliedPickup;
        }

        public static bool IsHost => NetworkServer.active && Run.instance;
        public static void Initialize()
        {
            if (initialized) return;
            SceneDirector.onPrePopulateSceneServer += BeforePopulate;
            SceneDirector.onSpawnInteractableServer += InteractableSpawned;
            SceneDirector.onPostPopulateSceneServer += AfterPopulate;
            Run.onRunDestroyGlobal += RunEnded;
            Run.onAvailablePickupsModified += PickupsChanged;
            initialized = true;
        }
        public static bool RequireHost()
        {
            if (IsHost) return true;
            Chat.AddMessage("Umbra: This cheat requires the host of an active run.");
            return false;
        }
        public static void SettingsChanged()
        {
            Initialize(); faulted = false;
            if (!FreeShopping) RestorePrices();
            if (!ProcChaos) RestoreLuck();
            if (!ChestParadise) extraRequests.Clear();
            bool differentRarity = tableRarity != Rarity;
            RestoreChestChanges(!LootMultiplier, Rarity == LootRarity.Natural || differentRarity);
            if (differentRarity || Rarity == LootRarity.Natural) ReleaseTable();
            nextTick = nextScan = 0;
        }
        private static void BeforePopulate(SceneDirector director)
        {
            Guard(() =>
            {
                if (!IsHost || !director || populatedDirector == director) return;
                ResetStage(); currentRun = Run.instance; populatedDirector = director;
                if (StageAbundance && director.interactableCredit > 0)
                {
                    int original = director.interactableCredit;
                    int desired = (int)Math.Min(MaxStageCredits, (long)original * Mathf.Clamp(StageMultiplier, 2, 5));
                    director.SetField("<interactableCredit>k__BackingField", Math.Max(original, desired));
                }
            });
        }
        private static void InteractableSpawned(SceneDirector director, GameObject spawned, DirectorCard card)
        {
            Guard(() =>
            {
                if (!IsHost || director != populatedDirector || !ChestParadise || !spawned || card == null || stageChests.Count >= MaxTrackedChests) return;
                var chest = spawned.GetComponent<ChestBehavior>();
                SpawnCard spawnCard = card.GetSpawnCard();
                if (chest && spawnCard && spawnCard.sendOverNetwork) stageChests.Add(spawnCard);
            });
        }
        private static void AfterPopulate(SceneDirector director)
        {
            Guard(() =>
            {
                if (!IsHost || director != populatedDirector || !ChestParadise || stageChests.Count == 0) return;
                int count = Math.Min(MaxExtraChests, stageChests.Count * (Mathf.Clamp(ChestMultiplier, 2, 5) - 1));
                for (int i = 0; i < count; i++) extraRequests.Enqueue(stageChests[i % stageChests.Count]);
                stageChests.Clear(); nextTick = nextScan = 0;
            });
        }
        private static void RunEnded(Run run) { Guard(Reset); }
        private static void PickupsChanged(Run run)
        {
            if (run != currentRun) return;
            Guard(() => { RestoreChestChanges(false, true); ReleaseTable(); nextTick = 0; });
        }
        public static void Tick()
        {
            if (!initialized || faulted) return;
            if (currentRun && currentRun != Run.instance) ResetStage();
            if (!IsHost)
            {
                if (currentRun || prices.Count > 0 || chests.Count > 0 || luckOwner || extraRequests.Count > 0
                    || StageAbundance || ChestParadise || FreeShopping || LootMultiplier || ProcChaos || FreezeEnemies || Rarity != LootRarity.Natural) Reset();
                return;
            }
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + .2f;
            Guard(() =>
            {
                currentRun = Run.instance; SpawnExtraChests();
                if (Time.unscaledTime >= nextScan && (FreeShopping || LootMultiplier || Rarity != LootRarity.Natural))
                {
                    nextScan = Time.unscaledTime + .5f; ScanTargets();
                }
                UpdatePrices(); UpdateChests(); UpdateLuck();
                if (FreezeEnemies) FreezeHostiles();
                if (unavailableRarity != Rarity || Rarity == LootRarity.Natural)
                    Status = StageAbundance || ChestParadise || FreeShopping || LootMultiplier || ProcChaos || FreezeEnemies || Rarity != LootRarity.Natural
                        ? "Host active / Extra chests: " + ExtraChestsSpawned + " / Queued: " + extraRequests.Count : "All cheats are off.";
            });
        }
        private static void SpawnExtraChests()
        {
            if (!ChestParadise || !DirectorCore.instance) { extraRequests.Clear(); return; }
            // Spread placement across ticks; reuse cards that actually spawned on this stage.
            for (int attempt = 0; attempt < 2 && extraRequests.Count > 0; attempt++)
            {
                SpawnCard card = extraRequests.Dequeue();
                if (!card || !card.sendOverNetwork) continue;
                var request = new DirectorSpawnRequest(card, new DirectorPlacementRule { placementMode = DirectorPlacementRule.PlacementMode.Random }, Run.instance.spawnRng);
                GameObject spawned = DirectorCore.instance.TrySpawnObject(request);
                if (!spawned) continue;
                ExtraChestsSpawned++;
                var purchase = spawned.GetComponent<PurchaseInteraction>();
                if (purchase && purchase.costType == CostTypeIndex.Money)
                    purchase.Networkcost = Run.instance.GetDifficultyScaledCost(purchase.cost);
                // Start() initializes contents and drop counts later.
                nextScan = 0;
            }
        }
        private static void ScanTargets()
        {
            stalePrices.Clear();
            foreach (var pair in prices) if (!pair.Key) stalePrices.Add(pair.Key);
            foreach (var purchase in stalePrices) prices.Remove(purchase);
            staleChests.Clear();
            foreach (var pair in chests) if (!pair.Key) staleChests.Add(pair.Key);
            foreach (var chest in staleChests) chests.Remove(chest);
            if (FreeShopping)
                foreach (var purchase in Object.FindObjectsOfType<PurchaseInteraction>())
                    if (purchase && purchase.costType == CostTypeIndex.Money && !prices.ContainsKey(purchase) && prices.Count < MaxPurchases)
                        prices.Add(purchase, new PriceState { Original = purchase.cost });
            if (LootMultiplier || Rarity != LootRarity.Natural)
                foreach (var chest in Object.FindObjectsOfType<ChestBehavior>())
                    if (chest && !chest.isChestOpened && chest.currentPickup.pickupIndex != PickupIndex.none && !chests.ContainsKey(chest) && chests.Count < MaxTrackedChests)
                        chests.Add(chest, new ChestState());
        }
        private static void UpdatePrices()
        {
            if (!FreeShopping) return;
            foreach (var pair in prices)
            {
                PurchaseInteraction purchase = pair.Key;
                if (!purchase || purchase.costType != CostTypeIndex.Money) continue;
                if (purchase.cost != 0) pair.Value.Original = purchase.cost;
                purchase.Networkcost = 0;
            }
        }
        private static void UpdateChests()
        {
            if (!LootMultiplier && Rarity == LootRarity.Natural) return;
            EnsureRarityTable();
            foreach (var pair in chests)
            {
                ChestBehavior chest = pair.Key; ChestState state = pair.Value;
                if (!chest || chest.isChestOpened || chest.currentPickup.pickupIndex == PickupIndex.none) continue;
                if (LootMultiplier)
                {
                    int count = chest.GetField<int>("dropCount");
                    if (count > 0 && (!state.CountApplied || count != state.AppliedCount)) state.OriginalCount = count;
                    if (state.OriginalCount > 0)
                    {
                        int desired = Math.Max(state.OriginalCount, (int)Math.Min(MaxDrops, (long)state.OriginalCount * Mathf.Clamp(DropMultiplier, 2, 5)));
                        state.AppliedCount = desired; state.CountApplied = true;
                        if (count != desired) chest.SetField("dropCount", desired);
                    }
                }
                var pickupDef = PickupCatalog.GetPickupDef(chest.currentPickup.pickupIndex);
                if (rarityTable && !state.RarityApplied && pickupDef != null && pickupDef.itemIndex != ItemIndex.None)
                {
                    state.OriginalTable = chest.dropTable; state.OriginalPickup = chest.currentPickup; state.RarityApplied = true;
                    chest.dropTable = rarityTable; chest.Roll(); state.AppliedPickup = chest.currentPickup;
                }
            }
        }
        private static bool MatchesRarity(ItemTier tier)
        {
            switch (Rarity)
            {
                case LootRarity.Common: return tier == ItemTier.Tier1;
                case LootRarity.Uncommon: return tier == ItemTier.Tier2;
                case LootRarity.Legendary: return tier == ItemTier.Tier3;
                case LootRarity.Boss: return tier == ItemTier.Boss;
                case LootRarity.Lunar: return tier == ItemTier.Lunar;
                case LootRarity.Void: return tier == ItemTier.VoidTier1 || tier == ItemTier.VoidTier2 || tier == ItemTier.VoidTier3 || tier == ItemTier.VoidBoss;
                case LootRarity.Meal: return tier == ItemTier.FoodTier;
                case LootRarity.Hallowed: return tier == ItemTier.Cursed;
                case LootRarity.Hex: return tier == ItemTier.Hex;
                case LootRarity.Pages: return tier == ItemTier.Page;
                default: return false;
            }
        }
        private static void EnsureRarityTable()
        {
            if (Rarity == LootRarity.Natural || rarityTable || unavailableRarity == Rarity) return;
            var entries = new List<ExplicitPickupDropTable.PickupDefEntry>(); var seen = new HashSet<ItemIndex>();
            foreach (var pool in Run.instance.allAvailableTierDroplists)
            {
                if (!MatchesRarity(pool.Key) || pool.Value == null) continue;
                foreach (PickupIndex pickup in pool.Value)
                {
                    var pickupDef = PickupCatalog.GetPickupDef(pickup);
                    if (pickupDef == null || pickupDef.itemIndex == ItemIndex.None || !seen.Add(pickupDef.itemIndex)) continue;
                    ItemDef def = ItemCatalog.GetItemDef(pickupDef.itemIndex);
                    if (!def || def.hidden || !Run.instance.IsItemAvailable(pickupDef.itemIndex)) continue;
                    entries.Add(new ExplicitPickupDropTable.PickupDefEntry { pickupDef = def, pickupWeight = 1f });
                }
            }
            tableRarity = Rarity;
            if (entries.Count == 0)
            {
                unavailableRarity = Rarity; Status = "No available items for " + Rarity + "; original loot retained."; return;
            }
            rarityTable = ScriptableObject.CreateInstance<WorldRarityDropTable>();
            rarityTable.hideFlags = HideFlags.HideAndDontSave; rarityTable.canDropBeReplaced = false;
            rarityTable.pickupEntries = entries.ToArray(); rarityTable.InitializeForRun(Run.instance); unavailableRarity = LootRarity.Natural;
        }
        private static void UpdateLuck()
        {
            if (!ProcChaos) return;
            CharacterMaster master = UmbraMenu.LocalPlayer;
            if (luckOwner != master) { RestoreLuck(); luckOwner = master; if (luckOwner) originalLuck = appliedLuck = luckOwner.luck; }
            if (!luckOwner) return;
            if (luckOwner.luck != appliedLuck) originalLuck = luckOwner.luck;
            appliedLuck = originalLuck + Mathf.Clamp(LuckBonus, 1, 20);
            if (luckOwner.luck != appliedLuck) luckOwner.SetField("<luck>k__BackingField", appliedLuck);
        }
        private static void FreezeHostiles()
        {
            var bodies = CharacterBody.readOnlyInstancesList; int count = bodies.Count;
            if (count == 0) return;
            for (int n = 0, checks = Math.Min(256, count); n < checks; n++)
            {
                if (freezeCursor >= count) freezeCursor = 0;
                CharacterBody body = bodies[freezeCursor++];
                if (!body || !body.healthComponent || !body.healthComponent.alive || !body.teamComponent) continue;
                TeamIndex team = body.teamComponent.teamIndex;
                if (team != TeamIndex.Monster && team != TeamIndex.Lunar && team != TeamIndex.Void) continue;
                var hurt = body.GetComponent<SetStateOnHurt>();
                if (!hurt || !hurt.targetStateMachine) continue;
                bool previous = hurt.canBeStunned;
                try { hurt.canBeStunned = true; hurt.SetStun(1f); }
                finally { hurt.canBeStunned = previous; }
            }
        }
        private static void RestorePrices()
        {
            foreach (var pair in prices)
                if (pair.Key && pair.Key.costType == CostTypeIndex.Money && pair.Key.cost == 0) pair.Key.Networkcost = pair.Value.Original;
            prices.Clear();
        }
        private static void RestoreLuck()
        {
            if (luckOwner && luckOwner.luck == appliedLuck) luckOwner.SetField("<luck>k__BackingField", originalLuck);
            luckOwner = null; originalLuck = appliedLuck = 0;
        }
        private static void RestoreChestChanges(bool counts, bool rarity)
        {
            foreach (var pair in chests)
            {
                ChestBehavior chest = pair.Key; ChestState state = pair.Value;
                if (!chest) continue;
                if (counts && state.CountApplied)
                {
                    if (chest.GetField<int>("dropCount") == state.AppliedCount) chest.SetField("dropCount", state.OriginalCount);
                    state.CountApplied = false;
                }
                if (rarity && state.RarityApplied)
                {
                    if (chest.dropTable == rarityTable)
                    {
                        chest.dropTable = state.OriginalTable;
                        if (!chest.isChestOpened && chest.currentPickup.pickupIndex == state.AppliedPickup.pickupIndex)
                            chest.SetField("<currentPickup>k__BackingField", state.OriginalPickup);
                    }
                    state.RarityApplied = false; state.OriginalTable = null;
                }
            }
        }
        private static void ReleaseTable()
        {
            if (rarityTable) Object.Destroy(rarityTable);
            rarityTable = null; unavailableRarity = LootRarity.Natural;
        }
        public static void ResetStage()
        {
            RestorePrices(); RestoreLuck(); RestoreChestChanges(true, true);
            chests.Clear(); stageChests.Clear(); extraRequests.Clear(); stalePrices.Clear(); staleChests.Clear();
            ReleaseTable(); currentRun = null; populatedDirector = null; ExtraChestsSpawned = freezeCursor = 0; nextTick = nextScan = 0;
        }
        public static void Reset()
        {
            StageAbundance = ChestParadise = FreeShopping = LootMultiplier = ProcChaos = FreezeEnemies = false;
            faulted = false; ResetStage();
            StageMultiplier = ChestMultiplier = DropMultiplier = 2; LuckBonus = 10; Rarity = LootRarity.Natural; Status = "All cheats are off.";
        }
        public static void Shutdown()
        {
            if (initialized)
            {
                SceneDirector.onPrePopulateSceneServer -= BeforePopulate; SceneDirector.onSpawnInteractableServer -= InteractableSpawned;
                SceneDirector.onPostPopulateSceneServer -= AfterPopulate; Run.onRunDestroyGlobal -= RunEnded;
                Run.onAvailablePickupsModified -= PickupsChanged; initialized = false;
            }
            Reset();
        }
        private static void Guard(Action action)
        {
            try { action(); }
            catch (Exception error)
            {
                Debug.LogWarning("Umbra: World Cheats disabled after an error: " + error);
                try { Reset(); } catch (Exception cleanupError) { Debug.LogWarning("Umbra: World Cheats cleanup: " + cleanupError); }
                faulted = true; Status = "Cheats paused after an error; see Player.log.";
            }
        }
    }
}
