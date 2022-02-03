using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ChunkData 
{
    public GameObject gameObject;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public MeshRenderer meshRenderer;


    public int3 chunkCoord; 
    public bool hasChanges;
    public bool hasMesh;

    public ChunkData(GameObject go)
    {
        this.gameObject = go;
        this.meshCollider = go.GetComponent<MeshCollider>();
        this.meshFilter = go.GetComponent<MeshFilter>();
        this.meshRenderer = go.GetComponent<MeshRenderer>();
    }


    public void Initialize(int3 coord, int chunkSize, float scale)
    {
        int3 _pos = (coord * chunkSize);
        this.gameObject.transform.position = new Vector3(_pos.x, _pos.y, _pos.z) * scale;
        this.chunkCoord = coord;
        this.hasChanges = false;
        this.hasMesh = false;
    }
    public void SetNoiseTexture(RenderTexture tex)
    {
        this.meshRenderer.sharedMaterial.SetTexture("_MainTex", tex);
    }

}
