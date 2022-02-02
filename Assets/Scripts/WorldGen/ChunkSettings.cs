using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class ChunkSettings  
{
    public NoiseSettings densitySettings;
    public NoiseSettings heightMapSettings; 
    public bool overlayHeightmapOverDensity = false;
    public float isoLevel = 0.5f;
    public int chunkSize = 96;
    public int seed = 0;
    public int viewRadius = 6; //in units  
    public float maxOverworldHeight;
    public int underworldDrawClamp;//in chunks;  
    public int overworldStartAt = 0;
    public float chunkScale = 1f;
}
