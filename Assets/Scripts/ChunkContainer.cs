using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.Rendering;
using System;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))] 
public class ChunkContainer : MonoBehaviour
{ 
    [SerializeField] ComputeShader marchingCubesCompute;
    [SerializeField] ComputeShader noiseCompute;
    public RenderTexture texture;

    public Chunky chunkData; 
    MeshRenderer mr;
    MeshFilter mf;
    MeshCollider mc;
    Mesh mesh;
    bool drawn  = false; 
    bool queuedForMarchCubes = false;
    public bool queuedForNoise = false; 



    // Start is called before the first frame update
    void Start()
    {
       //DrawChunk();
    }

    // Update is called once per frame
    void Update()
    { 
        if (!drawn && chunkData.meshUpToDate)
        {
            InitMesh();
            this.mesh = chunkData.GetMesh();
            RenderMesh();
        }
        else if (!drawn && !queuedForMarchCubes && chunkData.gridInitialized)
        {
            //QueueMarchCubes();
            MarchCubes_GPU();
        } 
        
    }
    public void Draw()
    { 
        InitMesh();
        if (chunkData.meshUpToDate)
        { 
            this.mesh = chunkData.GetMesh();
            RenderMesh(); 
        }
        else
        {
            QueueMarchCubes(); 
        }
        this.queuedForNoise = false;
    } 

