using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class CoordinatesHelper 
{
    // Start is called before the first frame update
    public static IEnumerable<int3> CoordinatesInRadius(int3 center, int radius)
    {

        var settings = ChunkManager.Instance.settings;
        float maxOverworldHeight = settings.maxOverworldHeight;
        int overworldStartAt = settings.overworldStartAt;
        int topChunk = Mathf.CeilToInt(maxOverworldHeight / settings.chunkSize) + overworldStartAt;
        int underworldDrawClamp = settings.underworldDrawClamp;

        for (int x= - radius; x<=radius; x++)
        {
            for(int y= -radius; y<=radius; y++)
            {
                for(int z= - radius; z<=radius; z++)
                {   
                    int3 currentCoord = new int3(x, y, z);
                    var distance = math.abs(math.distance(center, currentCoord));
                    var withinUDClamp = center.y - currentCoord.y <= underworldDrawClamp;
                    var belowHighestPoint = currentCoord.y <= topChunk;
                    if (distance <= radius && withinUDClamp && belowHighestPoint)
                        yield return currentCoord;
                }
            }
        }
    }

    public static IEnumerable<int3> GetChunkCoordinatesDelta(int3 oldCoord, int3 newCoord, int range)
    {
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                for (int z = -range; z <= range; z++)
                {
                    int3 currentCoord = new int3(x, y, z);
                    var distanceFromOld = math.abs(math.distance(currentCoord, oldCoord));
                    var distanceFromNew = math.abs(math.distance(currentCoord, newCoord));
                    if (distanceFromOld > (float)range && distanceFromNew <= (float)range)
                    {
                        yield return currentCoord;
                    }
                }
            } 
        }
    }

    public static int3 GetWorldGridCoordFromLocalChunkCoord(int3 localCoord, int3 chunkCoord, int chunkSize)
    {

        int3 _coord = new int3(
            localCoord.x + chunkCoord.x * chunkSize,
            localCoord.y + chunkCoord.y * chunkSize,
            localCoord.z + chunkCoord.z * chunkSize
            );
        return _coord;
    }

    public static Vector3Int ToVector3Int(this int3 i)
    {
        return new Vector3Int(i.x, i.y, i.z);
    }
    public static int3 ToInt3(this Vector3Int i)
    {
        return new int3(i.x, i.y, i.z);
    }
    public static Vector3 ToVector3(this float3 i)
    {
        return new Vector3(i.x, i.y, i.z);
    }
    public static float3 ToFloat3(this Vector3 i)
    {
        return new float3(i.x, i.y, i.z);
    }
}
