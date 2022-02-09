using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using Unity.Mathematics;

public class ChunkUpdater : MonoBehaviour
{
     
    void Start()
    {
        
    }
     
    void Update()
    {
        foreach (ChunkData chunk in ChunkManager.Instance.chunkStore.Chunks)
        {
            if (chunk.hasChanges)
            {
                ChunkManager.Instance.meshFactory.GenerateMeshAndUpdateChunk(chunk);
            }
        }
    }

} 
