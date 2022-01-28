using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunky
{

    public float[] densityGrid { get; private set; }
    public Vector3Int chunkCoord { get; private set; }
    public int chunkWidth { get; private set; }
    public int chunkHeight { get; private set; }
    public int chunkDepth { get; private set; }
    public bool gridInitialized { get; private set; }
    public bool meshUpToDate { get; private set; }
    public Mesh mesh { get; private set; }

    public Chunky(Vector3Int chunkCoord, int chunkWidth, int chunkHeight, int chunkDepth)
    {
        this.chunkCoord = chunkCoord;
        this.chunkWidth = chunkWidth;
        this.chunkHeight = chunkHeight;
        this.chunkDepth = chunkDepth;
    }
    public Chunky(Vector3Int chunkCoord, int chunkSize)
    {
        this.chunkCoord = chunkCoord;
        this.chunkWidth = chunkSize;
        this.chunkHeight = chunkSize;
        this.chunkDepth = chunkSize;
    }

    public bool InitGrid(float[] grid)
    {
        //TODO validate if grid is the correct size. not sure if needed
        densityGrid = grid;
        gridInitialized = true;
        return gridInitialized;
    }

    public Vector3Int GetWorldGridCoord(Vector3Int localCoord)
    {
   
        Vector3Int _coord = new Vector3Int(
            localCoord.x + chunkCoord.x  * chunkWidth,
            localCoord.y + chunkCoord.y  * chunkHeight,
            localCoord.z + chunkCoord.z  * chunkDepth
            );
        return _coord;
    }
    public Vector3Int GetLocalGridCoord(Vector3Int worldGridCoord)
    {

        Vector3Int _coord = new Vector3Int(
            worldGridCoord.x % chunkWidth,
            worldGridCoord.y % chunkHeight,
            worldGridCoord.z % chunkDepth
            );
        return _coord;
    }

    public float GetDensityAt(Vector3Int gridPointCoord)
    {
        if (!gridInitialized)
            return -1f;
        if (gridPointCoord.x > chunkWidth || gridPointCoord.y < chunkHeight || gridPointCoord.z < chunkDepth)
            return -1f; 
        var dgIdx =
            gridPointCoord.x +
            gridPointCoord.y * chunkWidth +
            gridPointCoord.z * chunkWidth * chunkHeight;
        return densityGrid[dgIdx];
    }
    public float GetDensityAt(int x, int y, int z)
    { 
        var gridPointCoord = new Vector3Int(x, y, z);
        return GetDensityAt(gridPointCoord);
    }
    public void UpdateMeshData(Mesh mesh)
    {
        this.mesh = GameObject.Instantiate(mesh) as Mesh;
        this.meshUpToDate = true;
    }

    public void ClearMeshData()
    {
        this.mesh = null;
        this.meshUpToDate = false;
    }
    public void SetMeshDirty()
    {
        this.meshUpToDate = false;
    }

    public Mesh GetMesh()
    {
        if (this.mesh == null)
            return null;
        return GameObject.Instantiate(mesh) as Mesh;
    }
   
}
public class MeshData
{
    public Vector3[] vertices;
    public Vector3[] normals;
    public int[] triangles;
}