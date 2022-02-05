using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public static class CoordinatesHelper 
{ 
    public static IEnumerable<int3> CoordinatesInRadius(int3 center, int radius)
    {

        //var settings = ChunkManager.Instance.settings;
        //float maxOverworldHeight = settings.maxOverworldHeight;
        //int overworldStartAt = settings.overworldStartAt;
        //int topChunk = Mathf.CeilToInt(maxOverworldHeight / settings.chunkSize) + overworldStartAt;
        //int underworldDrawClamp = settings.underworldDrawClamp;

        for (int x= - radius; x<=radius; x++)
        {
            for(int y= -radius; y<=radius; y++)
            {
                for(int z= - radius; z<=radius; z++)
                {
                    int3 currentCoord = new int3(x, y, z) + center;
                    //var distance = math.abs(math.distance(center, currentCoord));
                    //var withinUDClamp = center.y - currentCoord.y <= underworldDrawClamp;
                    //var belowHighestPoint = currentCoord.y <= topChunk;
                    //if (distance <= radius && withinUDClamp && belowHighestPoint)
                    if (IsCoordinateInRange(center,currentCoord,radius))
                        yield return currentCoord;
                }
            }
        }
    }

    public static bool IsCoordinateInRange(int3 refPoint, int3 coord, float range)
    {

        var settings = ChunkManager.Instance.settings;
        float maxOverworldHeight = settings.maxOverworldHeight;
        int overworldStartAt = settings.overworldStartAt;
        int topChunk = Mathf.CeilToInt(maxOverworldHeight / settings.chunkSize) + overworldStartAt;
        int underworldDrawClamp = settings.underworldDrawClamp;

        var distance = math.abs(math.distance(refPoint, coord));
        var withinUDClamp = refPoint.y - coord.y <= underworldDrawClamp;
        var withinOverworldClamp = (float)overworldStartAt - coord.y <= underworldDrawClamp;//chunk within the clamp from the overworld
        var refPointTooFarFromOverworld = refPoint.y - overworldStartAt > underworldDrawClamp;
        var withinClampOrOverworld = withinUDClamp || (refPointTooFarFromOverworld && withinOverworldClamp);

        var belowHighestPoint = coord.y <= topChunk;
        return distance <= range && withinClampOrOverworld && belowHighestPoint;
    }

    public static IEnumerable<int3> GetChunkCoordinatesDelta(int3 oldCoord, int3 newCoord, int range)
    {

        //var settings = ChunkManager.Instance.settings;
        //float maxOverworldHeight = settings.maxOverworldHeight;
        //int overworldStartAt = settings.overworldStartAt;
        //int topChunk = Mathf.CeilToInt(maxOverworldHeight / settings.chunkSize) + overworldStartAt;
        //int underworldDrawClamp = settings.underworldDrawClamp;
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                for (int z = -range; z <= range; z++)
                {
                    int3 currentCoord = new int3(x, y, z) + newCoord;
                    //var distanceFromOld = math.abs(math.distance(currentCoord, oldCoord));
                    //var distanceFromNew = math.abs(math.distance(currentCoord, newCoord)); 
                    //var withinUDClampOfOld = oldCoord.y - currentCoord.y <= underworldDrawClamp;
                    //var withinUDClampOfNew = newCoord.y - currentCoord.y <= underworldDrawClamp;
                    //var belowHighestPoint = currentCoord.y <= topChunk;

                    //var inRangeOfNew = (distanceFromNew <= range && withinUDClampOfNew && belowHighestPoint);
                    //var inRangeOfOld = (distanceFromOld <= range && withinUDClampOfOld && belowHighestPoint);

                    var inRangeOfNew = IsCoordinateInRange(newCoord,currentCoord,range);
                    var inRangeOfOld = IsCoordinateInRange(oldCoord, currentCoord, range);
                    if (inRangeOfNew && !inRangeOfOld)
                    {
                        yield return currentCoord;
                    }
                }
            } 
        }
    }

    public static IEnumerable<int3> GetChunkCoordinatesDeltaSorted(int3 oldCoord, int3 newCoord, int range)
    {
        var coords = GetChunkCoordinatesDelta(oldCoord,newCoord,range);

        return coords.OrderBy(coord => math.abs(math.distance(newCoord, coord))); 
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

}
