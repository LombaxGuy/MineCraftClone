using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[System.Serializable]
public class BlockType
{
    public string name;
    public bool isSolid;
    public bool renderNeighbourFaces;
    public float transparency;
    public Sprite icon;

    [Header("Texture Values")]
    public int frontTexture;
    public int backTexture;
    public int leftTexture;
    public int rightTexture;
    public int topTexture;
    public int bottomTexture;

    // front, back, left, right, top, bottom

    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return frontTexture;
            case 1:
                return backTexture;
            case 2:
                return leftTexture;
            case 3:
                return rightTexture;
            case 4:
                return topTexture;
            case 5:
                return bottomTexture;
            default:
                Debug.LogError(GetType().Name + " - " + MethodBase.GetCurrentMethod().Name + ": " + "Invalid faceIndex.");
                return 0;
        }
    }
}
