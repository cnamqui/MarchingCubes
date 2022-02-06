using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ChunkFactory : MonoBehaviour
{
    [SerializeField] GameObject chunkObjectPrefab;
    [SerializeField] int throttle = 10; 
    [SerializeField] ComputeShader noiseTexCompute;
    [SerializeField] RenderTexture noiseTex;

    private Queue<int3> queue;
    private List<QueuedMeshBuilder> concurrentBuilds;


    // Start is called before the first frame update
    void Start()
    {
        queue = new Queue<int3>();
        concurrentBuilds = new List<QueuedMeshBuilder>();  
        this.throttle = ChunkManager.Instance.settings.proceduralMeshProcessingThrottle; 
    }

    // Update is called once per frame
    void Update()
    {
        int ct = 0;
        while (queue.Count > 0 && ct < throttle && concurrentBuilds.Count < throttle)
        {
            int3 chunkCoordinate = queue.Dequeue();

            if (ChunkManager.Instance.chunkStore.TryGetDataChunk(chunkCoordinate, out ChunkData chunk))
            {
                if (!chunk.hasMesh)
                {
                    var builder = ChunkManager.Instance.chunkUpdater.GenerateMeshAsync(chunk);
                    if (builder != null)
                    {
                        concurrentBuilds.Add(builder);
                        ct++;
                    }
                }
            }
        }
        concurrentBuilds.RemoveAll(b => b.done);  
    }


    public void EnqueueChunkForMeshGeneration(int3 coord)
    {
        if(ChunkManager.Instance.chunkStore.DoesChunkExistAt(coord) && !queue.Contains(coord))
        {
            queue.Enqueue(coord);
        }
    }

   
    private ChunkData CreateChunk(int3 coord)
    { 
        int3 _pos = (coord * ChunkManager.Instance.settings.chunkSize);
        Vector3 worldPos = new Vector3(_pos.x, _pos.y, _pos.z) * ChunkManager.Instance.settings.chunkScale;
        GameObject chunkGameObject = Instantiate(chunkObjectPrefab, worldPos, Quaternion.identity); 
        chunkGameObject.transform.parent = transform;
        chunkGameObject.transform.localScale *= ChunkManager.Instance.settings.chunkScale;
        ChunkData chunk = new ChunkData(chunkGameObject); 
        chunk.Initialize(coord, ChunkManager.Instance.settings.chunkSize, ChunkManager.Instance.settings.chunkScale);
        if(this.noiseTex == null)
        {
            GenerateNoiseTex();
        }
        chunk.SetNoiseTexture(noiseTex);
        ChunkManager.Instance.chunkStore.AddChunk(coord, chunk);
        return chunk;
    }

    public void EnsureChunkExistsAt(int3 coord)
    {
        if (!ChunkManager.Instance.chunkStore.DoesChunkExistAt(coord))
        {
            var chunk = CreateChunk(coord);
            ChunkManager.Instance.chunkUpdater.GenerateMesh(chunk);
        }
    }
    public void CreateEmptyChunkAt(int3 coord)
    {
        if (!ChunkManager.Instance.chunkStore.DoesChunkExistAt(coord))
        {
            var chunk = CreateChunk(coord); 
        }
    }


    void GenerateNoiseTex()
    {
        var resolution = 256;
        RenderTexture texture = new RenderTexture(256, 256, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true
        };
        texture.Create();
        int threadGroups = Mathf.CeilToInt(resolution / 8f);

        noiseTexCompute.SetTexture(0, "Result", texture);
        noiseTexCompute.Dispatch(0, threadGroups, threadGroups, 1);
        noiseTex = texture;
    }
}
