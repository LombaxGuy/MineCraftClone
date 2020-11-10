using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Settings
{
    [Header("Performance")]
    public int viewDistance = 8;
    public bool enableThreading = true;

    [Header("Input")]
    [Range(0.1f, 10f)] public float mouseSensitivity = 1;
}
