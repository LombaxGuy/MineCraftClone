using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure
{
    public static Queue<VoxelMod> MakeTree(int minTrunkHeight, int maxTrunkHeight, Vector3 position)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        float rawTrunkHeight = Mathf.Lerp(minTrunkHeight, maxTrunkHeight, Noise.Get2DPerlin(new Vector2(position.x, position.z), 500, 3f));

        int trunkHeight = (int)rawTrunkHeight;

        if (trunkHeight < minTrunkHeight)
            trunkHeight = minTrunkHeight;

        // create the trunk
        for (int i = 1; i <= trunkHeight; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 6));
        }

        // add a leaf block on top of the trunk
        queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + trunkHeight + 1, position.z), 11));


        for (int x = -2; x <= 2; x++)
        {
            for (int y = trunkHeight - 2; y <= trunkHeight; y++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    // if x and z is 0, continue to the next loop (to not replace the trunk)
                    if (x == 0 && z == 0)
                        continue;

                    // add leaves around the trunk, until y is equal to trunk height
                    if (y < trunkHeight)
                    {
                        queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y, position.z + z), 11));
                    }
                    // add leaves around the top of the trunk
                    else if (y == trunkHeight)
                    {
                        // make the top a bit smaller than the layer below
                        if (x >= -1 && x <= 1 && z >= -1 && z <= 1)
                        {
                            queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y, position.z + z), 11));

                            // if either x or z is 0, add leaves above the block too, makes the "+" shape at the top of the tree
                            if (x == 0 || z == 0)
                            {
                                queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y + 1, position.z + z), 11));
                            }
                        }
                    }
                }
            }
        }

        return queue;
    }
}
