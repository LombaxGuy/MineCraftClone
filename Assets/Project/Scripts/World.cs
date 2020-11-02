using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    private bool applyingModifications = false;
    private Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    private bool inUI = false;

    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;

    [Header("Threading")]
    public bool enableThreading = true;

    private Thread chunkUpdateThread;
    public object chunkUpdateThreadLock = new object();

    [Header("Lighting")]
    public Color skyDay;
    public Color skyNight;

    [Range(0f, 1f)] public float globalLightLevel;

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

    private void OnDisable()
    {
        if (enableThreading)
        {
            chunkUpdateThread.Abort();
        }
    }

    private void Start()
    {
        Random.InitState(seed);

        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);

        if (enableThreading)
        {
            chunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            chunkUpdateThread.Start();
        }

        spawnPos = new Vector3(VoxelData.WorldSizeInVoxels / 2, VoxelData.chunkHeight - 50, VoxelData.WorldSizeInVoxels / 2);
        player.position = spawnPos;

        GenerateWorld();

        playerLastChunkCoordinate = GetChunkCoordinateFromPosition(player.position);

    }

    private void Update()
    {
        playerChunkCoordinate = GetChunkCoordinateFromPosition(player.position);

        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(skyNight, skyDay, globalLightLevel);

        if (!playerChunkCoordinate.Equals(playerLastChunkCoordinate))
        {
            CheckViewDistance();
        }

        if (chunksToCreate.Count > 0)
            CreateChunk();

        if (chunksToDraw.Count > 0)
        {
            if (chunksToDraw.Peek().IsEditable)
                chunksToDraw.Dequeue().CreateMesh();
        }

        // if threading is not enabled we run chunk updates on this thread
        if (!enableThreading)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }

        // enable debug screen
        if (Input.GetKeyDown(KeyCode.F3))
        {
            debugScreen.SetActive(!debugScreen.activeSelf);
        }
    }

    private void GenerateWorld()
    {
        int center = (VoxelData.worldSizeInChunks / 2);

        for (int x = center - VoxelData.viewDistanceInChunks; x < center + VoxelData.viewDistanceInChunks; x++)
        {
            for (int z = center - VoxelData.viewDistanceInChunks; z < center + VoxelData.viewDistanceInChunks; z++)
            {
                ChunkCoordinate newChunk = new ChunkCoordinate(x, z);

                chunks[x, z] = new Chunk(newChunk, this);
                chunksToCreate.Add(newChunk);
            }
        }

        CheckViewDistance();
    }

    private void CreateChunk()
    {
        ChunkCoordinate c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);

        chunks[c.x, c.z].Initialize();
    }

    private void UpdateChunks()
    {
        bool updated = false;
        int index = 0;

        lock (chunkUpdateThreadLock)
        {
            while (!updated && index < chunksToUpdate.Count - 1)
            {
                if (chunksToUpdate[index].IsEditable)
                {
                    chunksToUpdate[index].UpdateChunk();
                    activeChunks.Add(chunksToUpdate[index].coordinate);
                    chunksToUpdate.RemoveAt(index);
                    updated = true;
                }
                else
                {
                    index++;
                }
            }
        }
    }

    private void ThreadedUpdate()
    {
        while (true)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }
    }

    private void ApplyModifications()
    {
        applyingModifications = true;

        while (modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0)
            {
                VoxelMod v = queue.Dequeue();

                ChunkCoordinate c = GetChunkCoordinateFromPosition(v.position);

                if (chunks[c.x, c.z] == null)
                {
                    chunks[c.x, c.z] = new Chunk(c, this);
                    chunksToCreate.Add(c);
                }

                chunks[c.x, c.z].modifications.Enqueue(v);
            }
        }

        applyingModifications = false;
    }

    private void CheckViewDistance()
    {
        playerLastChunkCoordinate = playerChunkCoordinate;

        ChunkCoordinate coordinate = GetChunkCoordinateFromPosition(player.position);
        List<ChunkCoordinate> previouslyActiveChunks = new List<ChunkCoordinate>(activeChunks);

        activeChunks.Clear();

        // loop through all chunks within view distance
        for (int x = coordinate.x - VoxelData.viewDistanceInChunks; x < coordinate.x + VoxelData.viewDistanceInChunks; x++)
        {
            for (int z = coordinate.z - VoxelData.viewDistanceInChunks; z < coordinate.z + VoxelData.viewDistanceInChunks; z++)
            {
                ChunkCoordinate currentCoord = new ChunkCoordinate(x, z);

                // if current chunk is in the world...
                if (IsChunkInWorld(currentCoord))
                {
                    // ... if it doesn't exist, create it
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(currentCoord, this);
                        chunksToCreate.Add(currentCoord);
                    }
                    // ... if it does exist but is inactive, activate it
                    else if (!chunks[x, z].IsActive)
                    {
                        chunks[x, z].IsActive = true;
                    }

                    // add the current chunk to the active chunks list
                    activeChunks.Add(currentCoord);
                }

                // loop through previously active chunks and remove this chunk if it is there
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(currentCoord))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        // chunks left in previously active chunks should be disabled as they are now outside of the view distance
        foreach (var c in previouslyActiveChunks)
        {
            chunks[c.x, c.z].IsActive = false;
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
                    modifications.Enqueue(Structure.MakeTree(biome.minTreeHeight, biome.maxTreeHeight, position));
                }
            }
        }

        return voxelValue;
    }

    public bool CheckForVoxel(Vector3 position)
    {
        ChunkCoordinate thisChunk = new ChunkCoordinate(position);
        
        if (!IsVoxelInWorld(position))
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].IsEditable)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromPosition(position).id].isSolid;

        return blockTypes[GetVoxel(position)].isSolid;
    }

    public VoxelState GetVoxelState (Vector3 position)
    {
        ChunkCoordinate thisChunk = new ChunkCoordinate(position);

        if (!IsVoxelInWorld(position))
            return null;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].IsEditable)
            return chunks[thisChunk.x, thisChunk.z].GetVoxelFromPosition(position);

        return new VoxelState(GetVoxel(position));
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
