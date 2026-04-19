using HarmonyLib;
using UnityEngine;

namespace MoreCounterplay.Patches
{
    [HarmonyPatch]
    internal class FeioparPatch
    {
        internal class FeioparAdditionalData : MonoBehaviour
        {
            public Vector3 LastLaserPosition;
        }

        [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.Start))]
        [HarmonyPostfix]
        public static void OnSpawn(PumaAI __instance)
        {
            if (!MoreCounterplay.Settings.EnableFeioparCounterplay)
                return;

            __instance.gameObject.AddComponent<FeioparAdditionalData>();
        }

        [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.DoAIInterval))]
        [HarmonyPostfix]
        private static void CheckNearPlayersForLasers(PumaAI __instance)
        {
            if (!MoreCounterplay.Settings.EnableFeioparCounterplay)
                return;

            if (StartOfRound.Instance.livingPlayers == 0)
                return;

            if (__instance.isEnemyDead)
                return;

            foreach (var player in __instance.nearPlayers)
            {
                var currentlyHeldItem = player.currentlyHeldObjectServer;

                if (currentlyHeldItem == null)
                    continue;

                if (!currentlyHeldItem.itemProperties.itemName.ToLower().Contains("laser") || !currentlyHeldItem.itemProperties.itemName.ToLower().Contains("pointer"))
                    continue;

                if (!currentlyHeldItem.isBeingUsed)
                    continue;

                if (!Physics.Raycast(currentlyHeldItem.transform.position, currentlyHeldItem.transform.forward, out RaycastHit hit))
                    continue;

                float distance = Vector3.Distance(__instance.transform.position, hit.point);
                if (distance > MoreCounterplay.Settings.FeioparLaserPointerEffectiveRange)
                    continue;

                MoreCounterplay.Log($"Change feiopar behaviour");
                __instance.gameObject.GetComponent<FeioparAdditionalData>().LastLaserPosition = hit.point;
                __instance.SwitchToBehaviourState(3);
            }
        }

        [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.Update))]
        [HarmonyPostfix]
        private static void FeioparUpdate(PumaAI __instance)
        {
            if (!MoreCounterplay.Settings.EnableFeioparCounterplay)
                return;

            switch (__instance.currentBehaviourStateIndex)
            {
                case 3:
                    MoreCounterplay.Log($"Attack laser");
                    break;

                default:
                    return;
            }
        }
    }
}
