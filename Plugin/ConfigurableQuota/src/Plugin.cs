using BepInEx;
using BepInEx.Logging;
using ConfigurableQuota.Compat;
using ConfigurableQuota.Patches;
using HarmonyLib;

namespace ConfigurableQuota;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(ModGUIDs.LethalNetworkAPI)]
[BepInDependency(ModGUIDs.LethalConstellations, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ModGUIDs.LethalMoonUnlocks, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ModGUIDs.OpenLib, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ModGUIDs.LethalLevelLoader, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ModGUIDs.LunarConfig, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ModGUIDs.SelfSortingStorage, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ModGUIDs.MoreShipUpgrades, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ModGUIDs.ScrapInsurance, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

    public static Plugin Instance { get; private set; } = null!;
    public static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        Log.LogInfo($"Initializing {MyPluginInfo.PLUGIN_NAME}");

        ConfigManager.Initialize(Config);
        ConstellationDeadlineConfig.Initialize();
        NetworkSync.Initialize();
        _harmony.PatchAll();
        OpenLibEventBridge.TrySubscribe();

        Log.LogInfo($"Hello {MyPluginInfo.PLUGIN_NAME}!");
    }
}
