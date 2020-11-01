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
    private List<int> transparentTriangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    public byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    private World world;

    private bool isActive;

    public bool isVoxelMapPopulated = false;

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
    public Vector3 Position { get => chunkObject.transform.position; }

    public Chunk(ChunkCoordinate coordinate, World world, bool generateOnLoad)
    {
        this.coordinate = coordinate;
        this.world = world;

        isActive = true;

        if (generateOnLoad)
            Initialize();
    }

    public void Initialize()
    {
        chunkObject = new GameObject("Chunk" + coordinate.ToString());

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        meshRenderer.material = world.material;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coordinate.x * VoxelData.chunkWidth, 0, coordinate.z * VoxelData.chunkWidth);

        PopulateVoxelMap();
        UpdateChunk();
    }

    private void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.chunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.chunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + Position);
                }
            }
        }

        isVoxelMapPopulated = true;
    }

    public void UpdateChunk()
    {
        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position - Position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z] = v.id;
        }

        ClearMeshData();

        for (int y = 0; y < VoxelData.chunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.chunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                    {
                        UpdateMeshData(new Vector3(x, y, z));
                    }
                }
            }
        }

        CreateMesh();
    }

    private void ClearMeshData()
    {
        vertexIndex = 0;
        verticies.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
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

        voxelMap[xCheck, yCheck, zCheck] = newID;

        // update surrounding chunks
        UpdateSurrondingVoxels(new Vector3(xCheck, yCheck, zCheck));

        UpdateChunk();
    }

    private void UpdateSurrondingVoxels(Vector3 thisVoxel)
    {
        for (int i = 0; i < 6; i++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[i];

            if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                world.GetChunkFromPosition(currentVoxel + Position).UpdateChunk();
            }
        }
    }

    private bool CheckVoxel(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        if (!IsVoxelInChunk(x, y, z))
            return world.CheckIfVoxelTransparent(position + Position);

        return world.blockTypes[voxelMap[x, y, z]].isTransparent;
    }

    public byte GetVoxelFromPosition(Vector3 position)
    {
        // find the position of the voxel position belongs to
        int xCheck = Mathf.FloorToInt(position.x);
        int yCheck = Mathf.FloorToInt(position.y);
        int zCheck = Mathf.FloorToInt(position.z);

        // subtract the position of the chunk object to get the relative position
        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        // return the block at the the calculated position
        return voxelMap[xCheck, yCheck, zCheck];
    }

    private void UpdateMeshData(Vector3 position)
    {
        byte blockID = voxelMap[(int)position.x, (int)position.y, (int)position.z];
        bool isTransparent = world.blockTypes[blockID].isTransparent;

        // hardcoded to 6 as we only have blocks
        for (int i = 0; i < 6; i++)
        {
            if (CheckVoxel(position + VoxelData.faceChecks[i]))
            {
                CreateFace(blockID, i, isTransparent, position);
            }
        }
    }

    private void CreateFace(int blockID, int faceIndex, bool isTransparent, Vector3 position)
    {
        // adding face verticies to the vertices list
        verticies.Add(position + VoxelData.vertices[VoxelData.triangles[faceIndex, 0]]);
        verticies.Add(position + VoxelData.vertices[VoxelData.triangles[faceIndex, 1]]);
        verticies.Add(position + VoxelData.vertices[VoxelData.triangles[faceIndex, 2]]);
        verticies.Add(position + VoxelData.vertices[VoxelData.triangles[faceIndex, 3]]);

        // adding uvs to the uvs list
        AddTexture(world.blockTypes[blockID].GetTextureID(faceIndex));

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex);

        // increment vertex index by 4
        vertexIndex += 4;
    }

    private void CreateMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = verticies.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);

        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

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