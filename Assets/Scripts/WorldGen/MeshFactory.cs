using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshFactory : MonoBehaviour
{

    [SerializeField] ComputeShader marchingCubesCompute;
    [SerializeField] ComputeShader marchingCubesComputeMeshDirect;
    [SerializeField] ComputeShader marchingCubesCompute2;

    // Start is called before the first frame update
    void Start()
    { 
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public Mesh GenerateMesh(ChunkData chunk)
    {
        if (ChunkManager.Instance.voxelStore.TryGetVoxel(chunk.chunkCoord, out VoxelData voxelData))
        {
            if (ChunkManager.Instance.settings.asyncRenderMode == AsyncRenderMode.CPU)
            {
                return GenerateMeshCPU(chunk, voxelData);
            }
            return GenerateMeshGPU(chunk, voxelData);
        }
        return null;
    }

    public void GenerateMeshAndUpdateChunk(ChunkData chunk)
    {
        var mesh =  GenerateMesh(chunk);
        if(mesh != null)
        { 
            ChunkMeshHelper.UpdateChunkMesh(chunk, mesh );
        }
    }


    void GenerateMeshViaMeshApi(ChunkData chunk)
    {
        if (ChunkManager.Instance.voxelStore.TryGetVoxel(chunk.chunkCoord, out VoxelData voxelData))
        {
            // March Cubes

            ChunkSettings settings = ChunkManager.Instance.settings;

            var _builder = new MeshBuilderMA(chunk, voxelData, ChunkManager.Instance.settings, marchingCubesComputeMeshDirect);

            _builder.Build();

            var mesh = _builder.Mesh;
            // Update Mesh   
            //mesh.Optimize();
            chunk.meshFilter.sharedMesh = mesh;
            //chunk.meshCollider.sharedMesh = mesh; 
            //chunk.meshCollider.enabled = true;
            chunk.meshRenderer.enabled = true;

            chunk.hasChanges = false;

            chunk.hasMesh = true;
            //_builder.Dispose();
        }
    }


    void GenerateMeshGPUOld(ChunkData chunk)
    {
        if (ChunkManager.Instance.voxelStore.TryGetVoxel(chunk.chunkCoord, out VoxelData voxelData))
        {
            // March Cubes 


            /*
                StructuredBuffer<float> densityGrid; 
                RWStructuredBuffer<Vert> verts;
                RWStructuredBuffer<uint> triangleIndices;
                RWStructuredBuffer<uint> triangleCounter; // used only for counting 
                int chunkWidth;
                int chunkHeight;
                int chunkDepth;
                float isoLevel;
            */

            ChunkSettings settings = ChunkManager.Instance.settings;

            int maxTriangles = settings.chunkSize * settings.chunkSize * settings.chunkSize * 5;
            int vertStride = sizeof(float) * 6;
            ComputeBuffer vertBuffer = new ComputeBuffer(maxTriangles * 3, vertStride);
            ComputeBuffer triangleIndexBuffer = new ComputeBuffer(maxTriangles * 3, sizeof(int));
            ComputeBuffer triangleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);
            ComputeBuffer dgridBuffer = new ComputeBuffer(voxelData.density.Length, sizeof(float));
            ComputeBuffer triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            //ComputeBuffer triCountBuffer2 = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            //triCountBuffer.SetData(triCountData);
            dgridBuffer.SetData(voxelData.density);

            triangleCountBuffer.SetCounterValue(0);
            //vertBuffer.SetCounterValue(0);
            //triangleIndexBuffer.SetCounterValue(0);

            marchingCubesCompute2.SetBuffer(0, "verts", vertBuffer);
            marchingCubesCompute2.SetBuffer(0, "triangleIndices", triangleIndexBuffer);
            marchingCubesCompute2.SetBuffer(0, "triangleCounter", triangleCountBuffer);
            marchingCubesCompute2.SetBuffer(0, "densityGrid", dgridBuffer);
            marchingCubesCompute2.SetInt("chunkWidth", settings.chunkSize);
            marchingCubesCompute2.SetInt("chunkHeight", settings.chunkSize);
            marchingCubesCompute2.SetInt("chunkDepth", settings.chunkSize);
            marchingCubesCompute2.SetFloat("isoLevel", settings.isoLevel);

            int threadGroupsX = Mathf.CeilToInt((settings.chunkSize + 1) * (settings.chunkSize + 1) * (settings.chunkSize + 1) / 128f);
            marchingCubesCompute2.Dispatch(0, threadGroupsX, 1, 1);

            int[] vertCountData = new int[1];
            int[] counterData = new int[1];
            ComputeBuffer.CopyCount(triangleCountBuffer, triCountBuffer, 0);
            triCountBuffer.GetData(vertCountData);
            //ComputeBuffer.CopyCount(triangleCountBuffer, triCountBuffer2, 0);
            //triCountBuffer2.GetData(counterData);
            var vertCount = vertCountData[0] * 3;


            // Update Mesh
            Mesh mesh = new Mesh();
            SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);

            mesh.SetVertexBufferParams(vertCount, new VertexAttributeDescriptor[] {new VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Position),
            new VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Normal) });
            mesh.SetIndexBufferParams(vertCount, IndexFormat.UInt32);
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;


            var verts = new Vertex[vertCount];
            vertBuffer.GetData(verts, 0, 0, vertCount);
            var triangleIndices = new int[vertCount];
            triangleIndexBuffer.GetData(triangleIndices, 0, 0, vertCount);
            mesh.SetVertexBufferData(verts, 0, 0, vertCount, 0, MeshUpdateFlags.DontValidateIndices);
            mesh.SetIndexBufferData(triangleIndices, 0, 0, vertCount, MeshUpdateFlags.DontValidateIndices);

            //vertexBuffer.Dispose();
            //triangleIndexBuffer.Dispose(); 
            //vertCountBuffer.Dispose();


            mesh.subMeshCount = 1;
            subMesh.indexCount = vertCount;
            mesh.SetSubMesh(0, subMesh);

            triCountBuffer.Dispose();
            //var ext = new Vector3(settings.chunkSize, settings.chunkSize, settings.chunkSize);
            // mesh.bounds = new Bounds(Vector3.zero, ext);

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.Optimize();

            chunk.meshFilter.sharedMesh = mesh;
            chunk.meshCollider.sharedMesh = mesh;

            chunk.meshCollider.enabled = true;
            chunk.meshRenderer.enabled = true;

            chunk.hasChanges = false;

            chunk.hasMesh = true;

            dgridBuffer.Dispose();

            vertBuffer.Dispose();
            triangleIndexBuffer.Dispose();
            triangleCountBuffer.Dispose();
            dgridBuffer.Dispose();
        }
    }


    public IAsyncMeshBuilder GenerateMeshAsync(ChunkData chunk)
    {
        if (ChunkManager.Instance.voxelStore.TryGetVoxel(chunk.chunkCoord, out VoxelData voxelData))
        {
            if (ChunkManager.Instance.settings.asyncRenderMode == AsyncRenderMode.CPU)
            {
                return GenerateMeshCPUAsync(chunk, voxelData);
            }
            return GenerateMeshGPUAsync(chunk,voxelData);
        }
        return null;
    }

    public IAsyncMeshBuilder GenerateMeshGPUAsync(ChunkData chunk, VoxelData voxelData)
    { 
        var builder = new MeshBuilderGPU(chunk.chunkCoord, voxelData, marchingCubesCompute2);
        StartCoroutine(builder.StartBuildAndUpdate(chunk));
        return builder; 
    }
    public IAsyncMeshBuilder GenerateMeshCPUAsync(ChunkData chunk,  VoxelData voxelData)
    { 
        var builder = new MeshBuilderCPU(chunk.chunkCoord, voxelData);
        StartCoroutine(builder.StartBuildAndUpdate(chunk));
        return builder; 
    }


    Mesh GenerateMeshGPU(ChunkData chunk, VoxelData voxelData)
    {
        var builder = new MeshBuilderGPU(chunk.chunkCoord, voxelData, marchingCubesCompute2);
        return builder.Build(); 
    }
    Mesh GenerateMeshCPU(ChunkData chunk, VoxelData voxelData)
    { 
        var builder = new MeshBuilderCPU(chunk.chunkCoord, voxelData);
        return builder.Build();
    }



}
