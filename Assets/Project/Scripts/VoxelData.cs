using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData
{
    public static readonly int chunkWidth = 16;
    public static readonly int chunkHeight = 128;

    public static readonly int worldSizeInChunks = 100;
    public static int WorldSizeInVoxels { get => worldSizeInChunks * chunkWidth; }

    public static readonly int viewDistanceInChunks = 5;

    public static readonly int textureAtlasSizeInBlocks = 16;
    public static float NormalizedBlockTextureSize { get => 1f / (float)textureAtlasSizeInBlocks; }

    // Lighting values
    public static float minLightLevel = 0.15f;
    public static float maxLightLevel = 0.8f;
    public static float lightFalloff = 0.08f;

    public static readonly Vector3[] vertices =
    {
        new Vector3(0.0f, 0.0f, 0.0f), // (0,0,0)
        new Vector3(0.0f, 0.0f, 1.0f), // (0,0,1)
        new Vector3(0.0f, 1.0f, 0.0f), // (0,1,0)
        new Vector3(0.0f, 1.0f, 1.0f), // (0,1,1)
        new Vector3(1.0f, 0.0f, 0.0f), // (1,0,0)
        new Vector3(1.0f, 0.0f, 1.0f), // (1,0,1)
        new Vector3(1.0f, 1.0f, 0.0f), // (1,1,0)
        new Vector3(1.0f, 1.0f, 1.0f)  // (1,1,1)
    };

    public static readonly Vector3[] faceChecks =
    {
        new Vector3( 0.0f,  0.0f, -1.0f),
        new Vector3( 0.0f,  0.0f,  1.0f),
        new Vector3(-1.0f,  0.0f,  0.0f),
        new Vector3( 1.0f,  0.0f,  0.0f),
        new Vector3( 0.0f,  1.0f,  0.0f),
        new Vector3( 0.0f, -1.0f,  0.0f)
    };

    public static readonly int[,] triangles =
    {
        // vertex order of each face
        // 0, 1, 2, 2, 3, 0

        { 0, 2, 6, 4 }, // front
        { 5, 7, 3, 1 }, // back
        { 1, 3, 2, 0 }, // left
        { 4, 6, 7, 5 }, // right
        { 2, 3, 7, 6 }, // top
        { 1, 0, 4, 5 }  // bottom

        //// old array data
        //{ 0, 2, 6, 6, 4, 0 }, // front
        //{ 5, 7, 3, 3, 1, 5 }, // back
        //{ 1, 3, 2, 2, 0, 1 }, // left
        //{ 4, 6, 7, 7, 5, 4 }, // right
        //{ 2, 3, 7, 7, 6, 2 }, // top
        //{ 1, 0, 4, 4, 5, 1 }  // bottom
    };

    public static readonly Vector2[] uvs =
    {
        new Vector2(0.0f, 0.0f), // bot-left
        new Vector2(0.0f, 1.0f), // top-left
        new Vector2(1.0f, 1.0f), // top-right
        new Vector2(1.0f, 0.0f) // bot-right

        //// old array data
        //new Vector2(0f, 0f), // bot-left
        //new Vector2(0f, 1f), // top-left
        //new Vector2(1f, 1f), // top-right
        //new Vector2(1f, 1f), // top-right
        //new Vector2(1f, 0f), // bot-right
        //new Vector2(0f, 0f) // bot-left
    };
}