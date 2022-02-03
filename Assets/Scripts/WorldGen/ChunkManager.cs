using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine; 
using System.Linq;


[RequireComponent(typeof(ChunkFactory))]
[RequireComponent(typeof(ChunkStore))]
[RequireComponent(typeof(ChunkUpdater))]
[RequireComponent(typeof(VoxelStore))]
[RequireComponent(typeof(VoxelFactory))]
public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    [SerializeField] Transform playerTransform;

    [Header("Chunk Settings")]
    [SerializeField] public ChunkSettings settings;
    [Tooltip("Lock procedural generation. If true chunks will not be procedurally spawned around the player")]
    [SerializeField] public bool lockProcGen = false;

    public ChunkFactory chunkFactory {get; private set;}
    public ChunkStore chunkStore { get; private set; } 
    public ChunkUpdater chunkUpdater {get; private set;} 
    public VoxelStore voxelStore {get; private set;} 
    public VoxelFactory voxelFactory {get; private set;}


    public int3 playerChunk { get
        {
            int x = Mathf.FloorToInt(playerTransform.position.x / (float)(settings.chunkSize * settings.chunkScale));
            int y = Mathf.FloorToInt(playerTransform.position.y / (float)(settings.chunkSize * settings.chunkScale));
            int z = Mathf.FloorToInt(playerTransform.position.z / (float)(settings.chunkSize * settings.chunkScale));
            return new int3(x, y, z);
        } }

    int3 _lastPlayerChunk;

    private void Awake()
    {
        if (Instance == null) Instance = this;


        chunkFactory = GetComponent<ChunkFactory>();
        chunkStore = GetComponent<ChunkStore>();
        chunkUpdater = GetComponent<ChunkUpdater>();
        voxelStore = GetComponent<VoxelStore>();
        voxelFactory = GetComponent<VoxelFactory>();

    }

    // Start is called before the first frame update
    void Start()
    {
        if (settings.seed == 0)
            settings.seed = UnityEngine.Random.Range(-1000000, 1000000);
        System.Random random = new System.Random(settings.seed);
        if (math.all(settings.densitySettings.noiseOffset == int3.zero))
        {
            var offsetScale = random.Next(-100000, 100000);
            settings.densitySettings.noiseOffset = new int3(
                random.Next(-100000, 100000),
                random.Next(-100000, 100000),
                random.Next(-100000, 100000)
                );
        }


        GenerateChunksAroundPlayer();
        _lastPlayerChunk = playerChunk;
    }

    // Update is called once per frame
    void Update()
    {
        var distanceFromLastChunk =  math.abs(math.distance(playerChunk, _lastPlayerChunk));
        if(distanceFromLastChunk >= (float) settings.softLockProcGenRadius && !lockProcGen)
        {
            // move chunks around here
            var reusableChunks = chunkStore.GetChunkCoordinatesOutsideOfRange(playerChunk, settings.viewRadius).ToArray();
            var newCoordsInRange = CoordinatesHelper.GetChunkCoordinatesDelta(_lastPlayerChunk, playerChunk, settings.viewRadius).ToArray();

            var i = 0;
            //newCoordsInRange = CoordinatesHelper.GetChunkCoordinatesDelta(_lastPlayerChunk, playerChunk, settings.viewRadius).ToArray();
            foreach (var rChunkCoords in reusableChunks)
            {
                if (i >= newCoordsInRange.Length) break;
                var newCoords = newCoordsInRange[i];
                chunkStore.MoveChunk(rChunkCoords, newCoords);
                voxelFactory.CreateEmptyVoxelsAt(newCoords);
                voxelFactory.EnqueueVoxelForDataGeneration(newCoords, true);
                i++;
            }
            for(int j = i; j < newCoordsInRange.Length; j++)
            { 
                var newCoords = newCoordsInRange[j];
                voxelFactory.CreateEmptyVoxelsAt(newCoords);
                chunkFactory.CreateEmptyChunkAt(newCoords);
                voxelFactory.EnqueueVoxelForDataGeneration(newCoords, true);
            }
            
            _lastPlayerChunk = playerChunk;
        }
    }

    void GenerateChunksAroundPlayer()
    {
        lockProcGen = true;
        var coordAroundPlayer = CoordinatesHelper.CoordinatesInRadius(playerChunk, settings.viewRadius);
        foreach(var coord in coordAroundPlayer)
        {
            voxelFactory.EnsureVoxelsExistsAt(coord);
            chunkFactory.EnsureChunkExistsAt(coord);
        }
        lockProcGen = false;

    }


}
