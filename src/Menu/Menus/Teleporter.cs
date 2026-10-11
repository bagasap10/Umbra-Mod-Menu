using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UmbraMenu.Menus
{
    public class Teleporter : Menu
    {
        private static readonly ListMenu teleporter = new ListMenu(5, new Rect(10, 425, 20, 20), "TELEPORTER / PORTALS");
        private static readonly Dictionary<string, AsyncOperationHandle<GameObject>> portalAssets = new Dictionary<string, AsyncOperationHandle<GameObject>>();
        private static readonly Dictionary<string, float> nextAllowedSpawn = new Dictionary<string, float>();
        private static int generation;

        internal static void ReleaseAssets()
        {
            generation++;
            foreach (var handle in portalAssets.Values) if (handle.IsValid()) Addressables.Release(handle);
            portalAssets.Clear();
            nextAllowedSpawn.Clear();
        }
        private static int mountainStacks;

        public Button skipStage;
        public Button instaTele;
        public Button addMountain;
        public Button spawnAll;

        public Teleporter() : base(teleporter)
        {
            if (UmbraMenu.characterCollected)
            {
                skipStage = new Button(new NormalButton(this, 1, "SKIP STAGE", SkipStage));
                instaTele = new Button(new NormalButton(this, 2, "INSTA TELE/PURPLE CHARGE", InstaTeleporter));
                addMountain = new Button(new NormalButton(this, 3, $"ADD MOUNTAIN-COUNT : {mountainStacks}", AddMountain));
                spawnAll = new Button(new NormalButton(this, 4, "SPAWN ALL PORTALS", () => SpawnPortals("all")));
                List<Button> buttons = new List<Button> { skipStage, instaTele, addMountain, spawnAll };
                PortalCatalog.AddButtons(this, buttons, SpawnPortals);
                AddButtons(buttons);
                SetActivatingButton(Utility.FindButtonById(0, 5));
                SetPrevMenuId(0);
            }
        }

        public override void Draw()
        {
            if (IsEnabled())
            {
                SetWindow();
                base.Draw();
            }
        }

        public override void Reset()
        {
            mountainStacks = 0;
            base.Reset();
        }

        public void InstaTeleporter()
        {
            if (TeleporterInteraction.instance)
            {
                TeleporterInteraction.instance.holdoutZoneController.baseChargeDuration = 1;
            }
            else
            {
                foreach (PurchaseInteraction interaction in UnityEngine.Object.FindObjectsOfType<PurchaseInteraction>())
                {
                    HoldoutZoneController zone = interaction.GetComponent<HoldoutZoneController>();
                    if (zone)
                        zone.baseChargeDuration = 1;
                }
            }
        }

        public void SkipStage()
        {
            Run.instance.AdvanceStage(Run.instance.nextStageScene);
        }

        public void AddMountain()
        {
            if (!TeleporterInteraction.instance)
                return;
            TeleporterInteraction.instance.AddShrineStack();
            mountainStacks = TeleporterInteraction.instance.shrineBonusStacks;
            addMountain.SetText($"ADD MOUNTAIN-COUNT : {mountainStacks}");
        }

        public void SpawnPortals(string portal)
        {
            if (!UmbraMenu.InGameCheck() || !NetworkServer.active)
            {
                Chat.AddMessage("UmbraMenu: Portal spawning requires the host of an active run.");
                return;
            }
            if (string.Equals(portal, "all", StringComparison.OrdinalIgnoreCase))
            {
                int position = 0;
                foreach (PortalCatalog.Definition definition in PortalCatalog.Portals)
                    SpawnPortal(definition, position++);
                return;
            }
            PortalCatalog.Definition selected = PortalCatalog.Find(portal);
            if (selected == null)
            {
                Debug.LogWarning("UmbraMenu: Unknown portal: " + portal);
                return;
            }
            SpawnPortal(selected, 0);
        }

        private void SpawnPortal(PortalCatalog.Definition portal, int position)
        {
            if (nextAllowedSpawn.TryGetValue(portal.Address, out float allowed) && Time.unscaledTime < allowed) return;
            nextAllowedSpawn[portal.Address] = Time.unscaledTime + 1f;
            int requestedGeneration = generation;
            TeleporterInteraction tele = TeleporterInteraction.instance;
            // Preserve the standard orbs that open their portals after the teleporter charges.
            if (tele)
            {
                switch (portal.Key)
                {
                    case "blue":
                        tele.shouldAttemptToSpawnShopPortal = true;
                        tele.Network_shouldAttemptToSpawnShopPortal = true;
                        return;
                    case "gold":
                        tele.shouldAttemptToSpawnGoldshoresPortal = true;
                        tele.Network_shouldAttemptToSpawnGoldshoresPortal = true;
                        return;
                    case "cele":
                        tele.shouldAttemptToSpawnMSPortal = true;
                        tele.Network_shouldAttemptToSpawnMSPortal = true;
                        return;
                }
            }

            Transform anchor = tele ? tele.transform : UmbraMenu.LocalPlayerBody ? UmbraMenu.LocalPlayerBody.transform : null;
            if (!anchor)
            {
                Chat.AddMessage("UmbraMenu: No teleporter or player is available to place a portal.");
                return;
            }
            Run requestedRun = Run.instance;
            Vector3 spawnPosition = anchor.position + new Vector3(3 + 4 * (position % 5), 0, 3 + 4 * (position / 5));
            // Keep one completion callback per pending prefab, even if the user clicks again after the debounce.
            if (portalAssets.TryGetValue(portal.Address, out var pending) && !pending.IsDone) return;
            if (!portalAssets.TryGetValue(portal.Address, out AsyncOperationHandle<GameObject> operation))
            {
                operation = Addressables.LoadAssetAsync<GameObject>(portal.Address);
                portalAssets.Add(portal.Address, operation);
            }

            void SpawnLoaded(AsyncOperationHandle<GameObject> result)
            {
                try { SpawnLoadedCore(result); }
                catch (System.Exception error) { Debug.LogWarning("Umbra: portal spawning failed: " + error); Loader.Report("degraded", "Portal spawning failed. See Player.log."); }
            }

            void SpawnLoadedCore(AsyncOperationHandle<GameObject> result)
            {
                if (requestedGeneration != generation) return;
                if (result.Status != AsyncOperationStatus.Succeeded || !result.Result)
                {
                    Debug.LogWarning("UmbraMenu: Failed to load portal: " + portal.Address);
                    Chat.AddMessage("UmbraMenu: Could not load " + portal.Name + ".");
                    if (portalAssets.Remove(portal.Address))
                        Addressables.Release(result);
                    return;
                }
                // An asynchronous load must not place a stale request into another stage or run.
                if (!anchor || Run.instance != requestedRun || !NetworkServer.active || !UmbraMenu.InGameCheck())
                    return;
                GameObject instance = UnityEngine.Object.Instantiate(result.Result, spawnPosition, Quaternion.identity);
                if (instance.GetComponent<NetworkIdentity>())
                    NetworkServer.Spawn(instance);
                Debug.Log("UmbraMenu: Spawned " + portal.Name);
            }

            if (operation.IsDone)
                SpawnLoaded(operation);
            else
                operation.Completed += SpawnLoaded;
        }
    }
}
