using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

public static class MathClassHelper
{
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
