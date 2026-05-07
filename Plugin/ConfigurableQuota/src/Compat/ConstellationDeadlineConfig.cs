using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace ConfigurableQuota.Compat
{
    internal enum ConstellationDeadlineMode
    {
        UseGlobal,
        Fixed,
        Random
    }

    internal static class ConstellationDeadlineConfig
    {
        private const string FileName = "ConfigurableQuota_Constellations.cfg";
        private const string SectionPrefix = "Constellation: ";

        private sealed class ConstellationEntries
        {
            internal ConfigEntry<string> DeadlineMode = null!;
            internal ConfigEntry<int> FixedDays = null!;
            internal ConfigEntry<int> DeadlineMin = null!;
            internal ConfigEntry<int> DeadlineMax = null!;
        }

        private static readonly Dictionary<string, ConstellationEntries> Entries = new(StringComparer.OrdinalIgnoreCase);

        private static ConfigFile? _constellationConfig;
        private static bool _initialized;
        private static bool _loggedConfigFailure;

        internal static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            if (!LethalConstellationsCompat.IsInstalled)
                return;

            EnsureConfigReady();
            RefreshSections();
        }

        internal static void RefreshSections()
        {
            if (!LethalConstellationsCompat.IsInstalled)
                return;

            if (!EnsureConfigReady())
                return;

            try
            {
                List<string> constellationNames = LethalConstellationsCompat.GetKnownConstellationNames();
                if (constellationNames.Count == 0)
                    return;

                bool addedNewEntries = false;
                foreach (string constellationName in constellationNames)
                {
                    if (constellationName.Length == 0)
                        continue;

                    if (EnsureConstellationEntry(constellationName))
                        addedNewEntries = true;
                }

                if (addedNewEntries)
                    _constellationConfig!.Save();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not refresh constellation deadline sections: {e.Message}");
            }
        }

        internal static bool TryGetCurrentConstellationSettings(
            out string constellationName,
            out ConstellationDeadlineMode mode,
            out int fixedDays,
            out int min,
            out int max)
        {
            constellationName = string.Empty;
            mode = ConstellationDeadlineMode.UseGlobal;
            fixedDays = Math.Max(1, ConfigManager.DaysToDeadline.Value);
            min = Math.Max(1, ConfigManager.DeadlineMin.Value);
            max = Math.Max(min, ConfigManager.DeadlineMax.Value);

            if (!LethalConstellationsCompat.IsInstalled)
                return false;

            if (!LethalConstellationsCompat.TryGetCurrentConstellationName(out constellationName))
                return false;

            if (!EnsureConfigReady())
                return false;

            if (!Entries.TryGetValue(constellationName, out ConstellationEntries? entry))
            {
                if (!EnsureConstellationEntry(constellationName))
                    return false;

                _constellationConfig!.Save();
                entry = Entries[constellationName];
            }

            mode = ParseMode(entry.DeadlineMode.Value);
            fixedDays = Math.Max(1, entry.FixedDays.Value);
            min = Math.Max(1, entry.DeadlineMin.Value);
            max = Math.Max(min, entry.DeadlineMax.Value);
            return true;
        }

        private static bool EnsureConfigReady()
        {
            if (_constellationConfig != null)
                return true;

            try
            {
                string path = Path.Combine(Paths.ConfigPath, FileName);
                _constellationConfig = new ConfigFile(path, true);
                return true;
            }
            catch (Exception e)
            {
                if (!_loggedConfigFailure)
                {
                    _loggedConfigFailure = true;
                    Plugin.Log.LogWarning($"Could not initialize {FileName}: {e.Message}");
                }

                return false;
            }
        }

        private static bool EnsureConstellationEntry(string constellationName)
        {
            if (_constellationConfig == null)
                return false;

            if (Entries.ContainsKey(constellationName))
                return false;

            string section = $"{SectionPrefix}{constellationName}";
            int defaultFixed = Math.Max(1, ConfigManager.DaysToDeadline.Value);
            int defaultMin = Math.Max(1, ConfigManager.DeadlineMin.Value);
            int defaultMax = Math.Max(defaultMin, ConfigManager.DeadlineMax.Value);

            var entries = new ConstellationEntries
            {
                DeadlineMode = _constellationConfig.Bind(
                    section,
                    "DeadlineMode",
                    ConstellationDeadlineMode.UseGlobal.ToString(),
                    new ConfigDescription(
                        "Deadline mode for this constellation. UseGlobal uses main ConfigurableQuota deadline settings, Fixed uses FixedDaysToDeadline, Random uses DeadlineMin/DeadlineMax.",
                        new AcceptableValueList<string>(
                            ConstellationDeadlineMode.UseGlobal.ToString(),
                            ConstellationDeadlineMode.Fixed.ToString(),
                            ConstellationDeadlineMode.Random.ToString()
                        )
                    )
                ),
                FixedDays = _constellationConfig.Bind(
                    section,
                    "FixedDaysToDeadline",
                    defaultFixed,
                    "Used when Deadline Mode is Fixed."
                ),
                DeadlineMin = _constellationConfig.Bind(
                    section,
                    "DeadlineMin",
                    defaultMin,
                    "Used when Deadline Mode is Random."
                ),
                DeadlineMax = _constellationConfig.Bind(
                    section,
                    "DeadlineMax",
                    defaultMax,
                    "Used when Deadline Mode is Random."
                )
            };

            Entries[constellationName] = entries;
            Plugin.Log.LogInfo($"Added constellation deadline settings section for '{constellationName}'.");
            return true;
        }

        private static ConstellationDeadlineMode ParseMode(string rawMode)
        {
            if (Enum.TryParse(rawMode, true, out ConstellationDeadlineMode parsedMode))
                return parsedMode;

            return ConstellationDeadlineMode.UseGlobal;
        }
    }
}
