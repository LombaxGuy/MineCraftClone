using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MapGen : MonoBehaviour
{
    public int mapWidth = 100;
    public int mapHeight = 100;
    public float scale = 1;


    private bool changed = false;

    public void GenerateMap()
    {
        float[,] heightMap = new float[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                heightMap[x, y] = Noise.Perlin2D(x, y, scale);
            }
        }

        Display display = FindObjectOfType<Display>();
        display.DrawHeightMap(heightMap);
    }

    private void OnValidate()
    {
        changed = true;
    }


    public void Update()
    {
        if (changed)
        {
            GenerateMap();
            changed = false;
        }
    }
}
