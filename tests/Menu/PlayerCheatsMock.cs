using System;
using RoR2;
using UnityEngine;
namespace RoR2
{
    public class CharacterMotor : MonoBehaviour { public int jumpCount; }
    public struct DamageTypeCombo { }
    public class DamageReport { public CharacterMaster attackerMaster; public CharacterBody victimBody; public TeamIndex victimTeamIndex; public float damageDealt; }
    public static class GlobalEventManager
    {
        public static event Action<DamageReport> onServerDamageDealt;
        public static int Subscribers => onServerDamageDealt?.GetInvocationList().Length ?? 0;
        public static void Damage(DamageReport report) => onServerDamageDealt?.Invoke(report);
    }
    public partial class CharacterBody
    {
        [Flags] public enum BodyFlags { None=0, IgnoreFallDamage=1, Other=2 }
        public BodyFlags bodyFlags;
        public CharacterMotor characterMotor = new CharacterMotor();
        public float maxHealth=100, NativeSpeed=7, NativeAttack=1, NativeCrit=5, NativeShield;
        public int NativeJumps=1, Recalculations;
        public bool ThrowRecalculation;
        public float moveSpeed { get; private set; }
        public float attackSpeed { get; private set; }
        public float crit { get; private set; }
        public float maxShield { get; private set; }
        public int maxJumpCount { get; private set; }
        public event Action<CharacterBody> onRecalculateStats;
        public int StatSubscribers => onRecalculateStats?.GetInvocationList().Length ?? 0;
        public void RecalculateStats()
        {
            if (ThrowRecalculation) throw new InvalidOperationException("Test stat failure");
            Recalculations++; moveSpeed=NativeSpeed; attackSpeed=NativeAttack; crit=NativeCrit; maxShield=NativeShield; maxJumpCount=NativeJumps;
            onRecalculateStats?.Invoke(this);
        }
    }
    public partial class CharacterMaster
    {
        private uint balance; public uint money { get => balance; set { balance=value; OnMoneyValueChanged?.Invoke(this); } } public int Grants;
        public event Action<CharacterMaster> OnMoneyValueChanged;
        public int MoneySubscribers => OnMoneyValueChanged?.GetInvocationList().Length ?? 0;
        public void PublishGold(float amount) => OnGoldCollected?.Invoke(amount);
        public event Action<float> OnGoldCollected;
        public int GoldSubscribers => OnGoldCollected?.GetInvocationList().Length ?? 0;
        public void GiveMoney(uint amount,bool skip=false) { Grants++; money=unchecked(money+amount); OnGoldCollected?.Invoke(amount); }
    }
    public partial class HealthComponent
    {
        public CharacterBody owner;
        public float shield;
        public float fullShield => owner ? owner.maxShield : 0;
        public float Networkshield { get => shield; set => shield=value; }
        public int Recharges, Suicides;
        public bool ThrowRecharge;
        public Action AfterSuicide;
        public void RechargeShieldFull() { if(ThrowRecharge) throw new InvalidOperationException("Test shield failure"); Recharges++;shield=fullShield; }
        public void Suicide(GameObject attacker,GameObject inflictor,DamageTypeCombo type) { Suicides++;alive=false;AfterSuicide?.Invoke(); }
    }
}
