using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float Get2DPerlin(Vector2 position, float offset, float scale)
    {
        return Mathf.PerlinNoise(position.x / VoxelData.chunkWidth * scale + offset, position.y / VoxelData.chunkWidth * scale + offset);
    }

    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold)
    {
        float x = (position.x + .1f) / VoxelData.chunkWidth * scale + offset;
        float y = (position.y + .1f) / VoxelData.chunkWidth * scale + offset;
        float z = (position.z + .1f) / VoxelData.chunkWidth * scale + offset;

        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);
        float ba = Mathf.PerlinNoise(y, x);
        float cb = Mathf.PerlinNoise(z, y);
        float ca = Mathf.PerlinNoise(z, x);

        if ((ab + bc + ac + ba + cb + ca) / 6f > threshold)
            return true;
        else
            return false;
    }
}
