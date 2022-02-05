using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters;
using System.IO;
using System.Text;

public static class SettingsManager
{

    public static void SaveSettings(SettingsData sd)
    {
        string dataPath = string.Format("{0}/settings.json", Application.persistentDataPath);  
        try
        {
            var json = JsonUtility.ToJson(sd); 
            File.WriteAllText(dataPath, json);
        }
        catch (Exception e)
        {
        }
    }
    public static SettingsData Load()
    {
        SettingsData sd = null;
        string dataPath = string.Format("{0}/settings.json", Application.persistentDataPath);

        try
        {
            if (File.Exists(dataPath))
            { 
                var jsonstring = File.ReadAllText(dataPath);
                sd = JsonUtility.FromJson<SettingsData>(jsonstring);
            }
        }
        catch (Exception e)
        {
        }

        return sd;
    }
    public static void InitSaveFile(SettingsData sd)
    {
        string dataPath = string.Format("{0}/settings.json", Application.persistentDataPath);
        try
        { 
            if (!File.Exists(dataPath))
            { 
                var json = JsonUtility.ToJson(sd);
                File.WriteAllText(dataPath, json);
            }
        }
        catch (Exception e)
        {
        }
    }

}