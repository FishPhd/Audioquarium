using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MusicPlayer
{
    internal class Cfg
    {
        public static Dictionary<string, string> configFile;

        #region cfg Loading and Saving

        public static void SetVariable(string varName, string varValue, ref Dictionary<string, string> configDict)
        {
            if (configDict.ContainsKey(varName))
                configDict[varName] = varValue;
            else
                configDict.Add(varName, varValue);
        }

        public static bool SaveConfigFile(string CfgFileName, Dictionary<string, string> configDict)
        {
            try
            {
                if (File.Exists(CfgFileName))
                    File.Delete(CfgFileName);

                var lines = new List<string>();
                foreach (var kvp in configDict)
                    lines.Add(kvp.Key + " \"" + kvp.Value + "\"");

                File.WriteAllLines(CfgFileName, lines.ToArray());
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool LoadConfigFile(string CfgFileName, ref Dictionary<string, string> returnDict)
        {
            returnDict = new Dictionary<string, string>();
            if (!File.Exists(CfgFileName))
                return false;

            var lines = File.ReadAllLines(CfgFileName);
            foreach (var line in lines)
            {
                var splitIdx = line.IndexOf(" ");
                if (splitIdx < 0 || splitIdx + 1 >= line.Length)
                    continue; // line isn't valid?
                var varName = line.Substring(0, splitIdx);
                var varValue = line.Substring(splitIdx + 1);

                // remove quotes
                if (varValue.StartsWith("\""))
                    varValue = varValue.Substring(1);
                if (varValue.EndsWith("\""))
                    varValue = varValue.Substring(0, varValue.Length - 1);

                SetVariable(varName, varValue, ref returnDict);
            }
            return true;
        }

        public static void Initial(bool error)
        {
            var CfgFileExists = LoadConfigFile("music_prefs.cfg", ref configFile);

            if (!CfgFileExists)
            {
                SetVariable("Player.Color", "blue", ref configFile);
                SetVariable("Music.Directory1", "", ref configFile);
                Console.WriteLine("New CFG Created");
            }
            SaveConfigFile("music_prefs.cfg", configFile);
        }

        #endregion
    }
}