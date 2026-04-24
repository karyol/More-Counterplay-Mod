using HarmonyLib;
using MoreCounterplay.Behaviours;
using MoreCounterplay.Items;
using MoreCounterplay.Util;
using UnityEngine;

using static MoreCounterplay.MoreCounterplay;

namespace MoreCounterplay.Patches
{
    /// <summary>
    ///     Patches for loading/unloading stuff upon joining or leaving a lobby.
    /// </summary>
    [HarmonyPatch]
    internal class LoadPatches
    {
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.StartDisconnect))]
        [HarmonyPostfix]
        private static void OnDisconnect()
        {
            // Remove added components from the Coilhead prefab.
            CoilExplosion.RadioactiveFirePrefab?.transform.SetParent(null, false);
            Object.Destroy(CoilExplosion.CoilheadPrefab?.GetComponent<CoilExplosion>());

            // Reset Coilhead prefab to vanilla values.
            if (CoilExplosion.CoilheadPrefab?.TryGetComponent(out SpringManAI coilhead) == true)
            {
                coilhead.enemyHP = 3;
                coilhead.enemyType.canDie = false;
            }
        }

        /// <summary>
        ///     Finds vanilla prefabs and grabs their textures or adds components to them.
        /// </summary>
        internal static void FindAndModifyPrefabs()
        {
            // Currently, only the Coilhead counterplay requires textures loaded at runtime.
            if (!Settings.EnableCoilheadCounterplay)
                return;

            Log("Loading prefab textures...");

            // Try to find and obtain the Coilhead's enemy prefab.
            if (VanillaPrefabUtils.GetInsideEnemyPrefab("Spring", out GameObject? coilheadPrefab))
            {
                // Save reference to vanilla Coilhead prefab for convenience.
                CoilExplosion.CoilheadPrefab = coilheadPrefab;

                if (HeadItem.Prefab != null && coilheadPrefab != null)
                {
                    // Modify Coilhead health to the configured amount and allow it to die.
                    SpringManAI coilhead = coilheadPrefab.GetComponent<SpringManAI>();
                    coilhead.enemyHP = Settings.SpringDurability;
                    coilhead.enemyType.canDie = true;

                    if (Settings.LoreAccurateCoilheads)
                    {
                        // Add explosion network behaviour script to the Coilhead prefab.
                        coilheadPrefab.AddComponent<CoilExplosion>().enabled = false;

                        if (CoilExplosion.RadioactiveFirePrefab != null && (Settings.ExplosionFire.Value || Settings.ExplosionParticles.Value))
                        {
                            // Add radioactive fire particle effects container to the Coilhead prefab.
                            CoilExplosion.RadioactiveFirePrefab.transform.SetParent(coilheadPrefab.transform.Find("SpringManModel"), false);
                            CoilExplosion.RadioactiveFirePrefab.SetActive(false);

                            // Enable/disable individual components of the radioactive fire particle effects prefab.
                            CoilExplosion.RadioactiveFirePrefab.transform.Find("FireLight").GetComponent<Light>().enabled = Settings.ExplosionFire.Value;
                            CoilExplosion.RadioactiveFirePrefab.transform.Find("GreenFlame").GetComponent<ParticleSystemRenderer>().enabled = Settings.ExplosionFire.Value;
                            CoilExplosion.RadioactiveFirePrefab.transform.Find("RadioactiveParticles").GetComponent<ParticleSystemRenderer>().enabled = Settings.ExplosionParticles.Value;
                        }
                    }

                    if (Settings.DropHeadAsScrap)
                    {
                        // Find the Coilhead head transform in its enemy prefab.
                        var coilheadHead = coilheadPrefab.transform.Find("SpringManModel/Head");

                        // Obtain Coilhead head mesh from its enemy prefab and assign it to the 'Coilless Coilhead' scrap item prefab.
                        Mesh mesh = coilheadHead.GetComponent<MeshFilter>().sharedMesh;
                        HeadItem.Prefab.GetComponent<MeshFilter>().sharedMesh = mesh;

                        // Obtain Coilhead material from its enemy prefab and assign it to the 'Coilless Coilhead' scrap item prefab.
                        Material coilheadMaterial = coilheadHead.GetComponent<MeshRenderer>().sharedMaterial;
                        HeadItem.Prefab.GetComponent<MeshRenderer>().sharedMaterial = coilheadMaterial;
                    }
                }
                else
                {
                    LogWarning("Either the 'Coilless Coilhead' prefab did not load or the Coilhead enemy prefab could not be found. "
                        + "Some stuff might not load or work properly.");
                }
            }

            // Try to find and obtain the Forest Giant enemy prefab.
            if (Settings.LoreAccurateCoilheads && Settings.ExplosionFire.Value && VanillaPrefabUtils.GetOutsideEnemyPrefab("ForestGiant", out GameObject? giantPrefab))
            {
                if (CoilExplosion.RadioactiveFirePrefab != null
                    && giantPrefab != null
                    && giantPrefab.transform.Find("FireParticlesContainer/LingeringFire").TryGetComponent(out ParticleSystemRenderer giantLingeringFire))
                {
                    // Obtain burning Forest Giant flame texture from its enemy prefab and assign it to the radioactive fire's 'GreenFlame' material.
                    Texture flameTexture = giantLingeringFire.sharedMaterial.mainTexture;
                    var greenFlame = CoilExplosion.RadioactiveFirePrefab.transform.Find("GreenFlame");

                    if (greenFlame != null && greenFlame.TryGetComponent(out ParticleSystemRenderer greenFlameParticles))
                        greenFlameParticles.sharedMaterial.mainTexture = flameTexture;

                }
                else
                {
                    LogWarning("Either the 'RadioactiveFire' prefab did not load or the Forest Giant enemy prefab could not be found. "
                        + "Some stuff might not load or work properly.");
                }
            }

            Log("Finished loading textures!");
        }
    }
}