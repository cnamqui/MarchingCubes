using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Mathematics;

[Serializable]
public class NoiseSettings
{
    public int octaves;
    public float scale;
    public float persistence;
    public float lacunarity;
    public int3 noiseOffset;
}