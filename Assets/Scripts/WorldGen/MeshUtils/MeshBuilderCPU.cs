using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshBuilderCPU : IAsyncMeshBuilder, IMeshBuilder
{  
    int3 coord; 
    VoxelData voxelData;
    public bool isAsyncBuildDone { get; set; }
    public Mesh asyncMeshResult { get; private set; }

    Mesh _mesh;
     
    public MeshBuilderCPU(int3 coord, VoxelData voxelData )
    { 
        this.coord = coord;
        this.voxelData = voxelData; 
        this.isAsyncBuildDone = false;
    } 
    public Mesh Build()
    { 
        var job = CreateJob();
        var jobHandler = job.Schedule(); 
        jobHandler.Complete();
        var mesh = CreateMeshFromJobResults(job); 
        JobCleanup(job);
        return mesh;
    }

    public IEnumerator StartBuild()
    {
        isAsyncBuildDone = false; 
        var job = CreateJob(); 
        var jobHandler = job.Schedule();

        yield return new WaitUntil(() => jobHandler.IsCompleted);
        jobHandler.Complete();
        asyncMeshResult = CreateMeshFromJobResults(job);
        isAsyncBuildDone = true;

        JobCleanup(job); 
    }

    public IEnumerator StartBuildAndUpdate(ChunkData chunk)
    {
        yield return this.StartBuild(); 
        ChunkMeshHelper.UpdateChunkMesh(chunk, asyncMeshResult);
        //chunk.meshFilter.sharedMesh = asyncMeshResult;
        //chunk.meshCollider.sharedMesh = asyncMeshResult;

        //chunk.meshCollider.enabled = true;
        //chunk.meshRenderer.enabled = true;

        //chunk.hasChanges = false;

        //chunk.hasMesh = true;
    }

    MarchCubesJob CreateJob()
    {
        ChunkSettings settings = ChunkManager.Instance.settings;

        int maxTriangles = settings.chunkSize * settings.chunkSize * settings.chunkSize * 5;

        NativeArray<float> grid = new NativeArray<float>(voxelData.density.Length, Allocator.TempJob);
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
        return job;
    }
    void JobCleanup(MarchCubesJob job)
    {
        job.grid.Dispose();
        job.triangleIndices.Dispose();
        job.vertices.Dispose();
        job.triCounter.Dispose();
    }

    Mesh CreateMeshFromJobResults( MarchCubesJob job)
    {
        var vertCount = job.triCounter.Count * 3;
        Mesh mesh = new Mesh();
        SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);

        mesh.SetVertexBufferParams(vertCount, new VertexAttributeDescriptor[] {new VertexAttributeDescriptor(VertexAttribute.Position),
        new VertexAttributeDescriptor(VertexAttribute.Normal) });
        mesh.SetIndexBufferParams(vertCount, IndexFormat.UInt32);
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertexBufferData(job.vertices, 0, 0, vertCount, 0, MeshUpdateFlags.DontValidateIndices);
        mesh.SetIndexBufferData(job.triangleIndices, 0, 0, vertCount, MeshUpdateFlags.DontValidateIndices);
         
        mesh.subMeshCount = 1;
        subMesh.indexCount = vertCount;
        mesh.SetSubMesh(0, subMesh);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();
        return mesh;
    }
}