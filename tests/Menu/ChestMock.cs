using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace RoR2
{
    public struct PickupIndex
    {
        public int value;
        public static readonly PickupIndex none = new PickupIndex(-1);
        public PickupIndex(int value) { this.value = value; }
        public static bool operator ==(PickupIndex a, PickupIndex b) => a.value == b.value;
        public static bool operator !=(PickupIndex a, PickupIndex b) => a.value != b.value;
        public override bool Equals(object obj) => obj is PickupIndex other && this == other;
        public override int GetHashCode() => value;
    }
    public struct UniquePickup
    {
        public PickupIndex pickupIndex;
        public UniquePickup(PickupIndex index) { pickupIndex = index; }
    }
    public class PickupDef
    {
        public string nameToken;
        public Color32 baseColor = new Color32(255, 255, 255, 255);
        public ItemIndex itemIndex = ItemIndex.None;
        public EquipmentIndex equipmentIndex = EquipmentIndex.None;
    }
    public static class PickupCatalog
    {
        public static readonly Dictionary<PickupIndex, PickupDef> Defs = new Dictionary<PickupIndex, PickupDef>();
        public static PickupIndex FindPickupIndex(ItemIndex index)
        {
            if (ItemCatalog.GetItemDef(index) == null) return PickupIndex.none;
            var pickup = new PickupIndex((int)index);
            Defs[pickup] = new PickupDef { itemIndex = index, nameToken = ItemCatalog.GetItemDef(index).nameToken };
            return pickup;
        }
        public static PickupIndex FindPickupIndex(EquipmentIndex index)
        {
            if (EquipmentCatalog.GetEquipmentDef(index) == null) return PickupIndex.none;
            var pickup = new PickupIndex(1000000 + (int)index);
            Defs[pickup] = new PickupDef { equipmentIndex = index, nameToken = EquipmentCatalog.GetEquipmentDef(index).nameToken };
            return pickup;
        }
        public static PickupDef GetPickupDef(PickupIndex index) => Defs.TryGetValue(index, out var def) ? def : null;
    }
    public partial class ChestBehavior : MonoBehaviour
    {
        // Read-only auto-property, as in the game: tests exercise the production reflection helper.
        public UniquePickup currentPickup { get; } = new UniquePickup(PickupIndex.none);
        public ChestBehavior(PickupIndex index) { currentPickup = new UniquePickup(index); }
    }
    public partial class SceneDirector : MonoBehaviour
    {
        public static event Action<SceneDirector> onPostPopulateSceneServer;
        public static int Subscribers => onPostPopulateSceneServer?.GetInvocationList().Length ?? 0;
        public static void Populate() => onPostPopulateSceneServer?.Invoke(new SceneDirector());
    }
}
namespace UmbraMenu
{
    public static class ESPHelper { public static void DrawBox(float x, float y, float width, float height, Color32 color) {} }
}
namespace UmbraMenu.Menus
{
    public static class Render { public static bool renderInteractables; }
}
