using System;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using ConfigurableQuota.Patches;
using OpenLib.Events;

namespace ConfigurableQuota.Compat
{
    internal static class OpenLibEventBridge
    {
        private static bool _subscribed;

        internal static void TrySubscribe()
        {
            if (_subscribed)
                return;

            if (!Chainloader.PluginInfos.ContainsKey(Metadata.OPEN_LIB_GUID))
            {
                Plugin.Log.LogDebug("OpenLib not loaded; constellation deadline event subscription skipped.");
                return;
            }

            try
            {
                Subscribe();
                _subscribed = true;
                Plugin.Log.LogInfo("Subscribed to OpenLib StartOfRoundChangeLevel for constellation deadline updates.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not subscribe to OpenLib events: {e.Message}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Subscribe()
        {
            EventManager.StartOfRoundChangeLevel.AddListener(OnLevelChanged);
        }

        private static void OnLevelChanged()
        {
            try
            {
                var tod = TimeOfDay.Instance;
                if (tod == null || tod.timesFulfilledQuota != 0)
                    return;

                ConstellationDeadlineConfig.RefreshSections();

                if (!TimeOfDayQuotaPatch.TryApplyInitialDeadlineFromCurrentMode(
                        tod,
                        allowConstellationOverride: true,
                        logSelection: true))
                {
                    return;
                }

                if (((Unity.Netcode.NetworkBehaviour)tod).IsServer)
                {
                    NetworkSync.SyncDeadlineToClients(tod.daysUntilDeadline);
                    Plugin.Log.LogInfo($"Constellation deadline synced: {tod.daysUntilDeadline} days.");
                }

                TimeOfDayQuotaPatch.RefreshExternalMonitors();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"OpenLib level-change handler failed: {e.Message}");
            }
        }
    }
}
