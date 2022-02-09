using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class MeshStore : MonoBehaviour
{
    private Dictionary<int3, SerializableMeshData> _meshCache;
    private int chunkSize; 

    protected virtual void Awake()
    {
        _meshCache = new Dictionary<int3, SerializableMeshData>();
    }
    public virtual bool DoesMeshDataExistAt(int3 chunkCoordinate)
    {
        if (_meshCache.ContainsKey(chunkCoordinate)) return true;
        //TODO: check files for mesh data
        return false;
    }
    public virtual bool TryGetMesh(int3 chunkCoordinate, out SerializableMeshData voxelData)
    {
        return _meshCache.TryGetValue(chunkCoordinate, out voxelData);
    }
    public void AddMesh(int3 chunkCoordinate, SerializableMeshData data)
    {
        if (!DoesMeshDataExistAt(chunkCoordinate))
        {
            AddMeshUnchecked(chunkCoordinate, data);
        }
    }

    public void AddMeshUnchecked(int3 chunkCoordinate, SerializableMeshData data)
    {
        _meshCache.Add(chunkCoordinate, data);
        // TODO: Save mesh to file

    }

}