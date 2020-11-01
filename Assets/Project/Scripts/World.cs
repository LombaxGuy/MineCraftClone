using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking.Types;

public class World : MonoBehaviour
{
    public int seed;
    public BiomeAttributes biome;

    public Transform player;
    public Vector3 spawnPos;

    public Material material;
    public BlockType[] blockTypes;

    private Chunk[,] chunks = new Chunk[VoxelData.worldSizeInChunks, VoxelData.worldSizeInChunks];

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate>();
    
    public ChunkCoordinate playerChunkCoordinate;
    private ChunkCoordinate playerLastChunkCoordinate;

    private List<ChunkCoordinate> chunksToCreate = new List<ChunkCoordinate>();
    private List<Chunk> chunksToUpdate = new List<Chunk>();

    private bool applyingModifications = false;

    private Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    private bool inUI = false;

    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;

    [Header("Debug")]
    public GameObject debugScreen;

    public bool InUI
    {
        get => inUI;
        set
        {
            inUI = value;

            if (inUI)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
    }

    private void Start()
    {
        Random.InitState(seed);

        spawnPos = new Vector3(VoxelData.WorldSizeInVoxels / 2, VoxelData.chunkHeight - 50, VoxelData.WorldSizeInVoxels / 2);
        player.position = spawnPos;

        GenerateWorld();

        playerLastChunkCoordinate = GetChunkCoordinateFromPosition(player.position);
    }

    private void Update()
    {
        playerChunkCoordinate = GetChunkCoordinateFromPosition(player.position);

        if (!playerChunkCoordinate.Equals(playerLastChunkCoordinate))
        {
            CheckViewDistance();
        }

        if (modifications.Count > 0 && !applyingModifications)
            StartCoroutine(ApplyModifications());

        if (chunksToCreate.Count > 0)
            CreateChunk();

        if (chunksToUpdate.Count > 0)
            UpdateChunks();

        if (Input.GetKeyDown(KeyCode.F3))
        {
            debugScreen.SetActive(!debugScreen.activeSelf);
        }
    }

    public byte GetVoxel(Vector3 position)
    {
        int y = Mathf.FloorToInt(position.y);

        // if outside world, return 0 (air block)
        if (!IsVoxelInWorld(position))
            return 0;

        // if at the bottom of world, return 1 (bedrock)
        if (y == 0)
            return 1;

        // first terrain pass (basic terrain shape)

        int terrainHeight = Mathf.FloorToInt(Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.terrainScale) * biome.terrainHeight) + biome.solidGroundHeight;
        byte voxelValue = 0;

        // if at top layer of terrain, return 4 (grass block)
        if (y == terrainHeight)
            voxelValue = 4;
        // if less than 4 from top layer, retrun 3 (dirt block)
        else if (y < terrainHeight && y > terrainHeight - 4)
            voxelValue = 3;
        // if above terrain, return 0 (air block)
        else if (y > terrainHeight)
            return 0;
        // if none of the above, return 2 (stone block)
        else
            voxelValue =  2;

        // second terrain pass (adding ore lodes)

        if (voxelValue == 2)
        {
            foreach (var lode in biome.lodes)
            {
                if (y > lode.minHeight && y < lode.maxHeight)
                {
                    if (Noise.Get3DPerlin(position, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
                }
            }
        }

        // foliage pass (Trees)

        if (y == terrainHeight)
        {
            if (Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.treeZoneScale) > biome.treeZoneThreshold)
            {
                if (Noise.Get2DPerlin(new Vector2(position.x, position.z), 250, biome.treePlacementScale) > biome.treePlacementThreshold)
                {
                    Structure.MakeTree(biome.minTreeHeight, biome.maxTreeHeight, position, modifications);
                }
            }
        }

        return voxelValue;
    }

    public bool CheckForVoxel(Vector3 position)
    {
        ChunkCoordinate thisChunk = new ChunkCoordinate(position);
        
        if (!IsVoxelInWorld(position))
        //if (!IsChunkInWorld(thisChunk) || position.y < 0 || position.y > VoxelData.chunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromPosition(position)].isSolid;

        return blockTypes[GetVoxel(position)].isSolid;
    }

    public bool CheckIfVoxelTransparent(Vector3 position)
    {
        ChunkCoordinate thisChunk = new ChunkCoordinate(position);

        if (!IsVoxelInWorld(position))
            //if (!IsChunkInWorld(thisChunk) || position.y < 0 || position.y > VoxelData.chunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromPosition(position)].isTransparent;

        return blockTypes[GetVoxel(position)].isTransparent;
    }

    private void GenerateWorld()
    {
        int center = (VoxelData.worldSizeInChunks / 2);
        
        for (int x = center - VoxelData.viewDistanceInChunks; x < center + VoxelData.viewDistanceInChunks; x++)
        {
            for (int z = center - VoxelData.viewDistanceInChunks; z < center + VoxelData.viewDistanceInChunks; z++)
            {
                ChunkCoordinate cc = new ChunkCoordinate(x, z);

                chunks[x, z] = new Chunk(cc, this, true);

                activeChunks.Add(cc);
            }
        }

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();

            ChunkCoordinate c = GetChunkCoordinateFromPosition(v.position);
            
            if (chunks[c.x, c.z] == null)
            {
                chunks[c.x, c.z] = new Chunk(c, this, true);
                activeChunks.Add(c);
            }

            chunks[c.x, c.z].modifications.Enqueue(v);

            if (!chunksToUpdate.Contains(chunks[c.x, c.z]))
            {
                chunksToUpdate.Add(chunks[c.x, c.z]);
            }
        }

        for (int i = 0; i < chunksToUpdate.Count; i++)
        {
            chunksToUpdate[0].UpdateChunk();
            chunksToUpdate.RemoveAt(0);
        }
    }

