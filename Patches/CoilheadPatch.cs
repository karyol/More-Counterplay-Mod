using GameNetcodeStuff;
using HarmonyLib;
using MoreCounterplay.Config;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
namespace MoreCounterplay.Patches
{
    [HarmonyPatch]
    internal class CoilheadPatch
    {
        private const int SHOVEL_HIT_ID = 1;
        private const int KNIFE_HIT_ID = 5;

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        [HarmonyPostfix]
        public static void SetCoilheadHP(EnemyAI __instance)
        {
            if (!ConfigSettings.EnableCoilheadCounterplay.Value) return;
            if (__instance.GetType() != typeof(SpringManAI)) return;

            __instance.enemyHP = ConfigSettings.SpringDurability.Value;
            __instance.enemyType.canDie = true;
            __instance.dieSFX = ((SpringManAI)__instance).springNoises[Random.Range(0, ((SpringManAI)__instance).springNoises.Length)];
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.HitEnemy))]
        [HarmonyPostfix]
        public static void HitCoilhead(EnemyAI __instance, int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            if (!ConfigSettings.EnableCoilheadCounterplay.Value) return;
            if (__instance.GetType() != typeof(SpringManAI)) return;

            if (__instance.IsOwner && !__instance.isEnemyDead)
            {
                switch (hitID)
                {
                    case KNIFE_HIT_ID:
                        CoilheadTakeHit(__instance, ConfigSettings.CoilheadKnifeDamage.Value);
                        break;

                    case SHOVEL_HIT_ID:
                        CoilheadTakeHit(__instance, ConfigSettings.CoilheadShovelDamage.Value);
                        break;

                    default:
                        CoilheadTakeHit(__instance, ConfigSettings.CoilheadDefaultDamage.Value);
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.KillEnemy))]
        [HarmonyPostfix]
        public static void DecapitateCoilhead(EnemyAI __instance)
        {
            if (!ConfigSettings.EnableCoilheadCounterplay.Value) return;
            if (__instance.GetType() != typeof(SpringManAI)) return;
            __instance.meshRenderers.First(mesh => mesh.name == "Head").gameObject.SetActive(false);
        }

        public static void CoilheadTakeHit(EnemyAI __instance, int force)
        {
            MoreCounterplay.Log($"Coilhead hit for {force} damage");
            __instance.enemyHP -= force;
            if (__instance.enemyHP <= 0) KillCoilhead(__instance);
        }

        public static void KillCoilhead(EnemyAI __instance)
        {
            MoreCounterplay.Log($"Coilhead killed");
            __instance.KillEnemyOnOwnerClient(false);
            SpawnHead(__instance.meshRenderers.First(mesh => mesh.name == "Head").gameObject.transform.position);
        }

        public static void SpawnHead(Vector3 spawnPosition)
        {
            if (!ConfigSettings.DropHeadAsScrap.Value) return;

            // Scale Coilhead scrap item down.
            MoreCounterplay.HeadItem.spawnPrefab.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            var headItem = GameObject.Instantiate(MoreCounterplay.HeadItem.spawnPrefab, spawnPosition, Quaternion.identity);
            headItem.GetComponentInChildren<GrabbableObject>().SetScrapValue(Random.Range(ConfigSettings.MinHeadValue.Value, ConfigSettings.MaxHeadValue.Value));
            headItem.GetComponentInChildren<NetworkObject>().Spawn();
            RoundManager.Instance.SyncScrapValuesClientRpc(new NetworkObjectReference[] { headItem.GetComponent<NetworkObject>() }, new int[] { headItem.GetComponent<GrabbableObject>().scrapValue });
        }
    }
}
