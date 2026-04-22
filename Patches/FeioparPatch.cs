using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace MoreCounterplay.Patches
{
    [HarmonyPatch]
    internal class FeioparPatch
    {
        internal class FeioparAdditionalData : MonoBehaviour
        {
            public bool ActiveLaserPointerInRange;
            public bool WaitForTreeDrop;
            public float ClosestLaserDistance;
            public Vector3 LastLaserPosition;
            public int PumaAttackAnimationHash;
            public float TimeAtLastEnemyScratch;

            public void Initialize(PumaAI pumaAI)
            {
                ActiveLaserPointerInRange = false;
                WaitForTreeDrop = false;
                ClosestLaserDistance = float.MaxValue;
                LastLaserPosition = pumaAI.transform.position;
                PumaAttackAnimationHash = Animator.StringToHash("Base Layer.PumaAttack1");
                TimeAtLastEnemyScratch = Time.realtimeSinceStartup;
                AddBehaviourState(pumaAI);
                AllowHittingEnemies(pumaAI);
            }

            private void AddBehaviourState(PumaAI pumaAI)
            {
                pumaAI.enemyBehaviourStates = pumaAI.enemyBehaviourStates.AddToArray(new()
                {
                    name = "AttackLaserPointer",
                });
            }

            private void AllowHittingEnemies(PumaAI pumaAI)
            {
                var enemyAICollisionDetect = pumaAI.GetComponentInChildren<EnemyAICollisionDetect>();

                if (enemyAICollisionDetect.canCollideWithEnemies)
                    return;

                enemyAICollisionDetect.canCollideWithEnemies = MoreCounterplay.Settings.CanDamageEnemiesWhileAttackingLaser;
            }
        }

        [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.Start))]
        [HarmonyPostfix]
        public static void OnSpawn(PumaAI __instance)
        {
            if (!MoreCounterplay.Settings.EnableFeioparCounterplay)
                return;

            __instance.gameObject.AddComponent<FeioparAdditionalData>().Initialize(__instance);
        }

        [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.DoAIInterval))]
        [HarmonyPostfix]
        private static void CheckNearbyPlayersForLasers(PumaAI __instance)
        {
            if (!MoreCounterplay.Settings.EnableFeioparCounterplay)
                return;

            if (StartOfRound.Instance.livingPlayers == 0)
                return;

            if (__instance.isEnemyDead)
                return;

            if (!__instance.gameObject.TryGetComponent(out FeioparAdditionalData feioparAdditionalData))
                return;

            // Initialize the additional data for this interval
            feioparAdditionalData.ActiveLaserPointerInRange = false;
            feioparAdditionalData.ClosestLaserDistance = float.MaxValue;
            feioparAdditionalData.LastLaserPosition = __instance.transform.position;

            foreach (var player in __instance.nearPlayers)
            {
                var currentlyHeldItem = player.currentlyHeldObjectServer;

                if (currentlyHeldItem == null) // No item held
                    continue;

                if (!currentlyHeldItem.itemProperties.itemName.ToLower().Contains("laser") || !currentlyHeldItem.itemProperties.itemName.ToLower().Contains("pointer")) // Not a laser pointer
                    continue;

                if (!currentlyHeldItem.isBeingUsed) // Not being used
                    continue;

                Vector3 raycastStartPosition = ((FlashlightItem)currentlyHeldItem).flashlightBulb.transform.position;
                if (!DoRaycast(raycastStartPosition, currentlyHeldItem.transform.forward, out RaycastHit hit)) // No raycast hit
                    continue;

                float distance = Vector3.Distance(__instance.transform.position, hit.point);
                if (distance > MoreCounterplay.Settings.FeioparLaserPointerEffectiveRange) // Too far away
                    continue;

                if (feioparAdditionalData.ClosestLaserDistance < distance) // Not closer than the current closest laser
                    return;

                // Laser is close enough and closer than the current closest laser, update the data
                feioparAdditionalData.ActiveLaserPointerInRange = true;
                feioparAdditionalData.ClosestLaserDistance = distance;
                feioparAdditionalData.LastLaserPosition = hit.point;

                if (__instance.clingingToTree && !feioparAdditionalData.WaitForTreeDrop) // Drop if clinging to tree
                {
                    feioparAdditionalData.WaitForTreeDrop = true;
                    ForceTreeDrop(__instance);
                    return;
                }

                if (feioparAdditionalData.WaitForTreeDrop) // Wait for tree drop to finish
                    return;

                if (__instance.currentBehaviourStateIndex != 3) // Change state if needed
                    __instance.SwitchToBehaviourState(3);
            }
        }

        private static bool DoRaycast(Vector3 origin, Vector3 direction, out RaycastHit hit)
        {
            hit = default;
            var hits = Physics.RaycastAll(origin, direction); // Get all raycast hits in the direction of the laser pointer

            if (hits.Length == 0) // No hits at all
                return false;

            var sortedHits = hits.OrderBy(x => x.distance);
            foreach (var raycastHit in sortedHits)
            {
                if (raycastHit.transform.GetComponentInParent<PumaAI>() != null) // Ignore raycast hits on the Feiopars
                    continue;

                hit = raycastHit;
                break;
            }

            return true;
        }

        private static void ForceTreeDrop(PumaAI __instance)
        {
            if (!__instance.IsServer && !__instance.IsHost)
                return;

            MoreCounterplay.Log("Feiopar: Force tree drop");
            if (__instance.StartTreeDropOnLocalClient(false))
            {
                __instance.StartTreeDropRpc(
                    __instance.clingIdlePosition,
                    __instance.leapFromPosition,
                    __instance.treeClingRotation,
                    false
                );
            }
        }

        [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.EndDroppingDownAnimationOnLocalClient))]
        [HarmonyPrefix]
        private static bool InterceptTreeDropComplete(PumaAI __instance)
        {
            if (!MoreCounterplay.Settings.EnableFeioparCounterplay) // Normal flow if counterplay disabled
                return true;

            if (!__instance.gameObject.TryGetComponent(out FeioparAdditionalData feioparAdditionalData))
                return true;

            if (feioparAdditionalData.WaitForTreeDrop) // Waiting for the tree drop to complete in order to switch to the laser attack
            {
                MoreCounterplay.Log("Feiopar: Completed tree drop - switching to laser attack");
                __instance.inSpecialAnimation = false;
                __instance.serverPosition = __instance.transform.position;
                __instance.clingingToTree = false;
                __instance.relocatingToNewTree = false;
                __instance.treeState = TreeState.ClimbingUp;
                __instance.creatureAnimator.SetBool("TreeMode", false);
                __instance.creatureAnimator.SetBool("Dropping", false);
                __instance.creatureAnimator.SetBool("Climbing", false);
                __instance.timeAtLastDrop = Time.realtimeSinceStartup;

                if (!__instance.agent.enabled)
                    __instance.agent.enabled = true;

                __instance.SwitchToBehaviourStateOnLocalClient(3);
                feioparAdditionalData.WaitForTreeDrop = false;

                return false; // Skip the original method to prevent any unwanted side effects from it
            }

            return true; // Normal flow if not waiting for tree drop
        }

        [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.Update))]
        [HarmonyPostfix]
        private static void FeioparUpdate(PumaAI __instance)
        {
            if (!MoreCounterplay.Settings.EnableFeioparCounterplay)
                return;

            if (!__instance.gameObject.TryGetComponent(out FeioparAdditionalData feioparAdditionalData))
                return;

            switch (__instance.currentBehaviourStateIndex)
            {
                case 3: // Laser pointer behaviour state
                    if (__instance.previousState != __instance.currentBehaviourStateIndex) // Initialize state
                    {
                        MoreCounterplay.Log("Feiopar: Initialize laser attack state");
                        // Reset state parameters
                        __instance.TargetTree = null;
                        __instance.clingingToTree = false;
                        __instance.stalkingFrozen = false;
                        __instance.stalkingFrozenAnim = false;
                        __instance.hidingQuickly = false;
                        __instance.inSpecialAnimation = false;
                        __instance.wasOwnerLastFrame = false;
                        __instance.stopStalkingTimer = 0f;
                        __instance.leapAttempts = 0;

                        // Setup animation parameters
                        __instance.creatureAnimator.SetBool("Attacking", true);
                        __instance.creatureAnimator.SetBool("Running", false);
                        __instance.creatureAnimator.SetBool("TreeMode", false);
                        __instance.creatureAnimator.SetBool("Climbing", false);
                        __instance.creatureAnimator.SetBool("Startled", false);
                        __instance.creatureAnimator.Play(feioparAdditionalData.PumaAttackAnimationHash);

                        // Setup audio
                        __instance.attackSFX.clip = __instance.attackScream;
                        __instance.attackSFX.pitch = Random.Range(0.94f, 1.1f);
                        __instance.growlSource.Stop();
                        __instance.creatureSFX.clip = __instance.pumaRunSFX;
                        __instance.creatureSFX.Play();

                        __instance.previousState = __instance.currentBehaviourStateIndex;
                    }

                    if (!feioparAdditionalData.ActiveLaserPointerInRange) // No active laser pointer in range, exit this behaviour state
                    {
                        __instance.SwitchToBehaviourState(0);
                        return;
                    }

                    // Get the target point
                    Vector3 targetPoint = feioparAdditionalData.LastLaserPosition;

                    if (__instance.IsOwner && __instance.agent.enabled) // Move towards the laser pointer
                    {
                        __instance.SetDestinationToPosition(targetPoint, false);
                        __instance.movingTowardsTargetPlayer = false;
                        __instance.moveTowardsDestination = true;
                    }

                    // Movement parameters (the same as in state 2 - the attack state)
                    __instance.agent.stoppingDistance = 2.6f;
                    __instance.agent.speed = __instance.attackRunSpeed;
                    __instance.agent.acceleration = __instance.attackAcceleration;

                    // Calculate distance to target
                    float distanceToTarget = Vector3.Distance(__instance.transform.position, targetPoint);

                    // Play scratching animation if close enough to the target
                    __instance.scratching = distanceToTarget < 6f;
                    __instance.creatureAnimator.SetBool("Scratching", __instance.scratching);

                    // Scream audio if close enough to the target
                    if (distanceToTarget < 6f)
                    {
                        if (!__instance.startedScreamingInAttack)
                        {
                            __instance.startedScreamingInAttack = true;
                            __instance.attackSFX.Play();
                            __instance.attackSFXFaraway.Play();
                        }

                        __instance.attackSFX.volume = Mathf.Lerp(__instance.attackSFX.volume, 1f, 10f * Time.deltaTime);
                        __instance.attackSFXFaraway.volume = Mathf.Lerp(__instance.attackSFXFaraway.volume, 1f, 10f * Time.deltaTime);
                    }

                    // Look towards the target point
                    if (__instance.IsOwner)
                    {
                        __instance.turnCompass.LookAt(targetPoint);
                        __instance.turnCompass.eulerAngles = new Vector3(0f, __instance.turnCompass.eulerAngles.y, 0f);
                        __instance.transform.rotation = Quaternion.Lerp(
                            __instance.transform.rotation,
                            __instance.turnCompass.rotation,
                            10f * Time.deltaTime
                        );
                        __instance.transform.localEulerAngles = new Vector3(0f, __instance.transform.localEulerAngles.y, 0f);
                    }

                    // Animate movement
                    if (__instance.IsOwner && __instance.agent.enabled)
                    {
                        __instance.CalculateAnimationDirection(1f);
                    }
                    break;

                default:
                    return;
            }
        }

        [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.AnimationEventC))]
        [HarmonyPostfix]
        private static void ScratchAnimationEvent(PumaAI __instance)
        {
            if (!MoreCounterplay.Settings.EnableFeioparCounterplay)
                return;

            if (__instance.currentBehaviourStateIndex != 3) // Check if in the laser pointer attack state
                return;

            // Play scratch sound effect
            RoundManager.PlayRandomClip(__instance.creatureSFX, __instance.scratchSFX, true, Random.Range(0.6f, 1f), 0, 1000);

            if (MoreCounterplay.Settings.CanDamagePlayerWhileAttackingLaser) // Apply scratch damage to nearby players
                __instance.timeAtLastScratch = Time.realtimeSinceStartup;

            if (__instance.gameObject.TryGetComponent(out FeioparAdditionalData feioparAdditionalData) // Apply scratch damage to nearby enemies
                && MoreCounterplay.Settings.CanDamageEnemiesWhileAttackingLaser)
                feioparAdditionalData.TimeAtLastEnemyScratch = Time.realtimeSinceStartup;
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.OnCollideWithEnemy))]
        [HarmonyPostfix]
        private static void OnCollideWithEnemy(EnemyAI __instance, Collider other, EnemyAI collidedEnemy)
        {
            if (!MoreCounterplay.Settings.EnableFeioparCounterplay)
                return;

            if (!MoreCounterplay.Settings.CanDamageEnemiesWhileAttackingLaser)
                return;

            if (__instance.isEnemyDead || __instance.GetType() != typeof(PumaAI)) // Check if enemy is alive and is a Feiopar
                return;

            if (__instance.currentBehaviourStateIndex != 3) // Check if in the laser pointer attack state
                return;

            if (collidedEnemy == null)
                return;

            if (collidedEnemy.enemyType == __instance.enemyType) // Prevent dealing damage to enemies of the same type (Feiopars)
                return;

            if (!collidedEnemy.enemyType.canDie) // Do not try to damage immortal enemies
                return;

            if (!__instance.gameObject.TryGetComponent(out FeioparAdditionalData feioparAdditionalData))
                return;

            if (Time.realtimeSinceStartup - feioparAdditionalData.TimeAtLastEnemyScratch >= 3f) // Check cooldown
                return;

            MoreCounterplay.Log($"Feiopar: Deal damage to enemy {collidedEnemy.name}");
            feioparAdditionalData.TimeAtLastEnemyScratch = 0f;
            collidedEnemy.HitEnemy(MoreCounterplay.Settings.FeioparEnemyHitForce, null, true, -1);
        }
    }
}
