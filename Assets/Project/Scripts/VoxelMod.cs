using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMod
{
    public Vector3 position = Vector3.zero;
    public byte id = 0;

    public VoxelMod() { }

    public VoxelMod(Vector3 position, byte id)
    {
        this.position = position;
        this.id = id;
    }
}
