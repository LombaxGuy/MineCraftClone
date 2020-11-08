using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "Biome/New Biome")]
public class BiomeAttributes : ScriptableObject
{
    public string biomeName;
    public float offset;
    public float scale;

    public int terrainHeight;
    public float terrainScale;

    public byte surfaceBlock;
    public byte subSurfaceBlock;

    [Header("Major Flora")]
    public bool placeMajorFlora = true;
    public int majorFaunaIndex = 0;
    public float majorFloraZoneScale = 1.3f;
    [Range(0, 1)] public float majorFloraZoneThreshold = 0.6f;
    public float majorFloraPlacementScale = 15f;
    [Range(0, 1)] public float majorFloraPlacementThreshold = 0.8f;

    public int maxMajorFloraHeight = 7;
    public int minMajorFloraHeight = 4;

    public Lode[] lodes;
}

[System.Serializable]
public class Lode
{
    public string nodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
}