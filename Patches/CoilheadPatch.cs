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
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.HitEnemy))]
        [HarmonyPostfix]
        public static void HitCoilhead(EnemyAI __instance, int hitID = -1)
        {
            if (!ConfigSettings.EnableCoilheadCounterplay.Value) return;
            if (__instance.GetType() != typeof(SpringManAI)) return;

            if (__instance.IsOwner && !__instance.isEnemyDead)
            {
                switch (hitID)
                {
                    case KNIFE_HIT_ID:
                        DamageCoilhead((SpringManAI)__instance, ConfigSettings.CoilheadKnifeDamage.Value);
                        break;

                    case SHOVEL_HIT_ID:
                        DamageCoilhead((SpringManAI)__instance, ConfigSettings.CoilheadShovelDamage.Value);
                        break;

                    default:
                        DamageCoilhead((SpringManAI)__instance, ConfigSettings.CoilheadDefaultDamage.Value);
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
            SwapHead(__instance.meshRenderers.First(mesh => mesh.name == "Head").gameObject);
        }

        public static void DamageCoilhead(SpringManAI coilhead, int force)
        {
            MoreCounterplay.Log($"Coilhead hit for {force} damage");
            coilhead.enemyHP -= force;
            if (coilhead.enemyHP <= 0)
            {
                coilhead.DoSpringAnimation(true); // Boioioioing.
                KillCoilhead(coilhead);
            }
        }

        public static void KillCoilhead(SpringManAI coilhead)
        {
            MoreCounterplay.Log($"Coilhead killed");
            coilhead.KillEnemyOnOwnerClient(false);

            // Reset Coilhead animations, collider, and target after death.
            ResetCoilhead(coilhead);
        }

        public static void ResetCoilhead(SpringManAI coilhead)
        {
            coilhead.hasStopped = true;
            coilhead.creatureAnimator.SetFloat("walkSpeed", 0f);
            coilhead.currentAnimSpeed = 0f;
            coilhead.mainCollider.isTrigger = false;
            if (coilhead.IsOwner)
            {
                coilhead.agent.speed = 0f;
                coilhead.movingTowardsTargetPlayer = false;
                coilhead.SetDestinationToPosition(coilhead.transform.position);
            }
        }

        public static void SwapHead(GameObject originalHead)
        {
            GameObject headItem = Object.Instantiate(MoreCounterplay.HeadItem.spawnPrefab, originalHead.transform.position, originalHead.transform.rotation);
            headItem.GetComponentInChildren<NetworkObject>().Spawn();

            GrabbableObject grabbableHead = headItem.GetComponentInChildren<GrabbableObject>();
            grabbableHead.SetScrapValue(Random.Range(ConfigSettings.MinHeadValue.Value, ConfigSettings.MaxHeadValue.Value));
            RoundManager.Instance.SyncScrapValuesClientRpc([headItem.GetComponent<NetworkObject>()], [headItem.GetComponent<GrabbableObject>().scrapValue]);

            // Stop original head from rendering (instead of disabling it).
            originalHead.GetComponentInChildren<Renderer>().enabled = false;

            // Attach scrap head to the original to move alongside it.
            grabbableHead.parentObject = originalHead.transform;
        }
    }
}
