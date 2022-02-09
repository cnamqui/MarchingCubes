
using System;
using System.Collections;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MarchCubesJob : IJob
{
    [ReadOnly]
    public NativeArray<float> grid;  

    public NativeArray<Vertex> vertices;
    public NativeArray<uint> triangleIndices;
    public NativeCounter triCounter;
    public int width;
    public int height;
    public int depth;
    public float isoLevel;

    public void Execute()
    {

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {

                    NativeArray<float4> cubePoints = new NativeArray<float4>(8, Allocator.Temp);


                    if (x >= width || y >= height || z >= depth)
                    {
                        return;
                    }
                    //cubePoints[0] = new float4(x, y, z + 1,  chunkData.GetDensityAt(x, y, z + 1));
                    //cubePoints[1] = new float4(x + 1, y, z + 1,  chunkData.GetDensityAt(x + 1, y, z + 1));
                    //cubePoints[2] = new float4(x + 1, y, z,  chunkData.GetDensityAt(x + 1, y, z));
                    //cubePoints[3] = new float4(x, y, z,  chunkData.GetDensityAt(x, y, z));
                    //cubePoints[4] = new float4(x, y + 1, z + 1,  chunkData.GetDensityAt(x, y + 1, z + 1));
                    //cubePoints[5] = new float4(x + 1, y + 1, z + 1,  chunkData.GetDensityAt(x + 1, y + 1, z + 1));
                    //cubePoints[6] = new float4(x + 1, y + 1, z,  chunkData.GetDensityAt(x + 1, y + 1, z));
                    //cubePoints[7] = new float4(x, y + 1, z,  chunkData.GetDensityAt(x, y + 1, z));

                    cubePoints[0] = new float4(x, y, z, Grid(x, y, z));
                    cubePoints[1] = new float4(x + 1, y, z, Grid(x + 1, y, z));
                    cubePoints[2] = new float4(x + 1, y, z + 1, Grid(x + 1, y, z + 1));
                    cubePoints[3] = new float4(x, y, z + 1, Grid(x, y, z + 1));
                    cubePoints[4] = new float4(x, y + 1, z, Grid(x, y + 1, z));
                    cubePoints[5] = new float4(x + 1, y + 1, z, Grid(x + 1, y + 1, z));
                    cubePoints[6] = new float4(x + 1, y + 1, z + 1, Grid(x + 1, y + 1, z + 1));
                    cubePoints[7] = new float4(x, y + 1, z + 1, Grid(x, y + 1, z + 1));

                    int cubeIndex = 0;
                    if (cubePoints[0].w < isoLevel) cubeIndex |= 1;
                    if (cubePoints[1].w < isoLevel) cubeIndex |= 2;
                    if (cubePoints[2].w < isoLevel) cubeIndex |= 4;
                    if (cubePoints[3].w < isoLevel) cubeIndex |= 8;
                    if (cubePoints[4].w < isoLevel) cubeIndex |= 16;
                    if (cubePoints[5].w < isoLevel) cubeIndex |= 32;
                    if (cubePoints[6].w < isoLevel) cubeIndex |= 64;
                    if (cubePoints[7].w < isoLevel) cubeIndex |= 128;

                    for (int i = 0; lookupTriangulation(cubeIndex, i) != -1; i += 3)
                    {
                        int edge1Index = lookupTriangulation(cubeIndex, i);
                        int edge2Index = lookupTriangulation(cubeIndex, i + 1);
                        int edge3Index = lookupTriangulation(cubeIndex, i + 2);

                        // indices of the vertex in  cubePoints[]
                        // since they're ordered the same as the diagram
                        var edge1VertexA = lookupEdgeVertices(edge1Index, 0);
                        var edge1VertexB = lookupEdgeVertices(edge1Index, 1);
                        var edge2VertexA = lookupEdgeVertices(edge2Index, 0);
                        var edge2VertexB = lookupEdgeVertices(edge2Index, 1);
                        var edge3VertexA = lookupEdgeVertices(edge3Index, 0);
                        var edge3VertexB = lookupEdgeVertices(edge3Index, 1);

                        // interpolate between the vertices based on the distance between their noise values

                        float3 vertexOnEdge1 = CubePointLerp(cubePoints[edge1VertexA], cubePoints[edge1VertexB]);
                        float3 vertexOnEdge2 = CubePointLerp(cubePoints[edge2VertexA], cubePoints[edge2VertexB]);
                        float3 vertexOnEdge3 = CubePointLerp(cubePoints[edge3VertexA], cubePoints[edge3VertexB]);
                        float3 normalOnPtOnEdge1 = GetNormalOnEdge(cubePoints[edge1VertexA], cubePoints[edge1VertexB]);
                        float3 normalOnPtOnEdge2 = GetNormalOnEdge(cubePoints[edge2VertexA], cubePoints[edge2VertexB]);
                        float3 normalOnPtOnEdge3 = GetNormalOnEdge(cubePoints[edge3VertexA], cubePoints[edge3VertexB]);

                        int triCount = triCounter.Increment();
                        int triIndexStart = triCount * 3;
                        vertices[triIndexStart + 0] = new Vertex() { vert = vertexOnEdge1, normal = normalOnPtOnEdge1 };
                        vertices[triIndexStart + 1] = new Vertex() { vert = vertexOnEdge2, normal = normalOnPtOnEdge2 };
                        vertices[triIndexStart + 2] = new Vertex() { vert = vertexOnEdge3, normal = normalOnPtOnEdge3 };

                        triangleIndices[triIndexStart + 0] = (uint)triIndexStart + 0;
                        triangleIndices[triIndexStart + 1] = (uint)triIndexStart + 1;
                        triangleIndices[triIndexStart + 2] = (uint)triIndexStart + 2;
                    }
                }
            }
        }

    }

    float Grid(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x > width || y > height || z > depth)
        {
            return 0;
        }
        int gidx = x + (y * (width + 1)) + (z * (width + 1 ) * (height + 1 ));
        return grid[gidx];
    }
    float Grid(int3 idx)
    {
        return Grid(idx.x,idx.y, idx.z);   
    }

    int lookupTriangulation(int cubeIndex, int i)
    {
        int idx = cubeIndex * 16 + i;
        //return triangulation[idx];
        return FlattenedTables.triangulation[idx];
    }
    int lookupEdgeVertices(int edgeIndex, int i)
    {
        int idx = edgeIndex * 2 + i;
        //return edgeVertices[idx];
        return FlattenedTables.edgeVertices[idx];
    }

    float3 CubePointLerp(float4 a, float4 b)
    {
        float factor = (isoLevel - a.w) / (b.w - a.w);
        float3 _a = new float3(a.x, a.y, a.z);
        float3 _b = new float3(b.x, b.y, b.z);
        return math.lerp(_a, _b, factor);
    }

    float3 GetNormalFromDensityGrid(int3 coord)
    {
        int3 offsetX = new int3(1, 0, 0);
        int3 offsetY = new int3(0, 1, 0);
        int3 offsetZ = new int3(0, 0, 1);

        float dx = Grid(coord + offsetX) - Grid(coord - offsetX);
        float dy = Grid(coord + offsetY) - Grid(coord - offsetY);
        float dz = Grid(coord + offsetZ) - Grid(coord - offsetZ);

        return math.normalize(new float3(dx, dy, dz));
    }

    float3 GetNormalOnEdge(float4 a, float4 b)
    {
        float factor = (isoLevel - a.w) / (b.w - a.w);
        float3 na = GetNormalFromDensityGrid(new int3((int)a.x, (int)a.y, (int)a.z));
        float3 nb = GetNormalFromDensityGrid(new int3((int)b.x, (int)b.y, (int)b.z));

        float3 normal = math.normalize(na + factor * (nb - na));
        return normal;
    }
     
}