    public void RedrawChunk()
    {
        this.drawn = false;
        this.queuedForMarchCubes = false;
        this.queuedForNoise = false;
    }
    void InitMesh()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
        }
        mesh.Clear();  
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; //increase max vertices per mesh
        mesh.name = "ProceduralMesh";
    }
    
    void BuildMesh()
    {
        var _start = DateTime.Now; 
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();
        chunkData.UpdateMeshData(mesh);
        Debug.Log($"Build Mesh and Cache Time: {DateTime.Now - _start}");
    }

    void RenderMesh()
    {
        var _start = DateTime.Now;
        mf = GetComponent<MeshFilter>();  
        mr = GetComponent<MeshRenderer>();  
        mc = GetComponent<MeshCollider>(); 
        if (mf == null)
            mf = this.gameObject.AddComponent<MeshFilter>();  
        if (mr == null)
            mr = this.gameObject.AddComponent<MeshRenderer>();  
        if (mc == null)
            mc = this.gameObject.AddComponent<MeshCollider>();

        if (this.texture != null)
        {
            mr.sharedMaterial.SetTexture("_MainTex", this.texture);
        }

        mf.mesh = mesh;
        mc.cookingOptions = MeshColliderCookingOptions.WeldColocatedVertices;
        mc.sharedMesh = mesh;
        // force refresh
        mc.enabled = false;
        mc.enabled = true;
        //mr.enabled = false;
        mr.enabled = true;
        //mr.material = defaultMaterial;


        this.gameObject.SetActive(true);
        drawn = true; 
        queuedForMarchCubes = false;
        Debug.Log($"Render mesh Time: {DateTime.Now - _start}");
    }
     




    /*  Cube
     *      4 ------ 5
     *      |        |
     *      | 7 ------- 6
     *      | |      |  |
     *      0 | ---- 1  |
     *        |         |
     *        3 ------- 2
    **/
    void MarchCubes_Normal()
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        var width = chunkData.chunkWidth;
        var height = chunkData.chunkHeight ;
        var depth = chunkData.chunkDepth ; 
        for (int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                for(int z = 0; z < depth; z++)
                {
                    Vector4[] cubePoints = new Vector4[8];
                    //cubePoints[0] = new Vector4(x, y, z + 1,  chunkData.GetDensityAt(x, y, z + 1));
                    //cubePoints[1] = new Vector4(x + 1, y, z + 1,  chunkData.GetDensityAt(x + 1, y, z + 1));
                    //cubePoints[2] = new Vector4(x + 1, y, z,  chunkData.GetDensityAt(x + 1, y, z));
                    //cubePoints[3] = new Vector4(x, y, z,  chunkData.GetDensityAt(x, y, z));
                    //cubePoints[4] = new Vector4(x, y + 1, z + 1,  chunkData.GetDensityAt(x, y + 1, z + 1));
                    //cubePoints[5] = new Vector4(x + 1, y + 1, z + 1,  chunkData.GetDensityAt(x + 1, y + 1, z + 1));
                    //cubePoints[6] = new Vector4(x + 1, y + 1, z,  chunkData.GetDensityAt(x + 1, y + 1, z));
                    //cubePoints[7] = new Vector4(x, y + 1, z,  chunkData.GetDensityAt(x, y + 1, z));

                    cubePoints[0] = new Vector4(x, y, z, chunkData.GetDensityAt(x, y, z));
                    cubePoints[1] = new Vector4(x + 1, y, z, chunkData.GetDensityAt(x + 1, y, z));
                    cubePoints[2] = new Vector4(x + 1, y, z + 1, chunkData.GetDensityAt(x + 1, y, z + 1));
                    cubePoints[3] = new Vector4(x, y, z + 1, chunkData.GetDensityAt(x, y, z + 1));
                    cubePoints[4] = new Vector4(x, y + 1, z, chunkData.GetDensityAt(x, y + 1, z));
                    cubePoints[5] = new Vector4(x + 1, y + 1, z, chunkData.GetDensityAt(x + 1, y + 1, z));
                    cubePoints[6] = new Vector4(x + 1, y + 1, z + 1, chunkData.GetDensityAt(x + 1, y + 1, z + 1));
                    cubePoints[7] = new Vector4(x, y + 1, z + 1, chunkData.GetDensityAt(x, y + 1, z + 1));
                     
                    int cubeIndex = 0;
                    if (cubePoints[0].w < TerrainManager.instance.isoLevel) cubeIndex |= 1;
                    if (cubePoints[1].w < TerrainManager.instance.isoLevel) cubeIndex |= 2;
                    if (cubePoints[2].w < TerrainManager.instance.isoLevel) cubeIndex |= 4;
                    if (cubePoints[3].w < TerrainManager.instance.isoLevel) cubeIndex |= 8;
                    if (cubePoints[4].w < TerrainManager.instance.isoLevel) cubeIndex |= 16;
                    if (cubePoints[5].w < TerrainManager.instance.isoLevel) cubeIndex |= 32;
                    if (cubePoints[6].w < TerrainManager.instance.isoLevel) cubeIndex |= 64;
                    if (cubePoints[7].w < TerrainManager.instance.isoLevel) cubeIndex |= 128;

                    for (int i = 0; Tables.triangulation[cubeIndex,i] != -1; i += 3)
                    {
                        int edge1Index = Tables.triangulation[cubeIndex, i];
                        int edge2Index = Tables.triangulation[cubeIndex, i + 1];
                        int edge3Index = Tables.triangulation[cubeIndex, i + 2];
                        
                        // indices of the vertex in  cubePoints[]
                        // since they're ordered the same as the diagram
                        var edge1VertexA = Tables.edgeVertices[edge1Index,0];
                        var edge1VertexB = Tables.edgeVertices[edge1Index,1];
                        var edge2VertexA = Tables.edgeVertices[edge2Index,0];
                        var edge2VertexB = Tables.edgeVertices[edge2Index,1];
                        var edge3VertexA = Tables.edgeVertices[edge3Index,0];
                        var edge3VertexB = Tables.edgeVertices[edge3Index,1];

                        // interpolate between the vertices based on the distance between their noise values

                        Vector3 vertexOnEdge1 = CubePointLerp(cubePoints[edge1VertexA], cubePoints[edge1VertexB]);
                        Vector3 vertexOnEdge2 = CubePointLerp(cubePoints[edge2VertexA], cubePoints[edge2VertexB]);
                        Vector3 vertexOnEdge3 = CubePointLerp(cubePoints[edge3VertexA], cubePoints[edge3VertexB]);
                        Vector3 normalOnPtOnEdge1 = GetNormalOnEdge(cubePoints[edge1VertexA], cubePoints[edge1VertexB]);
                        Vector3 normalOnPtOnEdge2 = GetNormalOnEdge(cubePoints[edge2VertexA], cubePoints[edge2VertexB]);
                        Vector3 normalOnPtOnEdge3 = GetNormalOnEdge(cubePoints[edge3VertexA], cubePoints[edge3VertexB]);

                        var vertCount = vertices.Count;
                        vertices.Add(vertexOnEdge1);
                        vertices.Add(vertexOnEdge2);
                        vertices.Add(vertexOnEdge3);

                        normals.Add(normalOnPtOnEdge1);
                        normals.Add(normalOnPtOnEdge2);
                        normals.Add(normalOnPtOnEdge3);

                        triangles.Add(vertCount);
                        triangles.Add(vertCount + 1);
                        triangles.Add(vertCount + 2); 
                    }

                }
            }
        } 

    }
    
    void QueueMarchCubes()
    { 
        TerrainManager.instance.QueueMarchCubeGPUJobForProcessing(MarchCubesCoroutine());
        this.queuedForMarchCubes = true;
    }
    IEnumerator MarchCubesCoroutine()
    {
        MarchCubes_GPU();
        yield return null;
    }

    internal void ClearChunkData()
    {
        chunkData = null; 
        if(mesh!=null)
            mesh.Clear();
        drawn = false;
        queuedForMarchCubes = false;
        queuedForNoise = false;
    }

    void MarchCubes_GPU()
    {
        queuedForMarchCubes = true;
        var _start = DateTime.Now;

        int maxTriangles = chunkData.chunkWidth * chunkData.chunkHeight * chunkData.chunkDepth * 5;
        int triStride = sizeof(float) * 18;
        //int vertStride = sizeof(float) * 7;
        Triangle[] triangles;
        ComputeBuffer triangleBuffer = new ComputeBuffer(maxTriangles, triStride, ComputeBufferType.Append);
        //ComputeBuffer vertBuffer = new ComputeBuffer(maxTriangles * 3, vertStride);
        ComputeBuffer dgridBuffer = new ComputeBuffer(this.chunkData.densityGrid.Length, sizeof(float));
        ComputeBuffer triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        int[] triCountData = new int[1];
        //triCountBuffer.SetData(triCountData);
        dgridBuffer.SetData(chunkData.densityGrid);

        triangleBuffer.SetCounterValue(0);


        marchingCubesCompute.SetBuffer(0, "triangles", triangleBuffer);
        //marchingCubesCompute.SetBuffer(0, "verts", vertBuffer);

        marchingCubesCompute.SetBuffer(0, "densityGrid", dgridBuffer);
        marchingCubesCompute.SetInt("chunkWidth", chunkData.chunkWidth);
        marchingCubesCompute.SetInt("chunkHeight", chunkData.chunkHeight);
        marchingCubesCompute.SetInt("chunkDepth", chunkData.chunkDepth);
        marchingCubesCompute.SetFloat("isoLevel", TerrainManager.instance.isoLevel);

        int threadGroupsX = Mathf.CeilToInt((chunkData.chunkWidth + 1) * (chunkData.chunkHeight + 1) * (chunkData.chunkDepth + 1) / 1024f);
        //int threadGroupsY = Mathf.CeilToInt((chunkData.chunkHeight + 1)/ 8f);
        //int threadGroupsZ = Mathf.CeilToInt((chunkData.chunkDepth + 1)/ 8f);
        marchingCubesCompute.Dispatch(0, threadGroupsX, 1, 1);

        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        //triCountBuffer.GetData(triCountData);
        //triangles = new Triangle[triCountData[0]];
        //triangleBuffer.GetData(triangles, 0, 0, triCountData[0]);
        Debug.Log($"March Cubes Time: {DateTime.Now - _start}");
        _start = DateTime.Now;
        //AsyncGPUReadback.Request(vertBuffer, request => OnReceiveVerts(request, dgridBuffer, vertBuffer, Time.frameCount));

        AsyncGPUReadback.Request(triCountBuffer, request => OnTriCountReceived(request, triangleBuffer,triCountBuffer, Time.frameCount ));
         
        dgridBuffer.Dispose(); 
        Debug.Log($"Now:{DateTime.Now } frame: {Time.frameCount}");
    }

    private void OnTriCountReceived(AsyncGPUReadbackRequest request, ComputeBuffer triangleBuffer, ComputeBuffer triCountBuffer, int frameCount )
    {
        if (request.hasError || // Something wrong happened
                  !Application.isPlaying) // Callback happened in edit mode afterwards
        {
            return;
        }
        var triCount = request.GetData<int>(0);
        var _triCount = 0 + triCount[0];
        //var triangles = new Triangle[triCount[0]]; 
        AsyncGPUReadback.Request(triangleBuffer,  request => OnTrianglesReceived(request, triangleBuffer, frameCount, _triCount));
        triCount.Dispose();
        triCountBuffer.Dispose();
        //triangleBuffer.Dispose();
    }

    private void OnTrianglesReceived(AsyncGPUReadbackRequest request, ComputeBuffer triangleBuffer, int frameCount, int triCount)
    {
        if (request.hasError || // Something wrong happened
                  !Application.isPlaying) // Callback happened in edit mode afterwards
        {
            return;
        }
        var triangles = request.GetData<Triangle>(0);
        var _triangles = new Triangle[triCount];
        triangles.ToArray().Take(triCount).ToArray().CopyTo(_triangles,0);
        triangleBuffer.Dispose();
        triangles.Dispose();
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
        TerrainManager.instance.QueueMarchCubeGPUJobForProcessing(OnMarchCubesCoroutine(trianglesRes));
        //OnMarchCubesGPUDone(trianglesRes);
    }

    IEnumerator OnMarchCubesCoroutine(MarchComputeDestruct trianglesRes)
    {
        OnMarchCubesGPUDone(trianglesRes);
        yield return null;
    }

    internal void SetData(Chunky chunkData)
    {      
        this.chunkData = chunkData;
        if (!chunkData.gridInitialized && !queuedForNoise)
        {
            //TerrainManager.instance.QueueDrawSequenceJobForProcessing(GenerateNewChunkData);
            GenerateNewChunkData();
        }
    }
    /*
    void MarchCubes_UJ()
    {
        int chunkSizeInCubes = chunkData.chunkWidth * chunkData.chunkHeight * chunkData.chunkDepth;
        int maxVertices = chunkSizeInCubes * 15;

        var _t = Tables.triangulation.Cast<int>().ToArray();
        var _e = Tables.edgeVertices.Cast<int>().ToArray();

        NativeArray<Vert> vertices = new NativeArray<Vert>(maxVertices, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
        NativeArray<float> grid = new NativeArray<float>(chunkData.densityGrid, Allocator.TempJob); 
        NativeArray<int> triangulation = new NativeArray<int>(_t, Allocator.TempJob);
        NativeArray<int> edgeVertices = new NativeArray<int>(_e, Allocator.TempJob);
        var job = new MarchCubesJob()
        {
            vertices = vertices,
            grid = grid,
            edgeVertices = edgeVertices,
            triangulation = triangulation,
            width = chunkData.chunkWidth,
            height = chunkData.chunkHeight,
            depth = chunkData.chunkDepth,
            isoLevel = TerrainManager.instance.isoLevel
        };
        var jobHandle = job.Schedule(chunkSizeInCubes, 1);

        TerrainManager.instance.QueueMarchCubeJobForProcessing(new MarchCubesJobResultHandler(jobHandle, job, OnMarchCubesResults));

    }*/
    /*
    private void OnMarchCubesResults(Vert[] verts)
    {
        var trianglesRes = verts.Aggregate(new MarchComputeDestruct(), (acc, val) =>
        {
            var vertCount = acc.vertices.Count;
            if( val.vert.w > 0)
            { 
                acc.vertices.Add( val.vert.xyz ); 
                acc.normals.Add(val.normal ); 
                acc.triIndices.Add(vertCount); 
            }
            return acc;
        }); 

        Vector3[] vertices = trianglesRes.vertices.ToArray();
        int[] triangles = trianglesRes.triIndices.ToArray();
        Vector3[] normals = trianglesRes.normals.ToArray();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;

        OnMarchCubesDone();

    }
    */
    private void OnMarchCubesGPUDone(MarchComputeDestruct triangles)
    {
        if (this == null)
        {
            Debug.Log($"OnMarchCubesGPUDone called on null container");
            return; 
        }
        InitMesh();
        mesh.vertices = triangles.vertices.ToArray();
        mesh.triangles = triangles.triIndices.ToArray();
        mesh.normals = triangles.normals.ToArray();
        OnMarchCubesDone();
    }

    void OnMarchCubesDone()
    { 
        BuildMesh();
        RenderMesh();
    }

    void GenerateNewChunkData()
    { 

        int width = 1 + chunkData.chunkWidth;
        int height = 1 + chunkData.chunkHeight;
        int depth = 1 + chunkData.chunkDepth;
        int dgTotalSize = width * height * depth;
        Vector3Int gridOffset = chunkData.GetWorldGridCoord(Vector3Int.zero);
        Vector3Int noiseOffset = TerrainManager.instance.offset;
        var dGrid = new float[dgTotalSize];


        int threadGroupsX = Mathf.CeilToInt(width * height * depth / 1024f);


        ComputeBuffer dgBuffer = new ComputeBuffer(dgTotalSize, sizeof(float));


        dgBuffer.SetData(dGrid);

        noiseCompute.SetBuffer(0, "DensityGrid", dgBuffer);
        noiseCompute.SetInt("_width", width);
        noiseCompute.SetInt("_height", height);
        noiseCompute.SetInt("_depth", depth);
        noiseCompute.SetInts("_gridOffset", gridOffset.x, gridOffset.y, gridOffset.z);
        noiseCompute.SetInts("_noiseOffset", noiseOffset.x, noiseOffset.y, noiseOffset.z);
        noiseCompute.SetFloat("_lacunarity", TerrainManager.instance.gridNoise.lacunarity);
        noiseCompute.SetFloat("_persistence", TerrainManager.instance.gridNoise.persistence);
        noiseCompute.SetInt("_octaves", TerrainManager.instance.gridNoise.octaves);
        noiseCompute.SetFloat("_scale", TerrainManager.instance.gridNoise.scale);
        noiseCompute.SetFloat("_hLacunarity", TerrainManager.instance.heightNoise.lacunarity);
        noiseCompute.SetFloat("_hPersistence", TerrainManager.instance.heightNoise.persistence);
        noiseCompute.SetInt("_hOctaves", TerrainManager.instance.heightNoise.octaves);
        noiseCompute.SetFloat("_hScale", TerrainManager.instance.heightNoise.scale);
        noiseCompute.SetFloat("_maxOverworldHeight", TerrainManager.instance.maxOverworldHeight);
        noiseCompute.SetFloat("_overworldStartAt", (float)TerrainManager.instance.overworldStartAt);
        noiseCompute.SetFloat("_isoLevel", TerrainManager.instance.isoLevel);


        noiseCompute.Dispatch(0, threadGroupsX, 1, 1);

        //dgBuffer.GetData(chunkData.densityGrid);
        AsyncGPUReadback.Request(dgBuffer, (r) => OnNewChunkDataReceived(r, dgBuffer, Time.frameCount, chunkData, this));

    }
    private void OnNewChunkDataReceived(AsyncGPUReadbackRequest request, ComputeBuffer buffer, int frameCount, Chunky cd, ChunkContainer cont)
    {
        if (request.hasError || // Something wrong happened
               !Application.isPlaying) // Callback happened in edit mode afterwards
        {
            return;
        }

        var _r = request.GetData<float>(0);
        var dGrid = new float[_r.Length];
        _r.ToArray().CopyTo(dGrid, 0);
        _r.Dispose();
        buffer.Dispose();
        TerrainManager.instance.QueueDrawSequenceJobForProcessing(OnChunkDataReceivedCoroutine(dGrid, cd, cont));

        //cd.InitGrid(dGrid);
        //cont.queuedForNoise = false;

    }
    IEnumerator OnChunkDataReceivedCoroutine(float[] grid, Chunky cd, ChunkContainer cont)
    {
        if (cd != null)
        {
            cd.InitGrid(grid);
            cont.queuedForNoise = false;
        }
        yield return null;
    }


    Vector3 CubePointLerp(Vector4 a, Vector4 b)
    {
        float factor = (TerrainManager.instance.isoLevel - a.w) / (b.w - a.w);
        Vector3 _a = new Vector3(a.x, a.y, a.z);
        Vector3 _b = new Vector3(b.x, b.y, b.z);
        return Vector3.Lerp(_a, _b, factor);
    } 
    Vector3 GetNormalFromDensityGrid(Vector3Int coord)
    {
        Vector3Int offsetX = new Vector3Int(1, 0, 0);
        Vector3Int offsetY = new Vector3Int(0, 1, 0);
        Vector3Int offsetZ = new Vector3Int(0, 0, 1);

        float dx = chunkData.GetDensityAt(coord + offsetX) - chunkData.GetDensityAt(coord - offsetX);
        float dy = chunkData.GetDensityAt(coord + offsetY) - chunkData.GetDensityAt(coord - offsetY);
        float dz = chunkData.GetDensityAt(coord + offsetZ) - chunkData.GetDensityAt(coord - offsetZ);

        return  new Vector3 (dx, dy, dz).normalized;
    }
    Vector3 GetNormalOnEdge(Vector4 a, Vector4 b)
    {
        float factor = (TerrainManager.instance.isoLevel - a.w) / (b.w - a.w);
        var _a = new Vector3Int((int)a.x, (int)a.y, (int)a.z);
        var _b = new Vector3Int((int)b.x, (int)b.y, (int)b.z);

        Vector3 na = GetNormalFromDensityGrid(_a);
        Vector3 nb = GetNormalFromDensityGrid(_b);

        Vector3 normal =  (na + factor * (nb - na)).normalized;
        return normal;
    }
    private void OnRenderObject()
    { 
    }

}
struct Triangle
{
    public Vector3 vert1;
    public Vector3 vert2;
    public Vector3 vert3;

    public Vector3 normal1;
    public Vector3 normal2;
    public Vector3 normal3; 
};

public struct Vert
{
    public float4 vert;// float4. w is used to keep track if the vertex is on a generated tri
    public float3 normal;
}
public class MarchComputeDestruct
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<Vector3> normals = new List<Vector3>();
    public List<int> triIndices = new List<int>();
}
 