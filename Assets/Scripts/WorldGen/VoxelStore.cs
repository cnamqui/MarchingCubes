using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class VoxelStore : MonoBehaviour
{
    private Dictionary<int3, VoxelData> _voxels;
    private int chunkSize;

    public VoxelStore(int chunkSize)
    {
        this.chunkSize = chunkSize;
    }

    public IEnumerable<VoxelData> Voxels => _voxels.Values;

    protected virtual void Awake()
    {
        _voxels = new Dictionary<int3, VoxelData>();
    }
    public virtual bool DoesVoxelDataExistAt(int3 chunkCoordinate)
    {
        return _voxels.ContainsKey(chunkCoordinate);
    }
    public virtual bool TryGetVoxel(int3 chunkCoordinate, out VoxelData voxelData)
    {
        return _voxels.TryGetValue(chunkCoordinate, out voxelData);
    } 
    public void AddVoxel(int3 chunkCoordinate, VoxelData data)
    {
        if (!DoesVoxelDataExistAt(chunkCoordinate))
        {
            AddVoxelUnchecked(chunkCoordinate, data);
        }
    }

    public void AddVoxelUnchecked(int3 chunkCoordinate, VoxelData data)
    {
        _voxels.Add(chunkCoordinate, data);
    } 

}