    private void CreateChunk()
    {
        ChunkCoordinate c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);

        activeChunks.Add(c);
        chunks[c.x, c.z].Initialize();
    }

    private void UpdateChunks()
    {
        bool updated = false;
        int index = 0;

        while (!updated && index < chunksToUpdate.Count - 1)
        {
            if (chunksToUpdate[index].isVoxelMapPopulated)
            {
                chunksToUpdate[index].UpdateChunk();
                chunksToUpdate.RemoveAt(index);
                updated = true;
            }
            else
            {
                index++;
            }
        }
    }

    private IEnumerator ApplyModifications()
    {
        applyingModifications = true;
        int count = 0;

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();

            ChunkCoordinate c = GetChunkCoordinateFromPosition(v.position);

            if (chunks[c.x, c.z] == null)
            {
                chunks[c.x, c.z] = new Chunk(c, this, true);
                activeChunks.Add(c);
            }

            chunks[c.x, c.z].modifications.Enqueue(v);

            if (!chunksToUpdate.Contains(chunks[c.x, c.z]))
            {
                chunksToUpdate.Add(chunks[c.x, c.z]);
            }

            count++;

            if (count > 200)
            {
                count = 0;
                yield return null;
            }
        }

        applyingModifications = false;
    }

    private void CheckViewDistance()
    {
        playerLastChunkCoordinate = playerChunkCoordinate;
        
        ChunkCoordinate coordinate = GetChunkCoordinateFromPosition(player.position);

        List<ChunkCoordinate> previouslyActiveChunks = new List<ChunkCoordinate>(activeChunks);

        for (int x = coordinate.x - VoxelData.viewDistanceInChunks; x < coordinate.x + VoxelData.viewDistanceInChunks; x++)
        {
            for (int z = coordinate.z - VoxelData.viewDistanceInChunks; z < coordinate.z + VoxelData.viewDistanceInChunks; z++)
            {
                ChunkCoordinate currentCoordinate = new ChunkCoordinate(x, z);
                
                // if the chunck with the coordinate is outside the world we don't do anything
                if (IsChunkInWorld(currentCoordinate))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(currentCoordinate, this, false);
                        chunksToCreate.Add(currentCoordinate);
                    }
                    else if (!chunks[x, z].IsActive)
                    {
                        chunks[x, z].IsActive = true;
                    }
                    activeChunks.Add(currentCoordinate);
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(currentCoordinate))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        foreach (var cc in previouslyActiveChunks)
        {
            chunks[cc.x, cc.z].IsActive = false;
        }
    }

    private ChunkCoordinate GetChunkCoordinateFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(position.z / VoxelData.chunkWidth);
        return new ChunkCoordinate(x, z);
    }

    public Chunk GetChunkFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(position.z / VoxelData.chunkWidth);
        return chunks[x, z];
    }

    private bool IsChunkInWorld(ChunkCoordinate coordinate)
    {
        if (coordinate.x > 0 && coordinate.x < VoxelData.worldSizeInChunks - 1 &&
            coordinate.z > 0 && coordinate.z < VoxelData.worldSizeInChunks - 1)
            return true;
        else
            return false;
    }

    private bool IsVoxelInWorld(Vector3 position)
    {
        if (position.x >= 0 && position.x < VoxelData.WorldSizeInVoxels &&
            position.y >= 0 && position.y < VoxelData.chunkHeight &&
            position.z >= 0 && position.z < VoxelData.WorldSizeInVoxels)
            return true;
        else
            return false;
    }
}
