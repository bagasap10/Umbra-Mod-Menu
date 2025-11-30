using UnityEngine;
using RoR2;
using System.Collections.Generic;
using System.Linq;


namespace UmbraMenu.Menus
{
    public class Teleporter : Menu
    {
        private static readonly IMenu teleporter = new NormalMenu(5, new Rect(10, 425, 20, 20), "TELEPORTER MENU");
        private static int mountainStacks = 0;

        public Button skipStage;
        public Button instaTele;
        public Button addMountain;
        public Button spawnAll;
        public Button spawnBlue;
        public Button spawnCele;
        public Button spawnGold;
        public Button spawnHidden;

        public Button spawnArtifact;
        public Button spawnVoid;
        public Button spawnDeepVoid;
        public Button spawnNull;
        public Button spawnInfinite;
        public Button spawnGreen;
        public Button spawnDestination;
        public Button spawnVirtual;
        public Button spawnEncrypted;
        public Button spawnDecrypted;
        public Button spawnMainline;


        public Teleporter() : base(teleporter)
        {
            if (UmbraMenu.characterCollected)
            {
                void SpawnAllPortals() => SpawnPortals("all");
                void SpawnBluePortal() => SpawnPortals("blue");
                void SpawnCelestalPortal() => SpawnPortals("cele");
                void SpawnGoldPortal() => SpawnPortals("gold");
                void SpawnHiddenPortal() => SpawnPortals("hidden");


                skipStage = new Button(new NormalButton(this, 1, "SKIP STAGE", SkipStage));
                instaTele = new Button(new NormalButton(this, 2, "INSTA TELE/PURPLE CHARGE", InstaTeleporter));
                addMountain = new Button(new NormalButton(this, 3, $"ADD MOUNTAIN-COUNT : {mountainStacks}", AddMountain));
                spawnAll = new Button(new NormalButton(this, 4, "SPAWN ALL PORTALS", SpawnAllPortals));
                spawnBlue = new Button(new NormalButton(this, 5, "SPAWN BLUE PORTAL", SpawnBluePortal));
                spawnCele = new Button(new NormalButton(this, 6, "SPAWN CELESTAL PORTAL", SpawnCelestalPortal));
                spawnGold = new Button(new NormalButton(this, 7, "SPAWN GOLD PORTAL", SpawnGoldPortal));
                spawnHidden = new Button(new NormalButton(this, 8, "SPAWN HIDDEN REALM PORTAL", SpawnHiddenPortal));
                spawnArtifact = new Button(new NormalButton(this, 9, "SPAWN ARTIFACT PORTAL", () => SpawnPortals("artifact")));
                spawnVoid = new Button(new NormalButton(this, 10, "SPAWN VOID PORTAL", () => SpawnPortals("void")));
                spawnDeepVoid = new Button(new NormalButton(this, 11, "SPAWN DEEP VOID PORTAL", () => SpawnPortals("deepvoid")));
                spawnNull = new Button(new NormalButton(this, 12, "SPAWN NULL PORTAL", () => SpawnPortals("null")));
                spawnInfinite = new Button(new NormalButton(this, 13, "SPAWN INFINITE PORTAL", () => SpawnPortals("infinite")));
                spawnGreen = new Button(new NormalButton(this, 14, "SPAWN GREEN PORTAL", () => SpawnPortals("green")));
                spawnDestination = new Button(new NormalButton(this, 15, "SPAWN DESTINATION PORTAL", () => SpawnPortals("destination")));
                spawnVirtual = new Button(new NormalButton(this, 16, "SPAWN VIRTUAL PORTAL", () => SpawnPortals("virtual")));
                spawnEncrypted = new Button(new NormalButton(this, 17, "SPAWN ENCRYPTED PORTAL", () => SpawnPortals("encrypted")));
                spawnDecrypted = new Button(new NormalButton(this, 18, "SPAWN DECRYPTED PORTAL", () => SpawnPortals("decrypted")));
                spawnMainline = new Button(new NormalButton(this, 19, "SPAWN MAINLINE PORTAL", () => SpawnPortals("mainline")));

                AddButtons(new List<Button>()
                {
                    skipStage,
                    instaTele,
                    addMountain,
                    spawnAll,
                    spawnBlue,
                    spawnCele,
                    spawnGold,
                    spawnHidden,
                    spawnArtifact,
                    spawnVoid,
                    spawnDeepVoid,
                    spawnNull,
                    spawnInfinite,
                    spawnGreen,
                    spawnDestination,
                    spawnVirtual,
                    spawnEncrypted,
                    spawnDecrypted,
                    spawnMainline
                });
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
                var purchaseInteractions = UnityEngine.Object.FindObjectsOfType<PurchaseInteraction>().ToList();
                foreach (PurchaseInteraction purchaseInteraction in purchaseInteractions)
                {
                    var holdoutZone = purchaseInteraction?.gameObject.GetComponent<HoldoutZoneController>();
                    holdoutZone.baseChargeDuration = 1;
                }
            }
        }

