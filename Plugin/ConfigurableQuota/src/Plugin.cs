using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ConfigurableQuota.Compat;
using ConfigurableQuota.Patches;

namespace ConfigurableQuota
{
  [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
  [BepInDependency(ModGUIDs.LETHAL_NETWORK_API_GUID)]
  [BepInDependency(ModGUIDs.LETHAL_CONSTELLATIONS_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  [BepInDependency(ModGUIDs.LETHAL_MOON_UNLOCKS_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  [BepInDependency(ModGUIDs.OPEN_LIB_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  [BepInDependency(ModGUIDs.LLL_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  [BepInDependency(ModGUIDs.LUNAR_CONFIG_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  [BepInDependency(ModGUIDs.SELF_SORTING_STORAGE_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  public class Plugin : BaseUnityPlugin
  {
    private readonly Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);
    public static Plugin Instance { get; private set; } = null!;
    public static ManualLogSource Log { get; private set; } = null!;

    void Awake()
    {
      Instance = this;

      Log = base.Logger;

      Log.LogInfo($"Initializing {MyPluginInfo.PLUGIN_NAME}");

      ConfigManager.Initialize(Config);

      ConstellationDeadlineConfig.Initialize();

      NetworkSync.Initialize();

      _harmony.PatchAll();

      OpenLibEventBridge.TrySubscribe();

      Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} is loaded!");
    }
  }
}
