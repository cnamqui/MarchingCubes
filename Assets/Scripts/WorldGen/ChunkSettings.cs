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
    public int chunkSize = 48;
    public int seed = 0;
    public int viewRadius = 12; //in units  
    public float maxOverworldHeight = 180.23f;
    public int underworldDrawClamp = 5;//in chunks;  
    public int overworldStartAt = 0;
    public float chunkScale = 1f;
    public int softLockProcGenRadius = 5;
    public AsyncRenderMode asyncRenderMode = AsyncRenderMode.GPU;
    public int voxelProcessingThrottle = 10;
    public int proceduralMeshProcessingThrottle = 6;
}


[Serializable]
public enum AsyncRenderMode
{
    GPU,
    CPU
}