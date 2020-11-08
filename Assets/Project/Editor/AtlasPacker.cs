using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

public class AtlasPacker : EditorWindow
{
    private int blockSize = 16;
    private int atlasSizeInBlocks = 16;
    private int atlasSize;

    private Object[] rawTextures = new Object[256];
    private List<Texture2D> sortedTextures = new List<Texture2D>();
    private Texture2D atlas;

    [MenuItem("Minecraft/Atlas Packer")]
    public static void OpenWindow()
    {
        EditorWindow window = EditorWindow.GetWindow(typeof(AtlasPacker));
    }

    private void OnGUI()
    {
        atlasSize = blockSize * atlasSizeInBlocks;

        GUILayout.Label("Minecraft - Texture atlas packer", EditorStyles.boldLabel);

        blockSize = EditorGUILayout.IntField("Block Size", blockSize);
        atlasSizeInBlocks = EditorGUILayout.IntField("Atlas Size in blocks", atlasSizeInBlocks);

        if (GUILayout.Button("Load Textures"))
        {
            LoadTextures();
            PackAtlas();
        }

        if (GUILayout.Button("Clear Textures"))
        {
            atlas = new Texture2D(atlasSize, atlasSize);
            Debug.Log(GetType().Name + ": textures cleared.");
        }

        if (GUILayout.Button("Save Atlas"))
        {
            byte[] bytes = atlas.EncodeToPNG();

            File.WriteAllBytes(Application.dataPath + "/Project/Textures/PackedBlockAtlas.png", bytes);
        }

        GUILayout.Label(atlas);
    }

    private void LoadTextures()
    {
        sortedTextures.Clear();
        rawTextures = Resources.LoadAll("Atlas/Blocks", typeof(Texture2D));

        int index = 0;
        foreach (var textureObject in rawTextures)
        {
            Texture2D texture = (Texture2D)textureObject;

            if (texture.width == blockSize && texture.height == blockSize)
            {
                sortedTextures.Add(texture);
            }
            else
            {
                Debug.LogError(GetType().Name + ": " + textureObject.name + " has incorrect dimentions. Texture not loaded.");
            }

            index++;
        }

        Debug.Log(GetType().Name + ": " + sortedTextures.Count + " successfully loaded.");
    }

    private void PackAtlas()
    {
        atlas = new Texture2D(atlasSize, atlasSize);
        Color[] pixels = new Color[atlasSize * atlasSize];

        for (int x = 0; x < atlasSize; x++)
        {
            for (int y = 0; y < atlasSize; y++)
            {
                int currentBlockX = x / blockSize;
                int currentBlockY = y / blockSize;

                int index = currentBlockY * atlasSizeInBlocks + currentBlockX;

                int currentPixelX = x - (currentBlockX * blockSize);
                int currentPixleY = y - (currentBlockY * blockSize);

                if (index < sortedTextures.Count)
                {
                    pixels[(atlasSize - y - 1) * atlasSize + x] = sortedTextures[index].GetPixel(x, blockSize - y - 1);
                }
                else
                {
                    pixels[(atlasSize - y - 1) * atlasSize + x] = Color.magenta;
                }
            }
        }

        atlas.SetPixels(pixels);
        atlas.Apply();
    }
}
