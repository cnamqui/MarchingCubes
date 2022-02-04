using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshBuilderCPUAsync : QueuedMeshBuilder
{ 
    ComputeShader marchingCubesCompute;
    int3 coord;
    ChunkData chunk;
    VoxelData voxelData;
     
    public MeshBuilderCPUAsync(int3 coord, ChunkData chunk, VoxelData voxelData )
    { 
        this.coord = coord;
        this.voxelData = voxelData;
        this.chunk = chunk;
        this.done = false;
    }
    public override IEnumerator Build()
    {
        done = false;
        // March Cubes  
        ChunkSettings settings = ChunkManager.Instance.settings;
         
        int maxTriangles = settings.chunkSize * settings.chunkSize * settings.chunkSize * 5;

        NativeArray<float> grid = new NativeArray<float>(voxelData.density.Length,Allocator.TempJob);
        NativeArray<uint> triangleIndices = new NativeArray<uint>(maxTriangles * 3, Allocator.TempJob);
        NativeArray<Vertex> vertices = new NativeArray<Vertex>(maxTriangles * 3, Allocator.TempJob);
        NativeCounter triCounter = new NativeCounter(Allocator.TempJob);
        grid.CopyFrom(voxelData.density);
        var job = new MarchCubesJob()
        {
            depth = settings.chunkSize,
            width = settings.chunkSize,
            height = settings.chunkSize,
            grid = grid,
            isoLevel = settings.isoLevel,
            triangleIndices = triangleIndices,
            vertices = vertices,
            triCounter = triCounter
        };

        var jobHandler = job.Schedule();

        yield return new WaitUntil(() => jobHandler.IsCompleted);
        jobHandler.Complete();
        var vertCount = job.triCounter.Count * 3;

        //// Update Mesh
        Mesh mesh = new Mesh();
        SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);

        mesh.SetVertexBufferParams(vertCount, new VertexAttributeDescriptor[] {new VertexAttributeDescriptor(VertexAttribute.Position),
        new VertexAttributeDescriptor(VertexAttribute.Normal) });
        mesh.SetIndexBufferParams(vertCount, IndexFormat.UInt32);
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertexBufferData(job.vertices, 0, 0, vertCount, 0, MeshUpdateFlags.DontValidateIndices);
        mesh.SetIndexBufferData(job.triangleIndices, 0, 0, vertCount, MeshUpdateFlags.DontValidateIndices);

        yield return new WaitForEndOfFrame();
        mesh.subMeshCount = 1;
        subMesh.indexCount = vertCount;
        mesh.SetSubMesh(0, subMesh);


        //var ext = new Vector3(settings.chunkSize, settings.chunkSize, settings.chunkSize);
        //mesh.bounds = new Bounds(Vector3.zero, ext);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();

        chunk.meshFilter.sharedMesh = mesh;
        chunk.meshCollider.sharedMesh = mesh;

        chunk.meshCollider.enabled = true;
        chunk.meshRenderer.enabled = true;

        chunk.hasChanges = false;

        chunk.hasMesh = true;
        done = true;

        grid.Dispose();
        triangleIndices.Dispose();
        vertices.Dispose();
        triCounter.Dispose();

    }
    void DisposeBuffers()
    {
        //vertBuffer?.Dispose();
        //triangleIndexBuffer?.Dispose();
        //triangleCountBuffer?.Dispose();
        //dgridBuffer?.Dispose();
        //triCountBuffer?.Dispose(); 
    }
}