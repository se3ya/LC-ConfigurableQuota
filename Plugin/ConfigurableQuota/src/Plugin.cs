using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ConfigurableQuota.Compat;
using ConfigurableQuota.Patches;

namespace ConfigurableQuota
{
  [BepInPlugin(Metadata.GUID, Metadata.PLUGIN_NAME, Metadata.VERSION)]
  [BepInDependency(Metadata.LETHAL_NETWORK_API_GUID)]
  [BepInDependency(Metadata.LETHAL_CONSTELLATIONS_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  [BepInDependency(Metadata.LETHAL_MOON_UNLOCKS_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  [BepInDependency(Metadata.OPEN_LIB_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  [BepInDependency(Metadata.LLL_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  [BepInDependency(Metadata.LUNAR_CONFIG_GUID, BepInDependency.DependencyFlags.SoftDependency)]
  public class Plugin : BaseUnityPlugin
  {
    private readonly Harmony _harmony = new(Metadata.GUID);
    public static Plugin Instance { get; private set; } = null!;
    public static ManualLogSource Log { get; private set; } = null!;

    void Awake()
    {
      Instance = this;

      Log = base.Logger;

      Log.LogInfo($"Initializing {Metadata.PLUGIN_NAME}");

      ConfigManager.Initialize(Config);

      ConstellationDeadlineConfig.Initialize();

      NetworkSync.Initialize();

      _harmony.PatchAll();

      OpenLibEventBridge.TrySubscribe();

      Log.LogInfo($"{Metadata.PLUGIN_NAME} is loaded!");
    }
  }
}
