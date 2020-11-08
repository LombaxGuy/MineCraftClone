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

    public static float Perlin2D(float x, float y, float scale, float offset = 0)
    {
        if (scale <= 1)
        {
            Debug.LogWarning("Noise.cs: Scale value is 1 or less. A value of 0 will be returned.");
            return 0;
        }

        float sampleX = (x + offset) / scale;
        float sampleY = (y + offset) / scale;

        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);

        // clamp the value as unity documentation states that this value might be slightly above 1 and slightly below 0.
        return Mathf.Clamp(perlinValue, 0, 1);
    }

    public static float Perlin3D(float x, float y, float z, float scale, float offset = 0)
    {
        // create all 6 posible perlin noise values for these coordinates
        float xy = Perlin2D(x, y, scale, offset);
        float xz = Perlin2D(x, z, scale, offset);

        float yx = Perlin2D(y, x, scale, offset);
        float yz = Perlin2D(y, z, scale, offset);

        float zx = Perlin2D(z, x, scale, offset);
        float zy = Perlin2D(z, y, scale, offset);

        // calculate and return average perlin value, dividing by 6 as we have 6 permutations
        float average = (xy + xz + yx + yz + zx + zy) / 6f;

        return average;
    }
}
