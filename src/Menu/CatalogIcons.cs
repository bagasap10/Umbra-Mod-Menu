using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UmbraMenu
{
    internal static class CatalogIcons
    {
        internal const int MaximumLoads = 128;
        private sealed class Entry
        {
            public AsyncOperationHandle<Sprite> Handle;
            public long Used;
        }
        private static readonly Dictionary<object, Entry> loads = new Dictionary<object, Entry>();
        private static long clock;
        public static Sprite Get(ItemDef def) => def == null ? null : Get(def.GetIcon(), def.GetIconReference());
        public static Sprite Get(EquipmentDef def) => def == null ? null : Get(def.GetIcon(), def.GetIconReference());

        private static Sprite Get(Sprite legacyIcon, AssetReferenceT<Sprite> reference)
        {
            if (legacyIcon) return legacyIcon;
            if (reference == null || !reference.RuntimeKeyIsValid()) return null;
            object key = reference.RuntimeKey;
            if (!loads.TryGetValue(key, out var entry))
            {
                if (loads.Count >= MaximumLoads && !EvictOldestCompleted()) return null;
                entry = new Entry { Handle = Addressables.LoadAssetAsync<Sprite>(key) };
                loads.Add(key, entry);
            }
            entry.Used = ++clock;
            var handle = entry.Handle;
            return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
        }

        private static bool EvictOldestCompleted()
        {
            object oldest = null;
            long used = long.MaxValue;
            foreach (var pair in loads)
            {
                var handle = pair.Value.Handle;
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.None) continue;
                if (pair.Value.Used < used) { used = pair.Value.Used; oldest = pair.Key; }
            }
            if (oldest == null) return false;
            var evicted = loads[oldest].Handle;
            loads.Remove(oldest);
            if (evicted.IsValid()) Addressables.Release(evicted);
            return true;
        }

        public static void Release()
        {
            foreach (var entry in loads.Values)
            {
                try { if (entry.Handle.IsValid()) Addressables.Release(entry.Handle); }
                catch (System.Exception error) { Debug.LogWarning("Umbra: icon cleanup failed: " + error.Message); }
            }
            loads.Clear(); clock = 0;
        }
    }
}
