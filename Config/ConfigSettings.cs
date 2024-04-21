using BepInEx.Configuration;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace MoreCounterplay.Config
{
    public static class ConfigSettings
    {
        #region Variables
        #region Jester
        public static ConfigEntry<bool> EnableJesterCounterplay;
        public static ConfigEntry<float> WeightToPreventJester;
        #endregion

        #region Turret
        public static ConfigEntry<bool> EnableTurretCounterplay;
        #endregion
        #endregion

        public static Dictionary<string, ConfigEntryBase> currentConfigEntries = new Dictionary<string, ConfigEntryBase>();

        public static void BindConfigSettings()
        {
            MoreCounterplay.Log("BindingConfigs");

            #region Jester
            EnableJesterCounterplay = AddConfigEntry(MoreCounterplay.Instance.Config.Bind("Server-side", "EnableJesterCounterplay", true, "[Host only] Add counterplay for Jester."));
            WeightToPreventJester = AddConfigEntry(MoreCounterplay.Instance.Config.Bind("Server-side", "WeightToPreventJester", 30f, "[Host only] Weight of items needed to prevent Jester pop out."));
            #endregion

            #region Turret
            EnableTurretCounterplay = AddConfigEntry(MoreCounterplay.Instance.Config.Bind("Server-side", "EnableTurretCounterplay", true, "[Host only] Add counterplay for Turret."));
            #endregion

            TryRemoveOldConfigSettings();
        }

        public static ConfigEntry<T> AddConfigEntry<T>(ConfigEntry<T> configEntry)
        {
            currentConfigEntries.Add(configEntry.Definition.Key, configEntry);
            return configEntry;
        }

        public static void TryRemoveOldConfigSettings()
        {
            HashSet<string> headers = new HashSet<string>();
            HashSet<string> keys = new HashSet<string>();

            foreach (ConfigEntryBase entry in currentConfigEntries.Values)
            {
                headers.Add(entry.Definition.Section);
                keys.Add(entry.Definition.Key);
            }

            try
            {
                ConfigFile config = MoreCounterplay.Instance.Config;
                string filepath = config.ConfigFilePath;

                if (File.Exists(filepath))
                {
                    string contents = File.ReadAllText(filepath);
                    string[] lines = File.ReadAllLines(filepath);

                    string currentHeader = "";

                    for (int i = 0; i < lines.Length; i++)
                    {
                        lines[i] = lines[i].Replace("\n", "");
                        if (lines[i].Length <= 0)
                            continue;

                        if (lines[i].StartsWith("["))
                        {
                            if (currentHeader != "" && !headers.Contains(currentHeader))
                            {
                                currentHeader = "[" + currentHeader + "]";
                                int index0 = contents.IndexOf(currentHeader);
                                int index1 = contents.IndexOf(lines[i]);
                                contents = contents.Remove(index0, index1 - index0);
                            }
                            currentHeader = lines[i].Replace("[", "").Replace("]", "").Trim();
                        }

                        else if (currentHeader != "")
                        {
                            if (i <= (lines.Length - 4) && lines[i].StartsWith("##"))
                            {
                                int numLinesEntry = 1;
                                while (i + numLinesEntry < lines.Length && lines[i + numLinesEntry].Length > 3)
                                    numLinesEntry++;

                                if (headers.Contains(currentHeader))
                                {
                                    int indexAssignOperator = lines[i + numLinesEntry - 1].IndexOf("=");
                                    string key = lines[i + numLinesEntry - 1].Substring(0, indexAssignOperator - 1);
                                    if (!keys.Contains(key))
                                    {
                                        int index0 = contents.IndexOf(lines[i]);
                                        int index1 = contents.IndexOf(lines[i + numLinesEntry - 1]) + lines[i + numLinesEntry - 1].Length;
                                        contents = contents.Remove(index0, index1 - index0);
                                    }
                                }
                                i += (numLinesEntry - 1);
                            }
                            else if (lines[i].Length > 3)
                                contents = contents.Replace(lines[i], "");
                        }
                    }

                    if (!headers.Contains(currentHeader))
                    {
                        currentHeader = "[" + currentHeader + "]";
                        int index0 = contents.IndexOf(currentHeader);
                        contents = contents.Remove(index0, contents.Length - index0);
                    }

                    while (contents.Contains("\n\n\n"))
                        contents = contents.Replace("\n\n\n", "\n\n");

                    File.WriteAllText(filepath, contents);
                    config.Reload();
                }
            }
            catch { }
        }
    }
}