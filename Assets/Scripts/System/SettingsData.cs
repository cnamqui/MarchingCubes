using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsData
{
    public ChunkSettings chunkSettings;
    public PlayerSettings playerSettings;
    public SettingsData()
    {
        chunkSettings = new ChunkSettings();
        playerSettings = new PlayerSettings();
    }
}
