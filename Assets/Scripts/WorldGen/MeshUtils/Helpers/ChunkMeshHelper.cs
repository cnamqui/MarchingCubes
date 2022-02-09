using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class ChunkMeshHelper
{
    public static void UpdateChunkMesh(ChunkData chunk, Mesh mesh)
    {
        chunk.meshFilter.sharedMesh = mesh;
        chunk.meshCollider.sharedMesh = mesh;

        chunk.meshCollider.enabled = true;
        chunk.meshRenderer.enabled = true;

        chunk.hasChanges = false;

        chunk.hasMesh = true;
    }
}
 
