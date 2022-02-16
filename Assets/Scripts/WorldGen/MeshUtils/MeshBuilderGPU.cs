using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshBuilderGPU : IAsyncMeshBuilder, IMeshBuilder
{ 
    public bool isAsyncBuildDone { get; set; } 
    public Mesh asyncMeshResult { get; private set; }
    private bool _asyncHasError;

    ComputeShader marchingCubesCompute;
    int3 coord; 
    VoxelData voxelData;

    //Buffers 
    ComputeBuffer vertBuffer;
    ComputeBuffer triangleIndexBuffer;
    ComputeBuffer triangleCountBuffer;
    ComputeBuffer dgridBuffer;
    ComputeBuffer triCountBuffer;


    public MeshBuilderGPU(int3 coord, VoxelData voxelData, ComputeShader compute)
    {
        this.marchingCubesCompute = compute;
        this.coord = coord;
        this.voxelData = voxelData; 
        this.isAsyncBuildDone = false;
    }
    public  IEnumerator StartBuild()
    {
        isAsyncBuildDone = false;
        // March Cubes  
        ChunkSettings settings = ChunkManager.Instance.settings;
        AllocateBuffers(settings);

        int threadGroupsX = Mathf.CeilToInt((settings.chunkSize + 1) * (settings.chunkSize + 1) * (settings.chunkSize + 1) / 128f);
        marchingCubesCompute.Dispatch(0, threadGroupsX, 1, 1);

        ComputeBuffer.CopyCount(triangleCountBuffer, triCountBuffer, 0); 
        var triCountReq = AsyncGPUReadback.Request(triCountBuffer); 
        yield return new WaitUntil(() =>triCountReq.done || triCountReq.hasError);

        if (triCountReq.hasError)
        {
            CleanupAsyncBuild(true);
            Debug.Log($"ERROR: triCountReq");
            
            yield break;
        }
        var vertCountData = triCountReq.GetData<int>();
        int vertCount = vertCountData[0] * 3;

        var vertRequest = AsyncGPUReadback.Request(vertBuffer);
        var triIndexRequest = AsyncGPUReadback.Request(triangleIndexBuffer);

        //Initialize the mesh


        Mesh mesh = AllocateMesh(vertCount);

        //result is only available for one frame so we need to copy right after the result is available;
        //check whether one or the other is complete 
        yield return new WaitUntil(() =>  vertRequest.done || triIndexRequest.done   || vertRequest.hasError || triIndexRequest.hasError);
        if (vertRequest.hasError || triIndexRequest.hasError)
        {
            if(vertRequest.hasError) Debug.Log($"ERROR: vertRequest");
            if(triIndexRequest.hasError) Debug.Log($"ERROR: triIndexRequest");
            
            CleanupAsyncBuild(true);
            yield break;
        }
        //To Test: not sure whether they will ever complete at the same frame.
        //currently we assume only one is complete and we will wait for the other one.
        if (vertRequest.done && !triIndexRequest.done)
        {
            var verts = vertRequest.GetData<Vertex>();
            SetMeshVertices(mesh, vertCount, verts);
            yield return new WaitUntil(() =>  triIndexRequest.done || triIndexRequest.hasError);
            if ( triIndexRequest.hasError)
            { 
                if (triIndexRequest.hasError) Debug.Log($"ERROR: triIndexRequest");
                CleanupAsyncBuild(true);
                yield break;
            }
            var triIndices = triIndexRequest.GetData<int>();
            SetMeshTriangleIndices(mesh, vertCount, triIndices);
        } else if (triIndexRequest.done && !vertRequest.done)
        { 
            var triIndices = triIndexRequest.GetData<int>();
            SetMeshTriangleIndices(mesh, vertCount, triIndices);
            yield return new WaitUntil(() => vertRequest.done || vertRequest.hasError);
            if (vertRequest.hasError)
            {
                if (vertRequest.hasError) Debug.Log($"ERROR: vertRequest");
                CleanupAsyncBuild(true);
                
                yield break;
            }
            var verts = vertRequest.GetData<Vertex>();
            SetMeshVertices(mesh, vertCount, verts);
        }
        else// just in case they are both done
        {
            var verts = vertRequest.GetData<Vertex>();
            SetMeshVertices(mesh, vertCount, verts);
            var triIndices = triIndexRequest.GetData<int>();
            SetMeshTriangleIndices(mesh, vertCount, triIndices);
        }
        CleanupAsyncBuild(false); 
        yield return null;
        FinalizeMesh(mesh,vertCount);
        asyncMeshResult = mesh;
 
        isAsyncBuildDone = true;
    } 
    void CleanupAsyncBuild(bool hasErrors)
    {
        _asyncHasError = hasErrors;  
        isAsyncBuildDone = true;
        DisposeBuffers();
    }
    void DisposeBuffers()
    {
        vertBuffer?.Dispose();
        triangleIndexBuffer?.Dispose();
        triangleCountBuffer?.Dispose();
        dgridBuffer?.Dispose();
        triCountBuffer?.Dispose(); 
    }

    void SetMeshVertices(Mesh mesh, int vertCount, NativeArray<Vertex> vertices)
    {
        mesh.SetVertexBufferData(vertices, 0, 0, vertCount, 0, MeshUpdateFlags.DontValidateIndices); 
    }
    void SetMeshTriangleIndices(Mesh mesh, int vertCount, NativeArray<int> triIndices)
    { 
        mesh.SetIndexBufferData(triIndices, 0, 0, vertCount, MeshUpdateFlags.DontValidateIndices);
    }

    public IEnumerator StartBuildAndUpdate(ChunkData chunk)
    {
        yield return this.StartBuild();
        if(_asyncHasError) yield break;
        ChunkMeshHelper.UpdateChunkMesh(chunk, asyncMeshResult);
        //chunk.meshFilter.sharedMesh = asyncMeshResult;
        //chunk.meshCollider.sharedMesh = asyncMeshResult;

        //chunk.meshCollider.enabled = true;
        //chunk.meshRenderer.enabled = true;

        //chunk.hasChanges = false;

        //chunk.hasMesh = true;
    }

    public Mesh Build()
    {
        ChunkSettings settings = ChunkManager.Instance.settings;
        AllocateBuffers(settings);

        int threadGroupsX = Mathf.CeilToInt((settings.chunkSize + 1) * (settings.chunkSize + 1) * (settings.chunkSize + 1) / 128f);
        marchingCubesCompute.Dispatch(0, threadGroupsX, 1, 1);

        ComputeBuffer.CopyCount(triangleCountBuffer, triCountBuffer, 0);
        var triCountReq = AsyncGPUReadback.Request(triCountBuffer);

        var vertCountData = new int[1];
        triCountBuffer.GetData(vertCountData);
        int vertCount = vertCountData[0] * 3;
         
        //Initialize the mesh
         
        Mesh mesh = AllocateMesh(vertCount);
        Vertex[] vertices = new Vertex[vertCount];
        vertBuffer.GetData(vertices,0,0,vertCount);
        int[] triIndices = new int[vertCount];
        triangleIndexBuffer.GetData(triIndices, 0, 0, vertCount);

        mesh.SetIndexBufferData(triIndices, 0, 0, vertCount, MeshUpdateFlags.DontValidateIndices); 
        mesh.SetVertexBufferData(vertices, 0, 0, vertCount, 0, MeshUpdateFlags.DontValidateIndices);  
        CleanupAsyncBuild(false); 
        FinalizeMesh(mesh, vertCount);

        return mesh;
    }

    void AllocateBuffers(ChunkSettings settings)
    { 
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
    }

    Mesh AllocateMesh(int vertCount)
    {
        var mesh = new Mesh();
        //SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);

        mesh.SetVertexBufferParams(vertCount, new VertexAttributeDescriptor[] {new VertexAttributeDescriptor(VertexAttribute.Position),
        new VertexAttributeDescriptor(VertexAttribute.Normal) });
        mesh.SetIndexBufferParams(vertCount, IndexFormat.UInt32);
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        return mesh;
    }
    void FinalizeMesh(Mesh mesh, int vertCount)
    {
        SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);
        mesh.subMeshCount = 1;
        subMesh.indexCount = vertCount;
        mesh.SetSubMesh(0, subMesh); 
        //var ext = new Vector3(settings.chunkSize, settings.chunkSize, settings.chunkSize);
        //mesh.bounds = new Bounds(Vector3.zero, ext);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        //mesh.Optimize();
    }
}