        public void SkipStage()
        {
            Run.instance.AdvanceStage(Run.instance.nextStageScene);
        }

        public void AddMountain()
        {
            TeleporterInteraction.instance.AddShrineStack();
            mountainStacks = TeleporterInteraction.instance.shrineBonusStacks;
            addMountain.SetText($"ADD MOUNTAIN-COUNT : {mountainStacks}");
        }

        // Helper
        private GameObject SpawnPrefab(string path, Vector3 spawnPos)
        {
            GameObject prefab = Resources.Load<GameObject>(path);

            if (!prefab)
            {
                Debug.Log("UmbraMenu: Missing prefab → " + path);
                return null;
            }

            return GameObject.Instantiate(prefab, spawnPos, Quaternion.identity);
        }

        public void SpawnPortals(string portal)
        {
            var tele = TeleporterInteraction.instance;
            if (!tele)
            {
                Debug.Log("UmbraMenu: No teleporter in scene.");
                return;
            }

            Vector3 spawnPos = tele.transform.position + new Vector3(3f, 0f, 3f);

            switch (portal)
            {
                case "blue":
                    Debug.Log("UmbraMenu: Spawned Blue (Shop) Portal");
                    tele.shouldAttemptToSpawnShopPortal = true;
                    tele.Network_shouldAttemptToSpawnShopPortal = true;
                    break;

                case "gold":
                    Debug.Log("UmbraMenu: Spawned Gold Portal");
                    tele.shouldAttemptToSpawnGoldshoresPortal = true;
                    tele.Network_shouldAttemptToSpawnGoldshoresPortal = true;
                    break;

                case "cele":
                    Debug.Log("UmbraMenu: Spawned Celestial Portal");
                    tele.shouldAttemptToSpawnMSPortal = true;
                    tele.Network_shouldAttemptToSpawnMSPortal = true;
                    break;

                case "hidden":
                    Debug.Log("UmbraMenu: Spawned Hidden Realms Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalHiddenRealm", spawnPos);
                    break;

                case "artifact":
                    Debug.Log("UmbraMenu: Spawned Artifact Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalArtifactworld", spawnPos);
                    break;

                case "void":
                    Debug.Log("UmbraMenu: Spawned Void Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalVoid", spawnPos);
                    break;

                case "deepvoid":
                    Debug.Log("UmbraMenu: Spawned Deep Void Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalDeepVoid", spawnPos);
                    break;

                case "null":
                    Debug.Log("UmbraMenu: Spawned Null Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalStatue", spawnPos);
                    break;

                case "infinite":
                    Debug.Log("UmbraMenu: Spawned Infinite Tower Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalArena", spawnPos);
                    break;

                case "green":
                    Debug.Log("UmbraMenu: Spawned Green Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalGatewayGreen", spawnPos);
                    break;

                case "virtual":
                    Debug.Log("UmbraMenu: Spawned Virtual Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalVirtual", spawnPos);
                    break;

                case "destination":
                    Debug.Log("UmbraMenu: Spawned Destination Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalDestination", spawnPos);
                    break;

                case "encrypted":
                    Debug.Log("UmbraMenu: Spawned Encrypted Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalEncrypted", spawnPos);
                    break;

                case "decrypted":
                    Debug.Log("UmbraMenu: Spawned Decrypted Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalDecrypted", spawnPos);
                    break;

                case "mainline":
                    Debug.Log("UmbraMenu: Spawned Mainline Portal");
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalMainline", spawnPos);
                    break;

                case "all":
                    Debug.Log("UmbraMenu: Spawned ALL portals");

                    tele.Network_shouldAttemptToSpawnGoldshoresPortal = true;
                    tele.Network_shouldAttemptToSpawnShopPortal = true;
                    tele.Network_shouldAttemptToSpawnMSPortal = true;

                    tele.shouldAttemptToSpawnGoldshoresPortal = true;
                    tele.shouldAttemptToSpawnShopPortal = true;
                    tele.shouldAttemptToSpawnMSPortal = true;

                    // DLC Prefabs
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalArtifactworld", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalVoid", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalDeepVoid", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalStatue", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalArena", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalGatewayGreen", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalVirtual", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalDestination", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalEncrypted", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalDecrypted", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalMainline", spawnPos);
                    SpawnPrefab("Prefabs/NetworkedObjects/PortalHiddenRealm", spawnPos);
                    break;

                default:
                    Debug.Log("UmbraMenu: Unknown portal → " + portal);
                    break;
            }
        }


