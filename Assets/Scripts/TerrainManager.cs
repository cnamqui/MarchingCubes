using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager instance;

    [SerializeField] ChunkContainer containerPrefab;
    [SerializeField] Transform playerPos;
    [Header("Chunk Settings")]
    [SerializeField] int chunkSize = 100; 
    [SerializeField] int seed = 1; 
    [SerializeField] int viewRadius = 5; //in units  
    [SerializeField] public float maxOverworldHeight;
    [SerializeField] int underworldDrawClamp;//in chunks;  
    [SerializeField] public int overworldStartAt = 0;
    [SerializeField] public Vector3Int offset;


    [Header("Noise Settings ")] 
    [SerializeField] public NoiseSettings gridNoise;
    [SerializeField] public NoiseSettings heightNoise;
    [SerializeField] public float isoLevel = 0.5f;


    [Header("Misc ")] 
    [SerializeField] ComputeShader noiseTexCompute;


    Dictionary<Vector3Int, float[,,]> densityGrids = new Dictionary<Vector3Int, float[,,]>();
    Dictionary<Vector3Int, float[,]> heightMaps = new Dictionary<Vector3Int, float[,]>(); 
     
    Dictionary<Vector3Int,ChunkContainer> chunksInRadius = new Dictionary<Vector3Int, ChunkContainer>();
    Dictionary<Vector3Int,Chunky> chunks = new Dictionary<Vector3Int, Chunky>();
    Queue<ChunkContainer> chunkContainerPool = new Queue<ChunkContainer>();
     
    Queue<IEnumerator> marchingCubesGPUJobs = new Queue<IEnumerator>();
    Queue<IEnumerator> drawSequenceQueue = new Queue<IEnumerator>();

    private void Awake()
    {   
        TerrainManager.instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (seed == 0)
            seed = UnityEngine.Random.Range(-1000000, 1000000); 
        System.Random random = new System.Random(seed);
        if (offset == Vector3.zero)
        {
            var offsetScale = random.Next(-100000, 100000);
            offset = new Vector3Int(
                random.Next(-100000, 100000),
                random.Next(-100000, 100000),
                random.Next(-100000, 100000)
                );
        }
        GenerateNoiseTex(); 
        StartCoroutine(processMcgJobs());
        StartCoroutine(processDrawSequenceQueue()); 
    }
     

    IEnumerator processMcgJobs()
    {
        while (true)
        {
            while (marchingCubesGPUJobs.Count > 0)
            {
                
                var mcgJob = marchingCubesGPUJobs.Dequeue(); 
                yield return StartCoroutine(mcgJob);
                
            }
            yield return null;
        }
    }
    public void QueueMarchCubeGPUJobForProcessing(IEnumerator mcgj)
    {
        lock (this.marchingCubesGPUJobs)
        { 
            this.marchingCubesGPUJobs.Enqueue(mcgj);
        }
    }

    IEnumerator processDrawSequenceQueue()
    {
        while (true)
        {
            while (drawSequenceQueue.Count > 0)
            {

                var drawJob = drawSequenceQueue.Dequeue();  
                yield return StartCoroutine(drawJob); ;

            }
            yield return null;
        }
    }
    public void QueueDrawSequenceJobForProcessing(IEnumerator dj)
    {
        lock (this.drawSequenceQueue)
        {
            this.drawSequenceQueue.Enqueue(dj);
        }
    }

    // Update is called once per frame
    void Update()
    {
        DrawChunks();
    }

    void OnGUI()
    {
        if(GUI.Button(new Rect(0, 0, 100, 50), "TEST"))
        {
            foreach(var chunk in chunksInRadius.Values)
            {
                chunk.gameObject.SetActive(false);
                chunkContainerPool.Enqueue(chunk);
            }    
            chunksInRadius.Clear();
            chunks.Clear();
        } 
    }
     

    public void DrawChunks()
    {

        Vector3Int playerChunk = new Vector3Int
            (Mathf.FloorToInt(playerPos.position.x / chunkSize),
             Mathf.FloorToInt(playerPos.position.y / chunkSize),
             Mathf.FloorToInt(playerPos.position.z / chunkSize)
            );
        int topChunkLayer = 1 + (int)(maxOverworldHeight + overworldStartAt) / chunkSize;
        int distanceFromTopChunk = topChunkLayer - playerChunk.y;
        RemoveAllChunksOutOfRange(playerChunk, topChunkLayer, distanceFromTopChunk);
        PlaceAllChunksInRange(playerChunk, topChunkLayer, distanceFromTopChunk); 
     }

    private void PlaceAllChunksInRange(Vector3Int playerChunk, int topChunkLayer, int distanceFromTopChunk)
    { 
        for (int x = -viewRadius + 1; x < viewRadius; x++)
        {
            var yStart = Mathf.Max(-viewRadius + 1, -underworldDrawClamp);
            var yEnd = Mathf.Min(viewRadius, distanceFromTopChunk + 1);
            for (int y = yStart + 1; y < yEnd; y++)
            {
                for (int z = -viewRadius + 1; z < viewRadius; z++)
                {
                    ChunkContainer container;
                    var chunkCoord = new Vector3Int(x, y, z) + playerChunk;
                    if ((playerChunk - chunkCoord).magnitude < viewRadius)
                    {
                        bool containerFound = chunksInRadius.TryGetValue(chunkCoord, out container);
                        if (containerFound)
                        {
                            continue;
                        }
                        else
                        {
                            if (chunkContainerPool.Count > 0)
                            {
                                container = chunkContainerPool.Dequeue();
                                container.gameObject.SetActive(true);
                            }
                            else
                            {
                                container = Instantiate(containerPrefab);
                                container.transform.parent = transform;
                            }
                            chunksInRadius.Add(chunkCoord, container);
                            container.transform.position = (Vector3)(chunkCoord * chunkSize);
                        } 
                        Chunky chunkData;
                        if (!chunks.TryGetValue(chunkCoord, out chunkData))
                        {
                            chunkData = new Chunky(chunkCoord, this.chunkSize);
                            chunks.Add(chunkCoord, chunkData);
                        }
                        container.SetData(chunkData);  
                    }

                }
            }
        }
    }   

    private void RemoveAllChunksOutOfRange(Vector3Int playerChunk, int topChunkLayer, int distanceFromTopChunk)
    { 
        var toRemove = new List<Vector3Int>();
        foreach(var cir in chunksInRadius)
        {
            var vectorFromPlayer = cir.Key - playerChunk;
            var heightChunkDiff = playerChunk.y - cir.Key.y;
            bool outOfRange = vectorFromPlayer.magnitude >= viewRadius || cir.Key.y > topChunkLayer + 1 || heightChunkDiff > underworldDrawClamp;

            if (outOfRange)
            {
                toRemove.Add(cir.Key);
                chunkContainerPool.Enqueue(cir.Value);
                cir.Value.ClearChunkData();
                cir.Value.gameObject.SetActive(false);
                //Destroy(cir.Value.gameObject);
            } 
        };
        toRemove.ForEach(ci => chunksInRadius.Remove(ci));
    }

    void GenerateNoiseTex()
    {
        var resolution = 256;
        RenderTexture texture = new RenderTexture(256, 256, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true
        };
        texture.Create(); 
        int threadGroups = Mathf.CeilToInt(resolution/8f); 

        noiseTexCompute.SetTexture(0, "Result", texture);
        noiseTexCompute.Dispatch(0, threadGroups, threadGroups, 1);
        containerPrefab.texture = texture; 
    }

}
