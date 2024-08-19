using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MoreCounterplay.Config;
using System.IO;
using UnityEngine;

namespace MoreCounterplay;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
public class MoreCounterplay : BaseUnityPlugin
{
    public static MoreCounterplay Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    #region Assets
    public static AssetBundle? Bundle;
    public static Item HeadItem { get; private set; } = null!;
    #endregion

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        LoadAssets();
        ConfigSettings.BindConfigSettings();
        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private void LoadAssets()
    {
        Log($"Loading Assets");
        Bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "morecounterplayassets"));

        HeadItem = Bundle.LoadAsset<Item>("Head.asset");

        // Head size, position, and rotation adjustments.
        HeadItem.itemSpawnsOnGround = false;
        HeadItem.restingRotation = new Vector3(-90, 0, 90);
        HeadItem.spawnPrefab.transform.localScale = new Vector3(0.1763f, 0.1763f, 0.1763f);
        HeadItem.spawnPrefab.transform.rotation *= Quaternion.Euler(-90f, 0f, 0f);
        HeadItem.verticalOffset = 0.1f;

        LethalLib.Modules.Items.RegisterItem(HeadItem); // Register as a plain item to persist when reloading the save file.
        LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(HeadItem.spawnPrefab);
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);
        Harmony.PatchAll();
    }

    public static void Log(string message) => Logger.LogInfo(message);
    public static void LogError(string message) => Logger.LogError(message);
    public static void LogWarning(string message) => Logger.LogWarning(message);
    public static bool IsModLoaded(string guid) => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(guid);
}
