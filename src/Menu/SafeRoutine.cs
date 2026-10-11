using System;
using UnityEngine;

namespace UmbraMenu
{
    // Created once per component; no per-frame closures, exception floods or automatic repeated failing calls.
    internal sealed class SafeRoutine
    {
        private readonly string name;
        private readonly Action action;
        private int failures;
        private float retryAt;
        public SafeRoutine(string name, Action action) { this.name = name; this.action = action; }
        public void Run()
        {
            if (failures >= 3 || Time.unscaledTime < retryAt) return;
            try { action(); failures = 0; }
            catch (Exception error) when (!(error is ExitGUIException))
            {
                failures++;
                retryAt = Time.unscaledTime + 2f;
                Debug.LogWarning("Umbra: " + name + " failed (" + failures + "/3): " + error);
                if (failures == 3) Loader.Report("degraded", name + " was paused after repeated errors. Reload the menu or restart the game.");
            }
        }
        public void Reset() { failures = 0; retryAt = 0; }
    }
}
