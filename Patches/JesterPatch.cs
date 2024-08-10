using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MoreCounterplay.Config;
using UnityEngine;

namespace MoreCounterplay.Patches
{
    [HarmonyPatch]
    internal class JesterPatch
    {
        [HarmonyPatch(typeof(JesterAI), nameof(JesterAI.Start))]
        [HarmonyPostfix]
        public static void SpawnItemsCollider(JesterAI __instance)
        {
            if (!ConfigSettings.EnableJesterCounterplay.Value) return;
            AddHeadCollider(__instance, "HeadCollider");
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourState))]
        [HarmonyPrefix]
        public static bool CheckJesterHead(EnemyAI __instance, int stateIndex)
        {
            if (!ConfigSettings.EnableJesterCounterplay.Value) return true;
            if (__instance.GetType() != typeof(JesterAI)) return true;

            float weight = Mathf.RoundToInt(Mathf.Clamp(__instance.GetComponentInChildren<JesterHeadTrigger>().GetObjectsWeight(), 0f, 100f) * 105f);

            if (stateIndex == 2 && weight >= ConfigSettings.WeightToPreventJester.Value)
            {
                PreventJesterPopOut((JesterAI)__instance);
                return false;
            }

            if (stateIndex == 2 && weight < ConfigSettings.WeightToPreventJester.Value)
            {
                __instance.GetComponentInChildren<JesterHeadTrigger>().DropAllItems();
                return true;
            }

            return true;
        }

        public static void PreventJesterPopOut(JesterAI __instance)
        {
            MoreCounterplay.Log($"Jester pop out prevented");
            __instance.farAudio.Stop();
            __instance.creatureAnimator.SetFloat("CrankSpeedMultiplier", 1f);
            __instance.creatureAnimator.CrossFade(-51691287, .1f);
            __instance.SwitchToBehaviourState(0);
        }

        public static GameObject AddHeadCollider(JesterAI __instance, string name)
        {
            GameObject headCollider = new GameObject(name, typeof(BoxCollider));
            headCollider.transform.SetParent(__instance.GetComponentsInChildren<Transform>().FirstOrDefault(child => child.name == "MeshContainer"));
            headCollider.transform.localPosition = new Vector3(-.004f, 2.1f, .1171f);
            headCollider.transform.localRotation = Quaternion.identity;

            // v56 made 'GrabbableObject' items exclude the 'Colliders' layer.
            headCollider.layer = LayerMask.NameToLayer("Enemies");

            headCollider.GetComponent<BoxCollider>().center = Vector3.zero;
            headCollider.GetComponent<BoxCollider>().size = new Vector3(.9698f, .1f, 1.4889f);
            headCollider.GetComponent<BoxCollider>().isTrigger = true;
            headCollider.AddComponent<JesterHeadTrigger>();
            return headCollider;
        }
    }

    internal class JesterHeadTrigger : MonoBehaviour
    {
        private readonly List<GrabbableObject> _objectsOnHead = [];

        public float GetObjectsWeight()
        {
            float weight = 0f;

            // Remove all objects that match from the list.
            int itemsRemoved = _objectsOnHead.RemoveAll((GrabbableObject item) => item.parentObject != transform);
            MoreCounterplay.Log($"{itemsRemoved} objects removed from Jester's head");

            _objectsOnHead.ForEach(obj => weight += Mathf.Clamp(obj.itemProperties.weight - 1f, 0f, 10f));
            return weight;
        }

        public void DropAllItems()
        {
            MoreCounterplay.Log($"Drop all items from Jester");
            foreach (GrabbableObject item in _objectsOnHead)
            {
                _objectsOnHead.Remove(item);
                item.FallToGround();
                item.parentObject = null;
            }
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (!collision.CompareTag("PhysicsProp")) return;

            if (collision.TryGetComponent(out GrabbableObject grabbableObject))
            {
                // Check if item is inside the ship, or on the ground. Should prevent Jester stealing items if it goes under the ship.
                if (grabbableObject.isInShipRoom || grabbableObject.reachedFloorTarget)
                {
                    return;
                }

                MoreCounterplay.Log($"Add object to Jester's head {grabbableObject.name}");
                grabbableObject.parentObject = transform;
                if (!_objectsOnHead.Contains(grabbableObject)) _objectsOnHead.Add(grabbableObject);
            }
        }
    }
}
