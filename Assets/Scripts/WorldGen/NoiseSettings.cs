using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Mathematics;

[Serializable]
public class NoiseSettings
{
    public int octaves = 8;
    public float scale = 0.75f;
    public float persistence = 0.39f;
    public float lacunarity = 2;
    public int3 noiseOffset;
}