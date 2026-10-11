using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UmbraMenu
{
    internal static class PortalCatalog
    {
        internal sealed class Definition
        {
            public readonly string Key, Name, Address;
            public readonly int Expansion;

            public Definition(string key, string name, int expansion, string address)
            {
                Key = key;
                Name = name;
                Expansion = expansion;
                Address = address;
            }
        }

        public static readonly Definition[] Portals =
        {
            new Definition("blue", "BLUE PORTAL", 0, "RoR2/Base/PortalShop/PortalShop.prefab"),
            new Definition("cele", "CELESTIAL PORTAL", 0, "RoR2/Base/PortalMS/PortalMS.prefab"),
            new Definition("gold", "GOLD PORTAL", 0, "RoR2/Base/PortalGoldshores/PortalGoldshores.prefab"),
            new Definition("artifact", "ARTIFACT PORTAL", 0, "RoR2/Base/PortalArtifactworld/PortalArtifactworld.prefab"),
            new Definition("null", "NULL PORTAL", 0, "RoR2/Base/PortalArena/PortalArena.prefab"),
            new Definition("void", "VOID PORTAL", 1, "RoR2/DLC1/PortalVoid/PortalVoid.prefab"),
            new Definition("deepvoid", "DEEP VOID PORTAL", 1, "RoR2/DLC1/DeepVoidPortal/DeepVoidPortal.prefab"),
            new Definition("infinite", "INFINITE TOWER PORTAL", 1, "RoR2/DLC1/GameModes/InfiniteTowerRun/PortalInfiniteTower.prefab"),
            new Definition("voidoutro", "VOID OUTRO PORTAL", 1, "RoR2/DLC1/VoidOutroPortal/VoidOutroPortal.prefab"),
            new Definition("green", "GREEN PORTAL", 2, "RoR2/DLC2/PortalColossus.prefab"),
            new Definition("destination", "DESTINATION PORTAL", 2, "RoR2/DLC2/PM DestinationPortal.prefab"),
            new Definition("virtual", "VIRTUAL PORTAL", 3, "RoR2/DLC3/CompExchangePortal.prefab"),
            new Definition("encrypted", "ENCRYPTED PORTAL", 3, "RoR2/DLC3/HardwareProgPortal.prefab"),
            new Definition("decrypted", "DECRYPTED PORTAL", 3, "RoR2/DLC3/SolusWebPortal.prefab"),
            new Definition("mainline", "MAINLINE PORTAL", 3, "RoR2/DLC3/EyePortal/EyePortal.prefab"),
            new Definition("solutional", "SOLUTIONAL PORTAL", 3, "RoR2/DLC3/SolutionalHauntPortal.prefab"),
            new Definition("anima", "ANIMA PORTAL", 4, "RoR2/DLC4/TrialPortalEntry.prefab"),
            new Definition("animaexit", "ANIMA EXIT / LEAVE WINNOWING", 4, "RoR2/DLC4/TrialPortalExit.prefab"),
            new Definition("ritual", "RITUAL PORTAL", 4, "RoR2/DLC4/ChampRushPortal.prefab"),
            new Definition("mortal", "MORTAL PORTAL", 4, "RoR2/DLC4/ChampRushExitPortal.prefab"),
            new Definition("veil", "VEIL OF MEMORY", 4, "RoR2/DLC4/champrush/PortalToUltima.prefab")
        };

        private static readonly string[] GroupTitles =
        {
            "Normal / Not-DLC",
            "DLC#1 / Survivors of the Void",
            "DLC#2 / Seekers of the Storm",
            "DLC#3 / Alloyed Collective",
            "DLC#4 / Hallowed Concepts"
        };

        public static Definition Find(string key)
        {
            return Portals.FirstOrDefault(portal => string.Equals(portal.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        public static void AddButtons(Menu menu, List<Button> buttons, Action<string> spawn)
        {
            foreach (var group in Portals.GroupBy(portal => portal.Expansion).OrderBy(group => group.Key))
            {
                buttons.Add(new Button(new DividerButton(menu, buttons.Count + 1, GroupTitles[group.Key], new Color32(190, 190, 190, 255), true)));
                foreach (Definition portal in group)
                    buttons.Add(new Button(new PickupButton(menu, buttons.Count + 1, "SPAWN " + portal.Name, () => spawn(portal.Key), () => PortalIcons.Get(portal), "?")));
            }
        }
    }
}
