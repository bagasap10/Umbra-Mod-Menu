using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    internal static class EspTargets
    {
        internal sealed class Interactable
        {
            public MonoBehaviour Component;
            public IInteractable Interaction;
            public IDisplayNameProvider NameProvider;
        }

        public static List<CharacterBody> GetEnemies(CharacterBody localBody)
        {
            List<CharacterBody> enemies = new List<CharacterBody>();
            FillEnemies(localBody, enemies);
            return enemies;
        }

        public static void FillEnemies(CharacterBody localBody, List<CharacterBody> enemies)
        {
            enemies.Clear();
            if (!localBody || !localBody.teamComponent)
                return;

            TeamMask enemyTeams = TeamMask.allButNeutral;
            enemyTeams.RemoveTeam(localBody.teamComponent.teamIndex);
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (body && body != localBody && body.teamComponent && body.healthComponent && body.healthComponent.alive
                    && enemyTeams.HasTeam(body.teamComponent.teamIndex))
                    enemies.Add(body);
            }
        }

        public static Bounds GetBodyBounds(CharacterBody body)
        {
            Bounds bounds = new Bounds();
            bool foundCollider = false;
            if (body.hurtBoxGroup && body.hurtBoxGroup.hurtBoxes != null)
            {
                foreach (HurtBox hurtBox in body.hurtBoxGroup.hurtBoxes)
                {
                    if (!hurtBox || !hurtBox.collider || !hurtBox.collider.enabled || hurtBox.healthComponent != body.healthComponent)
                        continue;

                    if (!foundCollider)
                        bounds = hurtBox.collider.bounds;
                    else
                        bounds.Encapsulate(hurtBox.collider.bounds);
                    foundCollider = true;
                }
            }
            if (!foundCollider && body.mainHurtBox && body.mainHurtBox.collider)
            {
                bounds = body.mainHurtBox.collider.bounds;
                foundCollider = true;
            }
            if (!foundCollider)
            {
                float diameter = Mathf.Max(0.5f, body.radius * 2f);
                bounds = new Bounds(body.corePosition, Vector3.one * diameter);
            }
            return bounds;
        }

        public static bool TryGetScreenRect(Camera camera, Bounds bounds, out Rect rect)
        {
            rect = new Rect();
            if (!camera)
                return false;

            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 worldPoint = center + new Vector3((corner & 1) == 0 ? -extents.x : extents.x,
                    (corner & 2) == 0 ? -extents.y : extents.y, (corner & 4) == 0 ? -extents.z : extents.z);
                Vector3 screenPoint = camera.WorldToScreenPoint(worldPoint);
                // Bounds crossing the camera plane produce enormous or inverted rectangles.
                if (screenPoint.z <= camera.nearClipPlane)
                    return false;
                minX = Mathf.Min(minX, screenPoint.x);
                maxX = Mathf.Max(maxX, screenPoint.x);
                minY = Mathf.Min(minY, Screen.height - screenPoint.y);
                maxY = Mathf.Max(maxY, Screen.height - screenPoint.y);
            }
            if (maxX < 0 || minX > Screen.width || maxY < 0 || minY > Screen.height)
                return false;

            rect = Rect.MinMaxRect(Mathf.Max(0, minX), Mathf.Max(0, minY), Mathf.Min(Screen.width, maxX), Mathf.Min(Screen.height, maxY));
            return rect.width > 0 && rect.height > 0;
        }

        public static List<Interactable> GetAdditionalInteractables(IEnumerable<MonoBehaviour> components)
        {
            List<Interactable> interactables = new List<Interactable>();
            HashSet<MonoBehaviour> seenComponents = new HashSet<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (!component || !component.isActiveAndEnabled || !(component is IInteractable interaction)
                    || component is GenericPickupController || component is TeleporterInteraction
                    || component is PurchaseInteraction || component.GetComponent<BarrelInteraction>()
                    || component.GetComponent<ScrapperController>() || component.GetComponent<PressurePlateController>())
                    continue;

                if (!seenComponents.Add(component))
                    continue;
                interactables.Add(new Interactable
                {
                    Component = component,
                    Interaction = interaction,
                    NameProvider = component.GetComponents<MonoBehaviour>().OfType<IDisplayNameProvider>().FirstOrDefault()
                });
            }
            return interactables;
        }
    }
}
