using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class VoxelData 
{
    public float[] density; 
    public int size;
    public bool hasDensity; 
    public int3 chunkCoord { get; set; }
    public VoxelData(int size, int3 coord)
    {
        this.size = size;
        int densitySize = (size + 1) * (size + 1) * (size + 1);
        density = new float[ densitySize];
        hasDensity = false;
        chunkCoord = coord;
    }

    public float GetDensityAt(int3 localCoord)
    {
        int idx = localCoord.x + localCoord.y * (size + 1)  + localCoord.z * (size + 1) * (size + 1);
        return density[idx];
    }
    public float GetDensityAt(int x, int y, int z)
    {
        return GetDensityAt(new int3(x,y,z));
    }

}
