using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure
{
    public static void MakeTree(int minTrunkHeight, int maxTrunkHeight, Vector3 position, Queue<VoxelMod> queue)
    {
        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z), 500, 3f));

        if (height < minTrunkHeight)
            height = minTrunkHeight;

        // create the trunk
        for (int i = 1; i < height; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 6));
        }

        for (int x = -2; x <= 2; x++)
        {
            for (int y = height - 3; y < height; y++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    // don't replace the trunk
                    if (x != 0 || z != 0)
                    {
                        queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y, position.z + z), 11));
                    }
                }
            }
        }


    }
}
