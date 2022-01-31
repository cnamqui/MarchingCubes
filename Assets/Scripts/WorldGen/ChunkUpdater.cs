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
    
    [SerializeField] ComputeShader marchingCubesCompute;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (ChunkData chunk in ChunkManager.Instance.chunkStore.Chunks)
        {
            if (chunk.hasChanges)
            {
                GenerateMesh(chunk);
            }
        }
    }
    public void GenerateMesh2(ChunkData chunk)
    {
        if (ChunkManager.Instance.voxelStore.TryGetVoxel(chunk.chunkCoord, out VoxelData voxelData))
        {
            // March Cubes

            ChunkSettings settings = ChunkManager.Instance.settings;


            int maxTriangles = settings.chunkSize * settings.chunkSize * settings.chunkSize * 5;
            int vertexStride = sizeof(float) * 6;
            ComputeBuffer vertexBuffer = new ComputeBuffer(maxTriangles * 3, vertexStride, ComputeBufferType.Append);
            //ComputeBuffer triangleIndexBuffer = new ComputeBuffer(maxTriangles * 3, sizeof(int));
            ComputeBuffer dgridBuffer = new ComputeBuffer(voxelData.density.Length, sizeof(float));
            ComputeBuffer vertCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
 

            dgridBuffer.SetData(voxelData.density);

            vertexBuffer.SetCounterValue(0); 

            marchingCubesCompute.SetBuffer(0, "verts", vertexBuffer);
            //marchingCubesCompute.SetBuffer(0, "triangleIndices", triangleIndexBuffer);

            marchingCubesCompute.SetBuffer(0, "densityGrid", dgridBuffer);
            marchingCubesCompute.SetInt("chunkWidth", settings.chunkSize);
            marchingCubesCompute.SetInt("chunkHeight", settings.chunkSize);
            marchingCubesCompute.SetInt("chunkDepth", settings.chunkSize);
            marchingCubesCompute.SetFloat("isoLevel", settings.isoLevel);

            int threadGroupsX = Mathf.CeilToInt((settings.chunkSize + 1) * (settings.chunkSize + 1) * (settings.chunkSize + 1) / 128f);
            marchingCubesCompute.Dispatch(0, threadGroupsX, 1, 1);

            ComputeBuffer.CopyCount(vertexBuffer, vertCountBuffer, 0);

            int[] vertCountData = new int[1];
            vertCountBuffer.GetData(vertCountData);
            int vertCount = vertCountData[0];


            //AsyncGPUReadback.Request(vertCountBuffer, request => OnTriCountReceived(request, triangleBuffer, vertCountBuffer, Time.frameCount));


            dgridBuffer.Dispose();


            // Update Mesh
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);

            mesh.SetVertexBufferParams(vertCount, new VertexAttributeDescriptor[] {new VertexAttributeDescriptor(VertexAttribute.Position),
            new VertexAttributeDescriptor(VertexAttribute.Normal) });
            mesh.SetIndexBufferParams(vertCount, IndexFormat.UInt32);

            Vertex[] verts = new Vertex[vertCount];
            int[] triangleIndices = Enumerable.Range(0, vertCount).ToArray();//new int[vertCount];
            vertexBuffer.GetData(verts, 0, 0, vertCount);
            //triangleIndexBuffer.GetData(triangleIndices);


            var c = verts.Count(v => v.vert.z == 0);


            //mesh.vertices = verts.Select(v => v.vert.ToVector3()).ToArray();
            //mesh.normals = verts.Select(v => v.normal.ToVector3()).ToArray();
            //mesh.triangles = triangleIndices;
            mesh.SetVertexBufferData(verts, 0, 0, vertCount, 0, MeshUpdateFlags.DontValidateIndices);
            mesh.SetIndexBufferData(triangleIndices, 0, 0, vertCount, MeshUpdateFlags.DontValidateIndices);


            vertexBuffer.Dispose();
            //triangleIndexBuffer.Dispose();
            vertCountBuffer.Dispose();


            mesh.subMeshCount = 1;
            subMesh.indexCount = vertCount;
            mesh.SetSubMesh(0, subMesh);


            mesh.RecalculateBounds();
            //mesh.RecalculateNormals();
            //mesh.Optimize();

            chunk.meshFilter.sharedMesh = mesh;
            chunk.meshCollider.sharedMesh = mesh;

            chunk.meshCollider.enabled = true;
            chunk.meshRenderer.enabled = true;

            chunk.hasChanges = false;

            chunk.hasMesh = true;
        }
    }

    public void GenerateMesh(ChunkData chunk)
    {
        if (ChunkManager.Instance.voxelStore.TryGetVoxel(chunk.chunkCoord, out VoxelData voxelData))
        {
            // March Cubes 

            ChunkSettings settings = ChunkManager.Instance.settings;

            int maxTriangles = settings.chunkSize * settings.chunkSize * settings.chunkSize * 5;
            int triStride = sizeof(float) * 18;
            //int vertStride = sizeof(float) * 7;
            Triangle[] _triangles;
            ComputeBuffer triangleBuffer = new ComputeBuffer(maxTriangles, triStride, ComputeBufferType.Append);
            //ComputeBuffer vertBuffer = new ComputeBuffer(maxTriangles * 3, vertStride);
            ComputeBuffer dgridBuffer = new ComputeBuffer( voxelData.density.Length, sizeof(float));
            ComputeBuffer triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            int[] triCountData = new int[1];
            //triCountBuffer.SetData(triCountData);
            dgridBuffer.SetData(voxelData.density);

            triangleBuffer.SetCounterValue(0);


            marchingCubesCompute.SetBuffer(0, "triangles", triangleBuffer);
            //marchingCubesCompute.SetBuffer(0, "verts", vertBuffer);

            marchingCubesCompute.SetBuffer(0, "densityGrid", dgridBuffer);
            marchingCubesCompute.SetInt("chunkWidth", settings.chunkSize);
            marchingCubesCompute.SetInt("chunkHeight", settings.chunkSize);
            marchingCubesCompute.SetInt("chunkDepth", settings.chunkSize);
            marchingCubesCompute.SetFloat("isoLevel", settings.isoLevel);

            int threadGroupsX = Mathf.CeilToInt((settings.chunkSize + 1) * (settings.chunkSize + 1) * (settings.chunkSize + 1) / 128f); 
            marchingCubesCompute.Dispatch(0, threadGroupsX, 1, 1);

            ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
            triCountBuffer.GetData(triCountData);
            _triangles = new Triangle[triCountData[0]];
            triangleBuffer.GetData(_triangles, 0, 0, triCountData[0]);





            dgridBuffer.Dispose();


            // Update Mesh
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);

            //mesh.SetVertexBufferParams(vertCount, new VertexAttributeDescriptor[] {new VertexAttributeDescriptor(VertexAttribute.Position),
            //new VertexAttributeDescriptor(VertexAttribute.Normal) });
            //mesh.SetIndexBufferParams(vertCount, IndexFormat.UInt32);

            //Vertex[] verts = new Vertex[vertCount];
            //int[] triangleIndices = Enumerable.Range(0, vertCount).ToArray();//new int[vertCount];
            //vertexBuffer.GetData(verts, 0, 0, vertCount);
            //triangleIndexBuffer.GetData(triangleIndices);


            var trianglesRes = _triangles.Aggregate(new MarchComputeDestruct(), (acc, val) =>
            {
                var vertCount = acc.vertices.Count;
                acc.vertices.Add(val.vert1);
                acc.vertices.Add(val.vert2);
                acc.vertices.Add(val.vert3);
                acc.normals.Add(val.normal1);
                acc.normals.Add(val.normal2);
                acc.normals.Add(val.normal3);
                acc.triIndices.Add(vertCount + 0);
                acc.triIndices.Add(vertCount + 1);
                acc.triIndices.Add(vertCount + 2);
                return acc;
            });




            mesh.vertices = trianglesRes.vertices.ToArray();
            mesh.triangles = trianglesRes.triIndices.ToArray();
            mesh.normals = trianglesRes.normals.ToArray();
            //mesh.SetVertexBufferData(verts, 0, 0, 3, 0, MeshUpdateFlags.DontValidateIndices);
            //mesh.SetIndexBufferData(triangleIndices, 0, 0, 3, MeshUpdateFlags.DontValidateIndices); 


            //vertexBuffer.Dispose();
            //triangleIndexBuffer.Dispose(); 
            //vertCountBuffer.Dispose();


            mesh.subMeshCount = 1;
            subMesh.indexCount = triCountData[0] *3;
            mesh.SetSubMesh(0, subMesh);

            triangleBuffer.Dispose();
            triCountBuffer.Dispose();


            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.Optimize();

            chunk.meshFilter.sharedMesh = mesh;
            chunk.meshCollider.sharedMesh = mesh;

            chunk.meshCollider.enabled = true;
            chunk.meshRenderer.enabled = true;

            chunk.hasChanges = false;

            chunk.hasMesh = true;
        }
    }
}
