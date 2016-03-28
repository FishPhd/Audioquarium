using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Audioquarium
{
  internal class Cfg
  {
    public static Dictionary<string, string> ConfigFile;

    #region cfg Loading and Saving

    public static void SetVariable(string varName, string varValue, ref Dictionary<string, string> configDict)
    {
      if (configDict.ContainsKey(varName))
        configDict[varName] = varValue;
      else
        configDict.Add(varName, varValue);
    }

    public static bool SaveConfigFile(string cfgFileName, Dictionary<string, string> configDict)
    {
      try
      {
        if (File.Exists(cfgFileName))
          File.Delete(cfgFileName);

        File.WriteAllLines(cfgFileName, configDict.Select(kvp => kvp.Key + " \"" + kvp.Value + "\"").ToArray());
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static bool LoadConfigFile(string cfgFileName, ref Dictionary<string, string> returnDict)
    {
      if (returnDict == null) throw new ArgumentNullException(nameof(returnDict));
      returnDict = new Dictionary<string, string>();
      if (!File.Exists(cfgFileName))
        return false;

      var lines = File.ReadAllLines(cfgFileName);
      foreach (var line in lines)
      {
        var splitIdx = line.IndexOf(" ", StringComparison.Ordinal);
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
      var cfgFileExists = LoadConfigFile("music_prefs.cfg", ref ConfigFile);

      if (!cfgFileExists)
      {
        SetVariable("Player.Color", "blue", ref ConfigFile);
        SetVariable("Music.Directory1", "", ref ConfigFile);
        Console.WriteLine(@"New CFG Created");
      }
      SaveConfigFile("music_prefs.cfg", ConfigFile);
    }

    #endregion
  }
}