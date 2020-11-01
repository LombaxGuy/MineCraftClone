using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelState
{
    public byte id;
    public float globalLightPercent;

    public VoxelState()
    {
        id = 0;
        globalLightPercent = 0f;
    }

    public VoxelState(byte id)
    {
        this.id = id;
        globalLightPercent = 0f;
    }
}
