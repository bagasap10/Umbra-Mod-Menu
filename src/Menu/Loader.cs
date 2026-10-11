using System;
using System.Reflection;
using System.Threading;
using RoR2;
using UnityEngine;
using UmbraSuite;

namespace UmbraMenu
{
    public static class Loader
    {
        public static GameObject gameObject;
        public static bool updateAvailable, devBuild, upToDate = true;
        private static int pendingLoad, pendingUnload;
        private static DateTime requestedAt;
        internal static string ModuleId => Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToString("D");

        // The injector invokes this on a Mono thread. Unity objects are created on the game's next update.
        public static void Load()
        {
            if (Interlocked.CompareExchange(ref pendingLoad, 1, 0) != 0) return;
            requestedAt = DateTime.UtcNow;
            Report("waiting", "Waiting for game catalogs.");
            RoR2Application.onUpdate += InitializeOnMainThread;
        }

        private static void InitializeOnMainThread()
        {
            bool finished = false;
            bool createdHere = false;
            try
            {
                if (RoR2Application.isLoading || !ItemCatalog.availability.available || !EquipmentCatalog.availability.available
                    || !BodyCatalog.availability.available || !SurvivorCatalog.availability.available || !BuffCatalog.availability.available)
                {
                    if ((DateTime.UtcNow - requestedAt).TotalMinutes < 3) return;
                    throw new TimeoutException("Game catalogs did not finish loading. Restart the game before another attempt.");
                }
                finished = true;
                RoR2Application.onUpdate -= InitializeOnMainThread;
                gameObject = GameObject.Find("Umbra Menu");
                if (gameObject)
                {
                    var existing = gameObject.GetComponent<UmbraMenu>();
                    if (!existing) throw new InvalidOperationException("A different Umbra build is already present. Restart the game.");
                    UmbraMenu.mainMenu.SetEnabled(true);
                    Report("ready", "Menu is already active.", MenuUI.ToggleShortcut);
                    return;
                }
                gameObject = new GameObject("Umbra Menu");
                createdHere = true;
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                gameObject.SetActive(false);
                if (!gameObject.AddComponent<UmbraMenu>()) throw new InvalidOperationException("The menu component could not be initialized. See Player.log.");
                gameObject.SetActive(true);
            }
            catch (Exception error)
            {
                finished = true;
                RoR2Application.onUpdate -= InitializeOnMainThread;
                if (createdHere && gameObject) { gameObject.SetActive(false); UnityEngine.Object.Destroy(gameObject); gameObject = null; }
                Debug.LogError("Umbra: initialization failed: " + error);
                Report("error", error.GetBaseException().Message);
            }
            finally { if (finished) Interlocked.Exchange(ref pendingLoad, 0); }
        }

        public static void Unload()
        {
            if (Interlocked.CompareExchange(ref pendingUnload, 1, 0) != 0) return;
            RoR2Application.onUpdate += UnloadOnMainThread;
        }

        private static void UnloadOnMainThread()
        {
            RoR2Application.onUpdate -= UnloadOnMainThread;
            RoR2Application.onUpdate -= InitializeOnMainThread;
            Interlocked.Exchange(ref pendingLoad, 0);
            try
            {
                try { Utility.SaveSettings(); } catch (Exception error) { Debug.LogWarning("Umbra: settings could not be saved: " + error.Message); }
                if (gameObject) { gameObject.SetActive(false); UnityEngine.Object.Destroy(gameObject); }
                else Report("unloaded", "Menu unloaded.");
                gameObject = null;
            }
            finally { Interlocked.Exchange(ref pendingUnload, 0); }
        }

        internal static void Report(string state, string message, string shortcut = "")
        {
            try { SessionProtocol.WriteStatus(state, ModuleId, message, shortcut); }
            catch (Exception error) { Debug.LogWarning("Umbra: launcher status could not be written: " + error.Message); }
        }
    }
}
