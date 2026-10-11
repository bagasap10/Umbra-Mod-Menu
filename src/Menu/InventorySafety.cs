using System;

namespace UmbraMenu
{
    internal static class InventorySafety
    {
        // Bound work from one click; rolling invokes game callbacks once per item.
        public const int MaximumBulkQuantity = 1000, MaximumRollCount = 500;
        public static int BulkQuantity(int value) => Math.Max(1, Math.Min(MaximumBulkQuantity, value));
        public static int RollCount(int value) => Math.Max(5, Math.Min(MaximumRollCount, value));
        public static bool CanAdd(int current, int quantity) => current >= 0 && quantity > 0 && current <= int.MaxValue - quantity;
    }
}
