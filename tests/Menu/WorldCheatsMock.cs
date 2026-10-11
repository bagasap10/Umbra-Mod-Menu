using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UmbraMenu;
using UnityEngine;

namespace UnityEngine
{
    public class ScriptableObject : Object
    {
        public static int Created;
        public static T CreateInstance<T>() where T : ScriptableObject { Created++; return (T)Activator.CreateInstance(typeof(T), true); }
    }
}
public class Xoroshiro128Plus { public int Next; public int RangeInt(int min,int max) => min + Next++ % (max-min); }
namespace RoR2
{
    public enum CostTypeIndex { None, Money, PercentHealth, LunarCoin, WhiteItem, RedItem }
    public partial class PurchaseInteraction
    {
        public int cost;
        public CostTypeIndex costType;
        public int Networkcost { get => cost; set => cost = value; }
    }
    public class PickupDropTable : ScriptableObject
    {
        public bool canDropBeReplaced;
        public PickupIndex Pickup = PickupIndex.none;
        public virtual UniquePickup GeneratePickup(Xoroshiro128Plus rng) => new UniquePickup(Pickup);
    }
    public class ExplicitPickupDropTable : PickupDropTable
    {
        public struct PickupDefEntry { public UnityEngine.Object pickupDef; public float pickupWeight; }
        public PickupDefEntry[] pickupEntries;
        public static int Regenerations;
        protected void Regenerate(Run run) { Regenerations++; }
        public override UniquePickup GeneratePickup(Xoroshiro128Plus rng)
        {
            var entry = pickupEntries[rng.RangeInt(0,pickupEntries.Length)];
            return new UniquePickup(PickupCatalog.FindPickupIndex(ItemCatalog.Defs.Single(pair => ReferenceEquals(pair.Value,entry.pickupDef)).Key));
        }
    }
    public partial class ChestBehavior
    {
        public PickupDropTable dropTable;
        public bool isChestOpened;
        private int dropCount = 1;
        public int NativeDropCount => dropCount;
        public int RollCalls;
        public void Roll()
        {
            RollCalls++;
            if (dropTable) this.SetField("<currentPickup>k__BackingField", dropTable.GeneratePickup(new Xoroshiro128Plus()));
        }
    }
    public class SpawnCard : ScriptableObject { public bool sendOverNetwork = true; }
    public class DirectorCard { public SpawnCard spawnCard; public SpawnCard GetSpawnCard() => spawnCard; }
    public class DirectorPlacementRule { public enum PlacementMode { Random } public PlacementMode placementMode; }
    public class DirectorSpawnRequest
    {
        public SpawnCard spawnCard; public DirectorPlacementRule placementRule; public Xoroshiro128Plus rng;
        public DirectorSpawnRequest(SpawnCard card, DirectorPlacementRule placement, Xoroshiro128Plus random) {spawnCard=card;placementRule=placement;rng=random;}
    }
    public class DirectorCore : MonoBehaviour
    {
        public static DirectorCore instance;
        public int Attempts;
        public Func<DirectorSpawnRequest,GameObject> Factory;
        public GameObject TrySpawnObject(DirectorSpawnRequest request) { Attempts++; return Factory?.Invoke(request); }
    }
    public partial class SceneDirector
    {
        public int interactableCredit { get; private set; }
        public int monsterCredit = 100;
        public SceneDirector(int credits=100) {interactableCredit=credits;}
        public static event Action<SceneDirector> onPrePopulateSceneServer;
        public static event Action<SceneDirector,GameObject,DirectorCard> onSpawnInteractableServer;
        public static int PreSubscribers => onPrePopulateSceneServer?.GetInvocationList().Length ?? 0;
        public static int SpawnSubscribers => onSpawnInteractableServer?.GetInvocationList().Length ?? 0;
        public void Before() => onPrePopulateSceneServer?.Invoke(this);
        public void Spawn(GameObject obj,DirectorCard card) => onSpawnInteractableServer?.Invoke(this,obj,card);
        public void After() => onPostPopulateSceneServer?.Invoke(this);
    }
    public partial class Run
    {
        public Xoroshiro128Plus spawnRng = new Xoroshiro128Plus();
        public readonly Dictionary<ItemTier,List<PickupIndex>> allAvailableTierDroplists = new Dictionary<ItemTier,List<PickupIndex>>();
        public readonly HashSet<ItemIndex> Unavailable = new HashSet<ItemIndex>();
        public static event Action<Run> onRunDestroyGlobal,onAvailablePickupsModified;
        public static int DestroySubscribers => onRunDestroyGlobal?.GetInvocationList().Length ?? 0;
        public bool IsItemAvailable(ItemIndex index) => !Unavailable.Contains(index);
        public int GetDifficultyScaledCost(int cost) => cost*2;
        public void End() => onRunDestroyGlobal?.Invoke(this);
        public void PublishPools() => onAvailablePickupsModified?.Invoke(this);
    }
    public class CharacterMaster : MonoBehaviour { public float luck { get; private set; } }
    public class EntityStateMachine : MonoBehaviour {}
    public class SetStateOnHurt : MonoBehaviour
    {
        public bool canBeStunned;
        public EntityStateMachine targetStateMachine = new EntityStateMachine();
        public int Stuns; public float StunnedUntil; public bool Throw;
        public void SetStun(float duration)
        {
            if (!canBeStunned) return;
            if (Throw) throw new InvalidOperationException("Test enemy failure");
            Stuns++; StunnedUntil = Time.unscaledTime+duration;
        }
    }
}
