using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoordinate coordinate;

    private GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private int vertexIndex = 0;
    private List<Vector3> verticies = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Color> colors = new List<Color>();
    private List<Vector3> normals = new List<Vector3>();

    public VoxelState[,,] voxelMap = new VoxelState[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    private World world;

    private bool isActive;

    private bool isVoxelMapPopulated = false;

    public bool IsActive
    {
        get => isActive;
        set
        {
            isActive = value;

            if (chunkObject != null)
                chunkObject.SetActive(value);
        }
    }

    public Vector3 ChunkPosition { get; private set; }

    public bool IsEditable { get => isVoxelMapPopulated; }

    public Chunk(ChunkCoordinate coordinate, World world)
    {
        this.coordinate = coordinate;
        this.world = world;
    }

    public void Initialize()
    {
        chunkObject = new GameObject("Chunk" + coordinate.ToString());

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        meshRenderer.material = world.material;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coordinate.x * VoxelData.chunkWidth, 0, coordinate.z * VoxelData.chunkWidth);
        ChunkPosition = chunkObject.transform.position;

        PopulateVoxelMap();
    }

    private void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.chunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.chunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    voxelMap[x, y, z] = new VoxelState(world.GetVoxel(new Vector3(x, y, z) + ChunkPosition));
                }
            }
        }

        isVoxelMapPopulated = true;

        lock (world.chunkUpdateThreadLock)
        {
            world.chunksToUpdate.Add(this);
        }
    }

    public void UpdateChunk()
    {
        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position - ChunkPosition;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z].id = v.id;
        }

        ClearMeshData();

        CalculateLight();

        for (int y = 0; y < VoxelData.chunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.chunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z].id].isSolid)
                    {
                        UpdateMeshData(new Vector3(x, y, z));
                    }
                }
            }
        }

        world.chunksToDraw.Enqueue(this);
    }

    private void CalculateLight()
    {
        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

        for (int x = 0; x < VoxelData.chunkWidth; x++)
        {
            for (int z = 0; z < VoxelData.chunkWidth; z++)
            {
                float skyLight = 1f;

                for (int y = VoxelData.chunkHeight - 1; y >= 0; y--)
                {
                    VoxelState thisVoxel = voxelMap[x, y, z];

                    if (thisVoxel.id > 0 && skyLight > 0)
                    {
                        skyLight = Mathf.Clamp(skyLight - (1 - world.blockTypes[thisVoxel.id].transparency), 0, 1);
                    }

                    thisVoxel.globalLightPercent = skyLight;

                    voxelMap[x, y, z] = thisVoxel;

                    if (skyLight > VoxelData.lightFalloff)
                        litVoxels.Enqueue(new Vector3Int(x, y, z));
                }
            }
        }

        while (litVoxels.Count > 0)
        {
            Vector3Int v = litVoxels.Dequeue();

            for (int i = 0; i < 6; i++)
            {
                Vector3 currentVoxel = v + VoxelData.faceChecks[i];
                Vector3Int neighbor = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

                if (IsVoxelInChunk(neighbor.x, neighbor.y, neighbor.z))
                {
                    // if neighbor voxel is darker than thisvoxel, we can illuminate it
                    if (voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff)
                    {
                        voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent = voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;

                        if (voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff)
                        {
                            litVoxels.Enqueue(neighbor);
                        }
                    }
                }
            }
        }
    }

    private void ClearMeshData()
    {
        vertexIndex = 0;
        verticies.Clear();
        triangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
    }

    private bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1)
            return false;
        else
            return true;
    }

    public void EditVoxel(Vector3 position, byte newID)
    {
        // find the position of the voxel position belongs to
        int xCheck = Mathf.FloorToInt(position.x);
        int yCheck = Mathf.FloorToInt(position.y);
        int zCheck = Mathf.FloorToInt(position.z);

        // subtract the position of the chunk object to get the relative position
        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        voxelMap[xCheck, yCheck, zCheck].id = newID;

        lock (world.chunkUpdateThreadLock)
        {
            world.chunksToUpdate.Insert(0, this);

            // update surrounding chunks
            UpdateSurrondingVoxels(new Vector3(xCheck, yCheck, zCheck));
        }
    }

    private void UpdateSurrondingVoxels(Vector3 thisVoxel)
    {
        for (int i = 0; i < 6; i++)
        {
            Vector3 neighbourVoxelPosition = thisVoxel + VoxelData.faceChecks[i];

            if (!IsVoxelInChunk((int)neighbourVoxelPosition.x, (int)neighbourVoxelPosition.y, (int)neighbourVoxelPosition.z))
            {
                world.chunksToUpdate.Insert(0, world.GetChunkFromPosition(neighbourVoxelPosition + ChunkPosition));
            }
        }
    }

    private VoxelState CheckVoxel(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        if (!IsVoxelInChunk(x, y, z))
            return world.GetVoxelState(position + ChunkPosition);

        return voxelMap[x, y, z];
    }

    public VoxelState GetVoxelFromPosition(Vector3 position)
    {
        // find the position of the voxel position belongs to
        int xCheck = Mathf.FloorToInt(position.x);
        int yCheck = Mathf.FloorToInt(position.y);
        int zCheck = Mathf.FloorToInt(position.z);

        // subtract the position of the chunk object to get the relative position
        xCheck -= Mathf.FloorToInt(ChunkPosition.x);
        zCheck -= Mathf.FloorToInt(ChunkPosition.z);

        // return the block at the the calculated position
        return voxelMap[xCheck, yCheck, zCheck];
    }

    private void UpdateMeshData(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        byte blockID = voxelMap[x, y, z].id;

        // hardcoded to 6 as we only have blocks
        for (int i = 0; i < 6; i++)
        {
            VoxelState neighbour = CheckVoxel(position + VoxelData.faceChecks[i]);

            if (neighbour != null && world.blockTypes[neighbour.id].renderNeighbourFaces)
            {
                CreateFace(blockID, i, position, neighbour);
            }
        }
    }

    private void CreateFace(int blockID, int faceIndex, Vector3 position, VoxelState neighbour)
    {
        // adding face verticies to the vertices list
        verticies.Add(position + VoxelData.vertices[VoxelData.triangles[faceIndex, 0]]);
        verticies.Add(position + VoxelData.vertices[VoxelData.triangles[faceIndex, 1]]);
        verticies.Add(position + VoxelData.vertices[VoxelData.triangles[faceIndex, 2]]);
        verticies.Add(position + VoxelData.vertices[VoxelData.triangles[faceIndex, 3]]);

        normals.Add(VoxelData.faceChecks[faceIndex]);
        normals.Add(VoxelData.faceChecks[faceIndex]);
        normals.Add(VoxelData.faceChecks[faceIndex]);
        normals.Add(VoxelData.faceChecks[faceIndex]);

        // adding uvs to the uvs list
        AddTexture(world.blockTypes[blockID].GetTextureID(faceIndex));

        // setting local light level of face
        float lightLevel = neighbour.globalLightPercent;

        colors.Add(new Color(0, 0, 0, lightLevel));
        colors.Add(new Color(0, 0, 0, lightLevel));
        colors.Add(new Color(0, 0, 0, lightLevel));
        colors.Add(new Color(0, 0, 0, lightLevel));

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex);

        // increment vertex index by 4
        vertexIndex += 4;
    }

    public void CreateMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = verticies.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

        meshFilter.mesh = mesh;
    }

    private void AddTexture(int textureID)
    {
        float x = (textureID % VoxelData.textureAtlasSizeInBlocks) * VoxelData.NormalizedBlockTextureSize;
        float y = (VoxelData.textureAtlasSizeInBlocks - Mathf.Floor(textureID / VoxelData.textureAtlasSizeInBlocks)) * VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y - VoxelData.NormalizedBlockTextureSize));   // bot-left
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));                                          // top-left
        uvs.Add(new Vector2(x, y));                                                                                 // top-right
        uvs.Add(new Vector2(x, y - VoxelData.NormalizedBlockTextureSize));                                          // bot-right
    }
}

public class ChunkCoordinate : IEquatable<ChunkCoordinate>
{
    public int x;
    public int z;

    public ChunkCoordinate()
    {
        x = 0;
        z = 0;
    }

    public ChunkCoordinate(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public ChunkCoordinate(Vector3 position)
    {
        int xCheck = Mathf.FloorToInt(position.x);
        int zCheck = Mathf.FloorToInt(position.z);

        x = xCheck / VoxelData.chunkWidth;
        z = zCheck / VoxelData.chunkWidth;
    }

    public bool Equals(ChunkCoordinate other)
    {
        if (other == null)
            return false;

        if (other.x == x && other.z == z)
            return true;
        else
            return false;
    }

    public override string ToString()
    {
        return string.Format("({0},{1})", x, z);
    }
}