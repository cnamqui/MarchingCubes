using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshBuilder : System.IDisposable
{
    public Mesh Mesh => _mesh;
    ComputeShader marchingCubesCompute;
    ComputeBuffer counterBuffer;
    ComputeBuffer densityBuffer; 
    Mesh _mesh;
    GraphicsBuffer _vertexBuffer;
    GraphicsBuffer _indexBuffer; 
    ChunkData chunk;
    ChunkSettings settings;
    VoxelData voxelData;

    int _maxTriangles;
    int _threadGroupsX;

    public MeshBuilder(ChunkData chunk, VoxelData voxelData, ChunkSettings settings, ComputeShader compute)
    {
        Initialize(chunk, voxelData, settings, compute);
    }
     

    public void Dispose() => ReleaseAll();

    public void Build()
    {
        MarchCubes();
    }
     

    void Initialize(ChunkData chunk, VoxelData voxelData, ChunkSettings settings, ComputeShader compute)
    { 
        marchingCubesCompute = compute;
        this.chunk = chunk;
        this.settings = settings;
        this.voxelData = voxelData;
         
        _maxTriangles = settings.chunkSize * settings.chunkSize * settings.chunkSize * 5; 
        _threadGroupsX = Mathf.CeilToInt((settings.chunkSize + 1) * (settings.chunkSize + 1) * (settings.chunkSize + 1) / 128f);

        AllocateBuffers();
        AllocateMesh();
    }

    void ReleaseAll()
    {
        ReleaseBuffers();
        ReleaseMesh();
    }

    void MarchCubes()
    {
        counterBuffer.SetCounterValue(0);

        /* 
            StructuredBuffer<float> densityGrid;
            RWByteAddressBuffer vertices;
            RWByteAddressBuffer triangleIndices;
            RWStructuredBuffer<uint> triangleCounter; // used only for counting 
            int chunkWidth;
            int chunkHeight;
            int chunkDepth;
            float isoLevel;
            int maxTriangles;
        */

        // Isosurface reconstruction 

        var kernel = marchingCubesCompute.FindKernel("CSMain");
        
        marchingCubesCompute.SetInt("maxTriangles", _maxTriangles); 
        marchingCubesCompute.SetFloat("isoLevel", settings.isoLevel); 
        marchingCubesCompute.SetBuffer(kernel, "densityGrid", densityBuffer);
        marchingCubesCompute.SetBuffer(kernel, "vertices", _vertexBuffer);
        marchingCubesCompute.SetBuffer(kernel, "triangleIndices", _indexBuffer);
        marchingCubesCompute.SetBuffer(kernel, "triangleCounter", counterBuffer);
        marchingCubesCompute.Dispatch(kernel, _threadGroupsX, 1, 1);

        //clear unused area of the buffers.
        marchingCubesCompute.SetBuffer(1, "vertices", _vertexBuffer);
        marchingCubesCompute.SetBuffer(1, "triangleIndices", _indexBuffer);
        marchingCubesCompute.SetBuffer(1, "triangleCounter", counterBuffer);
        marchingCubesCompute.Dispatch(1, 1, 1, 1);

        // Bounding box
        var ext = new Vector3(settings.chunkSize, settings.chunkSize, settings.chunkSize);
        _mesh.bounds = new Bounds(Vector3.zero, ext); 

    }


    void AllocateBuffers()
    { 
        // Buffer for triangle counting
        counterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
        densityBuffer = new ComputeBuffer(voxelData.density.Length, sizeof(float));
    }

    void ReleaseBuffers()
    { 
        counterBuffer.Dispose();
        densityBuffer.Dispose();
    }
    void AllocateMesh()
    {
        int vertexCount = _maxTriangles * 3;
        _mesh = new Mesh();

        // https://docs.unity3d.com/2021.2/Documentation/ScriptReference/Mesh-vertexBufferTarget.html
        _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw; 
        var vp = new VertexAttributeDescriptor
          (VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
         
        var vn = new VertexAttributeDescriptor
          (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

        //var vads = new VertexAttributeDescriptor[] { vp, vn }; 


        _mesh.SetVertexBufferParams(vertexCount, vp,vn);
        _mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);

        // Submesh initialization
        _mesh.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount),
                         MeshUpdateFlags.DontRecalculateBounds);

        // GraphicsBuffer references
        _vertexBuffer = _mesh.GetVertexBuffer(0); 
        _indexBuffer = _mesh.GetIndexBuffer();
    }

    void ReleaseMesh()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        Object.Destroy(_mesh);
    }

}