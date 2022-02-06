using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

public class VoxelFactory : MonoBehaviour
{
    [SerializeField] int throttle = 10; 
    [SerializeField] ComputeShader noiseCompute;
    private Queue<(int3,bool)> queue;


    // Start is called before the first frame update
    void Start()
    {
        queue = new Queue<(int3,bool)>(); 
        this.throttle = ChunkManager.Instance.settings.voxelProcessingThrottle;
    }

    // Update is called once per frame
    void Update()
    {
        int ct = 0;
        while (queue.Count > 0 && ct < throttle)
        {
            var qi = queue.Dequeue();
            int3 chunkCoordinate = qi.Item1;
            bool queueForMeshGen = qi.Item2;
            if (ChunkManager.Instance.voxelStore.TryGetVoxel(chunkCoordinate, out VoxelData data))
            {
                if (!data.hasDensity)
                {
                    GenerateNoise(data);
                    ct++;
                }
                if(queueForMeshGen) ChunkManager.Instance.chunkFactory.EnqueueChunkForMeshGeneration(chunkCoordinate);
            }
        }
    }

    public void EnqueueVoxelForDataGeneration(int3 coord, bool enqueueChunkGeneration = false)
    {
        bool alreadyInQueue = queue.Any(qi => math.all(qi.Item1 == coord));
        if (ChunkManager.Instance.chunkStore.DoesChunkExistAt(coord) && !alreadyInQueue)
        {
            queue.Enqueue((coord,enqueueChunkGeneration));
        }
    }

    private void GenerateNoise(VoxelData data)
    {

        // Generate noise
        var _settings = ChunkManager.Instance.settings;
        int chunkSize = _settings.chunkSize;
        int gridDim = 1 + chunkSize;
        int dgTotalSize = gridDim * gridDim * gridDim;
        int3 gridOffset = CoordinatesHelper.GetWorldGridCoordFromLocalChunkCoord(int3.zero,data.chunkCoord,chunkSize);
        int3 noiseOffset = _settings.densitySettings.noiseOffset;
        ///var dGrid = new float[dgTotalSize];


        int threadGroupsX = Mathf.CeilToInt(gridDim * gridDim * gridDim / 1024f);


        ComputeBuffer dgBuffer = new ComputeBuffer(dgTotalSize, sizeof(float));


        dgBuffer.SetData(data.density);

        noiseCompute.SetBuffer(0, "DensityGrid", dgBuffer);
        noiseCompute.SetInt("_width", gridDim);
        noiseCompute.SetInt("_height", gridDim);
        noiseCompute.SetInt("_depth", gridDim);
        noiseCompute.SetInts("_gridOffset", gridOffset.x, gridOffset.y, gridOffset.z);
        noiseCompute.SetInts("_noiseOffset", noiseOffset.x, noiseOffset.y, noiseOffset.z);
        noiseCompute.SetFloat("_lacunarity", _settings.densitySettings.lacunarity);
        noiseCompute.SetFloat("_persistence", _settings.densitySettings.persistence);
        noiseCompute.SetInt("_octaves", _settings.densitySettings.octaves);
        noiseCompute.SetFloat("_scale", _settings.densitySettings.scale);
        noiseCompute.SetFloat("_hLacunarity", _settings.heightMapSettings.lacunarity);
        noiseCompute.SetFloat("_hPersistence", _settings.heightMapSettings.persistence);
        noiseCompute.SetInt("_hOctaves", _settings.heightMapSettings.octaves);
        noiseCompute.SetFloat("_hScale", _settings.heightMapSettings.scale);
        noiseCompute.SetFloat("_maxOverworldHeight", _settings.maxOverworldHeight);
        noiseCompute.SetFloat("_overworldStartAt", (float)_settings.overworldStartAt);
        noiseCompute.SetFloat("_isoLevel", _settings.isoLevel);


        noiseCompute.Dispatch(0, threadGroupsX, 1, 1);

        dgBuffer.GetData(data.density);  
        data.hasDensity = true;
        dgBuffer.Dispose();
        //AsyncGPUReadback.Request(dgBuffer, (r) => OnNewChunkDataReceived(r, dgBuffer, Time.frameCount, chunkData, this));
         
    }
    private void CreateVoxelsAt(int3 coord)
    { 
        VoxelData data = new VoxelData(ChunkManager.Instance.settings.chunkSize,coord); 
        GenerateNoise(data);
        ChunkManager.Instance.voxelStore.AddVoxel(coord, data);
    }

    public void EnsureVoxelsExistsAt(int3 coord)
    {
        if (!ChunkManager.Instance.chunkStore.DoesChunkExistAt(coord))
        {
            CreateVoxelsAt(coord);
        }
    }
    public void CreateEmptyVoxelsAt(int3 coord)
    { 
        VoxelData data = new VoxelData(ChunkManager.Instance.settings.chunkSize, coord); 
        ChunkManager.Instance.voxelStore.AddVoxel(coord, data);
    }
     
    
}
