using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class ChunkStore :MonoBehaviour { 
    private Dictionary<int3, ChunkData> _chunks; 

    public IEnumerable<ChunkData> Chunks => _chunks.Values; 

    protected virtual void Awake()
    {
        _chunks = new Dictionary<int3, ChunkData>();
    } 
    public virtual bool DoesChunkExistAt(int3 chunkCoordinate)
    {
        return _chunks.ContainsKey(chunkCoordinate);
    } 
    public virtual bool TryGetDataChunk(int3 chunkCoordinate, out ChunkData chunkData)
    {
        return _chunks.TryGetValue(chunkCoordinate, out chunkData);
    } 
    public virtual void MoveChunk(int3 from, int3 to)
    {
        // Check that 'from' and 'to' are not equal
        if (from.Equals(to)) { return; }

        // Check that a chunk does NOT already exist at 'to'
        if (DoesChunkExistAt(to)) { return; }

        // Check that a chunk exists at 'from'
        if (TryGetDataChunk(from, out ChunkData existingChunk))
        {
            RemoveChunk(from);
            ReuseChunkForNewChunk(to, existingChunk);
        }
    }
     
    public void RemoveChunk(int3 chunkCoordinate)
    {
        _chunks.Remove(chunkCoordinate);
    }
     
    public void GenerateDataForChunk(int3 chunkCoordinate)
    {
        if (!DoesChunkExistAt(chunkCoordinate))
        {
            CreateNewChunk(chunkCoordinate);
        }
    }
     
    public void GenerateDataForChunk(int3 chunkCoordinate, ChunkData existingChunk)
    {
        if (!DoesChunkExistAt(chunkCoordinate))
        {
            ReuseChunkForNewChunk(chunkCoordinate, existingChunk);
        }
    }

    public void CreateNewChunk(int3 chunkCoordinate) {
    }

    public void ReuseChunkForNewChunk(int3 chunkCoordinate, ChunkData existingChunk) {

        existingChunk.meshCollider.enabled = false;
        existingChunk.meshRenderer.enabled = false;
        existingChunk.Initialize(chunkCoordinate, ChunkManager.Instance.settings.chunkSize, ChunkManager.Instance.settings.chunkScale);
        AddChunk(chunkCoordinate, existingChunk);
    }
     
    public void AddChunk(int3 chunkCoordinate, ChunkData data)
    {
        if (!DoesChunkExistAt(chunkCoordinate))
        {
            AddChunkUnchecked(chunkCoordinate, data);
        }
    }
     
    public void AddChunkUnchecked(int3 chunkCoordinate, ChunkData data)
    {
        _chunks.Add(chunkCoordinate, data);
    } 
    public IEnumerable<int3> GetChunkCoordinatesOutsideOfRange(int3 coordinate, int range)
    { 
        var settings = ChunkManager.Instance.settings;
        float maxOverworldHeight = settings.maxOverworldHeight;
        int overworldStartAt = settings.overworldStartAt;
        int topChunk = Mathf.CeilToInt(maxOverworldHeight / settings.chunkSize) + overworldStartAt;
        int underworldDrawClamp = settings.underworldDrawClamp;

        foreach (int3 chunkCoordinate in _chunks.Keys )
        {
            var distance = math.abs(math.distance(coordinate, chunkCoordinate));
            var underworldDistanceLowerThanClamp = coordinate.y - chunkCoordinate.y >underworldDrawClamp;
            var chunkHigherThanHighestPoint = chunkCoordinate.y > topChunk;
            if(distance > (float)range || underworldDistanceLowerThanClamp || chunkHigherThanHighestPoint)
            {
                yield return chunkCoordinate;
            }
        }
    }  

}