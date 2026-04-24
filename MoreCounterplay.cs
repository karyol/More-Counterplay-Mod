using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MoreCounterplay.Behaviours;
using MoreCounterplay.Config;
using MoreCounterplay.Items;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using static MoreCounterplay.Patches.FeioparPatch;

namespace MoreCounterplay;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.sigurd.csync", "5.0.1")]
public class MoreCounterplay : BaseUnityPlugin
{
    public static MoreCounterplay Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony Harmony { get; set; } = null!;
    internal static ConfigSettings Settings { get; set; } = null!;

    public static AssetBundle? Bundle;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        Settings = new ConfigSettings(Config);

        NetcodePatcher();
        LoadAssets();
        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private void LoadAssets()
    {
        Log($"Loading Assets...");
        Bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "morecounterplayassets"));

        #region Jester
        // Load Jester surface prefab.
        GameObject jesterSurfaceContainer = Bundle.LoadAsset<GameObject>("JesterSurface.prefab");

        // Add JesterSurface component to Jester surface prefab.
        jesterSurfaceContainer.AddComponent<JesterSurface>();
        JesterSurface.JesterSurfacePrefab = jesterSurfaceContainer;

        // Register Jester surface prefab as a network prefab.
        LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(jesterSurfaceContainer);
        #endregion

        #region Coilhead
        // Load radioactive fire particle effects prefab.
        CoilExplosion.RadioactiveFirePrefab = Bundle.LoadAsset<GameObject>("RadioactiveFire.prefab");

        // Load 'Coilless Coilhead' prefab.
        Item headProperties = Bundle.LoadAsset<Item>("CoillessCoilhead.asset");

        // Add HeadItem component to 'Coilless Coilhead' prefab.
        headProperties.spawnPrefab.AddComponent<HeadItem>().itemProperties = headProperties;
        HeadItem.Prefab = headProperties.spawnPrefab;

        // Register 'Coilless Coilhead' as both a plain item and a network prefab.
        LethalLib.Modules.Items.RegisterItem(headProperties);
        LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(headProperties.spawnPrefab);
        #endregion

        #region Feiopar
        // Load Feiopar additional data container prefab.
        GameObject feioparDataContainer = Bundle.LoadAsset<GameObject>("FeioparAdditionalData.prefab");

        // Add FeioparAdditionalData component to Feiopar additional data container prefab.
        feioparDataContainer.AddComponent<FeioparAdditionalData>();
        FeioparAdditionalData.FeioparAdditionalDataPrefab = feioparDataContainer;

        // Register Feiopar additional data container as a network prefab.
        LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(feioparDataContainer);
        #endregion
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);
        Harmony.PatchAll();
    }

    private static void NetcodePatcher()
    {
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false).Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }

    public static void Log(string message) => Logger.LogInfo(message);
    public static void LogDebug(string message) => Logger.LogDebug(message);
    public static void LogError(string message) => Logger.LogError(message);
    public static void LogWarning(string message) => Logger.LogWarning(message);
    public static bool IsModLoaded(string guid) => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(guid);
}
