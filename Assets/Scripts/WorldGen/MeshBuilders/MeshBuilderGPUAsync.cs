using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshBuilderGPUAsync : QueuedMeshBuilder
{ 
    ComputeShader marchingCubesCompute;
    int3 coord;
    ChunkData chunk;
    VoxelData voxelData;

    //Buffers 
    ComputeBuffer vertBuffer;
    ComputeBuffer triangleIndexBuffer;
    ComputeBuffer triangleCountBuffer;
    ComputeBuffer dgridBuffer;
    ComputeBuffer triCountBuffer; 

    public MeshBuilderGPUAsync(int3 coord, ChunkData chunk, VoxelData voxelData, ComputeShader compute)
    {
        this.marchingCubesCompute = compute;
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
        int vertStride = sizeof(float) * 6;
        vertBuffer = new ComputeBuffer(maxTriangles * 3, vertStride);
        triangleIndexBuffer = new ComputeBuffer(maxTriangles * 3, sizeof(int));
        triangleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);
        dgridBuffer = new ComputeBuffer(voxelData.density.Length, sizeof(float));
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw); 
        //triCountBuffer.SetData(triCountData);
        dgridBuffer.SetData(voxelData.density);

        triangleCountBuffer.SetCounterValue(0);
        //vertBuffer.SetCounterValue(0);
        //triangleIndexBuffer.SetCounterValue(0);

        marchingCubesCompute.SetBuffer(0, "verts", vertBuffer);
        marchingCubesCompute.SetBuffer(0, "triangleIndices", triangleIndexBuffer);
        marchingCubesCompute.SetBuffer(0, "triangleCounter", triangleCountBuffer);
        marchingCubesCompute.SetBuffer(0, "densityGrid", dgridBuffer);
        marchingCubesCompute.SetInt("chunkWidth", settings.chunkSize);
        marchingCubesCompute.SetInt("chunkHeight", settings.chunkSize);
        marchingCubesCompute.SetInt("chunkDepth", settings.chunkSize);
        marchingCubesCompute.SetFloat("isoLevel", settings.isoLevel);

        int threadGroupsX = Mathf.CeilToInt((settings.chunkSize + 1) * (settings.chunkSize + 1) * (settings.chunkSize + 1) / 128f);
        marchingCubesCompute.Dispatch(0, threadGroupsX, 1, 1);

        ComputeBuffer.CopyCount(triangleCountBuffer, triCountBuffer, 0); 
        var triCountReq = AsyncGPUReadback.Request(triCountBuffer); 
        yield return new WaitUntil(() =>triCountReq.done || triCountReq.hasError);

        if (triCountReq.hasError)
        {
            DisposeBuffers();
            yield break;
        }
        var vertCountData = triCountReq.GetData<int>();
        int vertCount = vertCountData[0] * 3;


        var vertRequest = AsyncGPUReadback.Request(vertBuffer);
        var triIndexRequest = AsyncGPUReadback.Request(triangleIndexBuffer);

        yield return new WaitUntil(() => (vertRequest.done && triIndexRequest.done ) || vertRequest.hasError || triIndexRequest.hasError);
        if (vertRequest.hasError || triIndexRequest.hasError)
        {
            DisposeBuffers();
            yield break;
        }
        var _verts = vertRequest.GetData<Vertex>();
        var _triIndices = triIndexRequest.GetData<int>();
        DisposeBuffers();
        // Update Mesh
        Mesh mesh = new Mesh();
        SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);

        mesh.SetVertexBufferParams(vertCount, new VertexAttributeDescriptor[] {new VertexAttributeDescriptor(VertexAttribute.Position),
        new VertexAttributeDescriptor(VertexAttribute.Normal) });
        mesh.SetIndexBufferParams(vertCount, IndexFormat.UInt32);
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertexBufferData(_verts, 0, 0, vertCount, 0, MeshUpdateFlags.DontValidateIndices);
        mesh.SetIndexBufferData(_triIndices, 0, 0, vertCount, MeshUpdateFlags.DontValidateIndices);

        yield return new WaitForEndOfFrame();

        mesh.subMeshCount = 1;
        subMesh.indexCount = vertCount;
        mesh.SetSubMesh(0, subMesh);

        triCountBuffer.Dispose();


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
    } 
    void DisposeBuffers()
    {
        vertBuffer?.Dispose();
        triangleIndexBuffer?.Dispose();
        triangleCountBuffer?.Dispose();
        dgridBuffer?.Dispose();
        triCountBuffer?.Dispose(); 
    }
}