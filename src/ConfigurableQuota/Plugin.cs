using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ConfigurableQuota.Patches;

namespace ConfigurableQuota
{
  [BepInPlugin(Metadata.GUID, Metadata.PLUGIN_NAME, Metadata.VERSION)]
  [BepInDependency("LethalNetworkAPI")]
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

      NetworkSync.Initialize();

      _harmony.PatchAll();

      Log.LogInfo($"{Metadata.PLUGIN_NAME} is loaded!");
    }
  }
}