        //public void SpawnPortals(string portal)
        //{
        //    if (TeleporterInteraction.instance)
        //    {
        //        if (portal.Equals("gold"))
        //        {
        //            Debug.Log("UmbraMenu : Spawned Gold Portal");
        //            TeleporterInteraction.instance.Network_shouldAttemptToSpawnGoldshoresPortal = true;
        //            TeleporterInteraction.instance.shouldAttemptToSpawnGoldshoresPortal = true;
        //        }
        //        else if (portal.Equals("blue"))
        //        {
        //            Debug.Log("UmbraMenu : Spawned Shop Portal");
        //            TeleporterInteraction.instance.Network_shouldAttemptToSpawnShopPortal = true;
        //            TeleporterInteraction.instance.shouldAttemptToSpawnShopPortal = true;
        //        }
        //        else if (portal.Equals("cele"))
        //        {
        //            Debug.Log("UmbraMenu : Spawned Celestal Portal");
        //            TeleporterInteraction.instance.Network_shouldAttemptToSpawnMSPortal = true;
        //            TeleporterInteraction.instance.shouldAttemptToSpawnMSPortal = true;
        //        }
        //        else if (portal.Equals("hidden")) // NEW
        //        {
        //            Debug.Log("UmbraMenu : Spawned Hidden Realms Portal");
        //            TeleporterInteraction.instance.Network_shouldAttemptToSpawnHiddenRealmPortal = true;
        //            TeleporterInteraction.instance.shouldAttemptToSpawnHiddenRealmsPortal = true;
        //        }
        //        else if (portal.Equals("all"))
        //        {
        //            Debug.Log("UmbraMenu : Spawned All Portals");
        //            TeleporterInteraction.instance.Network_shouldAttemptToSpawnGoldshoresPortal = true;
        //            TeleporterInteraction.instance.Network_shouldAttemptToSpawnShopPortal = true;
        //            TeleporterInteraction.instance.Network_shouldAttemptToSpawnMSPortal = true;
        //            TeleporterInteraction.instance.Network_shouldAttemptToSpawnHiddenRealmPortal = true;   // NEW

        //            TeleporterInteraction.instance.shouldAttemptToSpawnGoldshoresPortal = true;
        //            TeleporterInteraction.instance.shouldAttemptToSpawnShopPortal = true;
        //            TeleporterInteraction.instance.shouldAttemptToSpawnMSPortal = true;
        //            TeleporterInteraction.instance.shouldAttemptToSpawnHiddenRealmsPortal = true;
        //        }
        //    }
        //}
    }
}
