using GameNetcodeStuff;
using HarmonyLib;
using MoreCounterplay.Behaviours;
using Unity.Netcode;
using UnityEngine;

namespace MoreCounterplay.Patches
{
    [HarmonyPatch]
    internal class JesterPatch
    {
        [HarmonyPatch(typeof(JesterAI), nameof(JesterAI.Start))]
        [HarmonyPostfix]
        public static void OnSpawn(JesterAI __instance)
        {
            if ((!__instance.IsServer && !__instance.IsHost) || !MoreCounterplay.Settings.EnableJesterCounterplay)
                return;

            if (JesterSurface.JesterSurfacePrefab == null)
            {
                MoreCounterplay.LogWarning("Jester surface prefab did not load correctly or is missing; its counterplay will not work.");
                return;
            }

            // Create Jester surface prefab instance.
            GameObject jesterSurfaceContainer = Object.Instantiate(JesterSurface.JesterSurfacePrefab);
            jesterSurfaceContainer.name = JesterSurface.JesterSurfacePrefab.name;

            if (!jesterSurfaceContainer.TryGetComponent(out NetworkObject networkSurface))
            {
                return;
            }

            // Spawn Jester surface NetworkObject.
            networkSurface.Spawn();

            // Set Jester surface instance as a child of its respective Jester.
            jesterSurfaceContainer.transform.SetParent(__instance.transform, false);
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourState))]
        [HarmonyPrefix]
        public static void CheckJesterHead(EnemyAI __instance, ref int stateIndex)
        {
            if ((!__instance.IsServer && !__instance.IsHost) || !MoreCounterplay.Settings.EnableJesterCounterplay)
                return;
            if (__instance.GetType() != typeof(JesterAI))
                return;

            // Find and obtain JesterSurface component.
            if (__instance.transform.Find("JesterSurface")?.TryGetComponent(out JesterSurface jesterSurface) != true)
                return;

            // Check which behaviour state the Jester is about to switch to.
            switch (stateIndex)
            {
                case 0:
                case 1:
                    // Pop Jester immediately if cranking while over the 'panic' threshold.
                    if (MoreCounterplay.Settings.JesterPanicThreshold > 0 && jesterSurface.TotalWeight >= MoreCounterplay.Settings.JesterPanicThreshold)
                    {
                        MoreCounterplay.Log("Uh oh...");

                        // Change behaviour state parameter to 2 to skip cranking.
                        stateIndex = 2;

                        // Switch Jester animation state to "JesterPopUp".
                        jesterSurface.SwitchAnimationClientRpc(panic: true);
                    }
                    break;
                case 2:
                    // Prevent Jester from popping if its weight goes past the 'prevent' threshold.
                    if (MoreCounterplay.Settings.JesterPreventThreshold > 0 && jesterSurface.TotalWeight >= MoreCounterplay.Settings.JesterPreventThreshold)
                    {
                        MoreCounterplay.Log("Preventing Jester from popping...");

                        // Change behaviour state parameter to 0 to prevent popping.
                        stateIndex = 0;

                        // Switch Jester animation state to "JesterPopUp", or "IdleDocile" if items will stay on its head.
                        jesterSurface.SwitchAnimationClientRpc();
                    }
                    else
                    {
                        // Drop all items on all clients if the Jester finished cranking while its weight is under the 'prevent' threshold.
                        jesterSurface.DropAllItemsOnClient();
                        jesterSurface.DropAllItemsServerRpc(GameNetworkManager.Instance.localPlayerController.GetComponent<NetworkObject>());
                    }
                    break;
                default:
                    break;
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItem))]
        [HarmonyPrefix]
        public static void GrabItem(GrabbableObject __instance)
        {
            if (!__instance.IsOwner || !MoreCounterplay.Settings.EnableJesterCounterplay)
                return;

            // Invoke item grab event for JesterSurface.
            JesterSurface.OnItemGrabAction?.Invoke(__instance);
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.HitEnemy))]
        [HarmonyPrefix]
        private static void HitJester(EnemyAI __instance, PlayerControllerB playerWhoHit, int hitID = -1)
        {
            if (playerWhoHit == null || !playerWhoHit.IsOwner || !MoreCounterplay.Settings.EnableJesterCounterplay)
                return;
            if (__instance.isEnemyDead || __instance.GetType() != typeof(JesterAI))
                return;

            // Drop all items on all clients if the Jester is hit by a shovel.
            if (__instance.currentBehaviourStateIndex != 2 && hitID == 1 && MoreCounterplay.Settings.DropItemsOnHit
                && __instance.transform.Find("JesterSurface")?.TryGetComponent(out JesterSurface jesterSurface) == true)
            {
                jesterSurface.DropAllItemsOnClient(hit: true);
                jesterSurface.DropAllItemsServerRpc(playerWhoHit.GetComponent<NetworkObject>(), hit: true);
            }
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.KillEnemy))]
        [HarmonyPrefix]
        private static void KillJester(EnemyAI __instance)
        {
            if (!__instance.IsOwner || !MoreCounterplay.Settings.EnableJesterCounterplay)
                return;
            if (__instance.isEnemyDead || __instance.GetType() != typeof(JesterAI))
                return;

            // Drop all items if 'EnemyAI.KillEnemy()' is ever called on a Jester (by another mod).
            if (__instance.transform.Find("JesterSurface")?.TryGetComponent(out JesterSurface jesterSurface) == true)
            {
                jesterSurface.DropAllItemsOnClient(hit: true);
                jesterSurface.DropAllItemsServerRpc(GameNetworkManager.Instance.localPlayerController.GetComponent<NetworkObject>(), hit: true);
            }
        }
    }
}
