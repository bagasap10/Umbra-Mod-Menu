using System.Collections.Generic;
using UnityEngine;
using RoR2;

namespace UmbraMenu.Menus
{
    public class SpawnList : Menu
    {
        private static readonly IMenu spawnList = new ListMenu(15, new Rect(1503, 10, 20, 20), "SPAWN CARDS MENU");

        public SpawnList() : base(spawnList)
        {
            if (UmbraMenu.characterCollected)
            {
                List<Button> buttons = new List<Button>();
                for (int i = 0; i < UmbraMenu.spawnCards.Count; i++)
                {
                    var spawnCard = UmbraMenu.spawnCards[i];
                    string cardName = spawnCard.ToString();
                    string category = "";
                    string buttonText = "";
                    if (cardName.Contains("MultiCharacterSpawnCard"))
                    {
                        cardName = cardName.Replace(" (RoR2.MultiCharacterSpawnCard)", "");
                        category = "CharacterSpawnCard";
                        buttonText = cardName.Replace("csc", "");
                    }
                    else if (cardName.Contains("CharacterSpawnCard"))
                    {
                        cardName = cardName.Replace(" (RoR2.CharacterSpawnCard)", "");
                        category = "CharacterSpawnCard";
                        buttonText = cardName.Replace("csc", "");
                    }
                    else if (cardName.Contains("InteractableSpawnCard"))
                    {
                        cardName = cardName.Replace(" (RoR2.InteractableSpawnCard)", "");
                        category = "InteractableSpawnCard";
                        buttonText = cardName.Replace("isc", "");
                    }
                    else if (cardName.Contains("BodySpawnCard"))
                    {
                        cardName = cardName.Replace(" (RoR2.BodySpawnCard)", "");
                        category = "BodySpawnCard";
                        buttonText = cardName.Replace("bsc", "");
                    }
                    string path = $"SpawnCards/{category}/{cardName}";

                    void ButtonAction() => SpawnSpawnCard(spawnCard);
                    Button button = new Button(new NormalButton(this, i + 1, buttonText, ButtonAction));
                    buttons.Add(button);
                }
                AddButtons(buttons);
                SetActivatingButton(Utility.FindButtonById(4, 4));
                SetPrevMenuId(4);
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

        public override void SetEnabled(bool value)
        {
            if (value != IsEnabled())
            {
                if (value) EnableSpawnList();
                else DisableSpawnList();
            }
            base.SetEnabled(value);
        }

        public override void Reset()
        {
            DisableSpawnList();
            base.Reset();
        }
        private static void SpawnSpawnCard(SpawnCard spawnCard)
        {
            if (!UmbraMenu.characterCollected || !UmbraMenu.LocalPlayerBody || !spawnCard || !DirectorCore.instance || !UnityEngine.Networking.NetworkServer.active)
            {
                Chat.AddMessage("Umbra: spawning requires the host of an active run with a living player.");
                return;
            }
            bool tracked = spawnCard is InteractableSpawnCard;
            Spawn.spawnedObjects.RemoveAll(instance => !instance);
            if (tracked && Spawn.spawnedObjects.Count >= 256)
            {
                Chat.AddMessage("Umbra: 256 spawned interactables are already tracked. Clear them before spawning more.");
                return;
            }
            var request = new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                minDistance = Spawn.minDistance, maxDistance = Spawn.maxDistance,
                position = UmbraMenu.LocalPlayerBody.footPosition
            }, RoR2Application.rng)
            {
                ignoreTeamMemberLimit = true,
                teamIndexOverride = Spawn.team[Spawn.teamIndex]
            };
            bool originalNetworkFlag = spawnCard.sendOverNetwork;
            GameObject spawned;
            try
            {
                spawnCard.sendOverNetwork = true;
                spawned = DirectorCore.instance.TrySpawnObject(request);
            }
            finally { spawnCard.sendOverNetwork = originalNetworkFlag; }
            if (!spawned) { Chat.AddMessage("Umbra: no valid location was found for that spawn."); return; }
            if (tracked) Spawn.spawnedObjects.Add(spawned);
            Chat.AddMessage("<color=yellow>Spawned " + spawnCard.name + "</color>");
        }
        public static bool onSpawnListEnable = true;
        public static void EnableSpawnList()
        {
            if (onSpawnListEnable)
            {
                DumpInteractables(null);
                SceneDirector.onPostPopulateSceneServer += DumpInteractables;
                onSpawnListEnable = false;
            }
            else
            {
                return;
            }
        }

        public static void DisableSpawnList()
        {
            if (!onSpawnListEnable)
            {
                SceneDirector.onPostPopulateSceneServer -= DumpInteractables;
                onSpawnListEnable = true;
            }
            else
            {
                return;
            }
        }

        private static void DumpInteractables(SceneDirector obj)
        {
            try { UmbraMenu.spawnCards = Utility.GetSpawnCards(); }
            catch (System.Exception error) { Debug.LogWarning("Umbra: spawn catalog refresh failed: " + error); Loader.Report("degraded", "Spawn catalog refresh failed. See Player.log."); }
        }
    }
}
