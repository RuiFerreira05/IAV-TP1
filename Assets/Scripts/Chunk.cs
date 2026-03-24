using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    public int chunkSize = 16;
    public float densityThreshold = -0.3f;
    public float scale = 0.05f;
    public int octaves = 4;
    public int noiseOffsetX = 0;
    public int noiseOffsetY = 0;
    public int noiseOffsetZ = 0;
    public float offsetScale = 1;
    public int stoneHeight = 2;
    public int grassHeight = 7;
    public Block[,,] chunkData;
    public Material chunkMaterial;
    public WorldManager3D worldManager;
    public bool drawn = false;
    public float caveScale = 0.1f;
    public float caveThreshold = 0.65f;
    public float heightNoiseExponent = 1.5f;
    public float seaLevel = 4f;
    public float maxHeight = 40f;
    public float detailAmplitude = 1.5f;
    public int carvingThreshold = 5;

    //void Start()
    //{
    //    Generate(worldManager);
    //}

    public void Generate(WorldManager3D worldManager)
    {
        this.worldManager = worldManager;
        InitializeChunk();
    }

    bool HasSolidNeighbour(int x, int y, int z)
    {
        if (x >= 0 && x < chunkSize &&
            y >= 0 && y < chunkSize &&
            z >= 0 && z < chunkSize)
            return chunkData[x, y, z].isSolid;

        // Out of bounds � look up the neighboring chunk
        Vector3Int thisCoord = new Vector3Int(
            (int)transform.position.x / chunkSize,
            (int)transform.position.y / chunkSize,
            (int)transform.position.z / chunkSize
        );
        Vector3Int neighborCoord = new Vector3Int(
            thisCoord.x + (x < 0 ? -1 : x >= chunkSize ? 1 : 0),
            thisCoord.y + (y < 0 ? -1 : y >= chunkSize ? 1 : 0),
            thisCoord.z + (z < 0 ? -1 : z >= chunkSize ? 1 : 0)
        );

        Chunk neighbor = worldManager.GetChunk(neighborCoord);
        if (neighbor == null || neighbor.chunkData == null) return false;

        int localX = ((x % chunkSize) + chunkSize) % chunkSize;
        int localY = ((y % chunkSize) + chunkSize) % chunkSize;
        int localZ = ((z % chunkSize) + chunkSize) % chunkSize;
        return neighbor.chunkData[localX, localY, localZ].isSolid;
    }

    Block.BlockType NeighbourType(int x, int y, int z)
    {
        if (x >= 0 && x < chunkSize &&
            y >= 0 && y < chunkSize &&
            z >= 0 && z < chunkSize)
            return chunkData[x, y, z].type;

        Vector3Int thisCoord = new Vector3Int(
            (int)transform.position.x / chunkSize,
            (int)transform.position.y / chunkSize,
            (int)transform.position.z / chunkSize
        );
        Vector3Int neighborCoord = new Vector3Int(
            thisCoord.x + (x < 0 ? -1 : x >= chunkSize ? 1 : 0),
            thisCoord.y + (y < 0 ? -1 : y >= chunkSize ? 1 : 0),
            thisCoord.z + (z < 0 ? -1 : z >= chunkSize ? 1 : 0)
        );

        Chunk neighbor = worldManager.GetChunk(neighborCoord);
        if (neighbor == null || neighbor.chunkData == null) return Block.BlockType.NONE;

        int localX = ((x % chunkSize) + chunkSize) % chunkSize;
        int localY = ((y % chunkSize) + chunkSize) % chunkSize;
        int localZ = ((z % chunkSize) + chunkSize) % chunkSize;
        return neighbor.chunkData[localX, localY, localZ].type;
    }

    public void InitializeChunk()
    {
        chunkData = new Block[chunkSize, chunkSize, chunkSize];

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    Block.BlockType type;

                    float worldX = transform.position.x + x;
                    float worldY = transform.position.y + y;
                    float worldZ = transform.position.z + z;

                    // Three separate noise layers
                    float continentalness = FBm(worldX + noiseOffsetX, worldZ + noiseOffsetZ, 2, 0.005f);
                    float baseHeight = FBm(worldX + noiseOffsetX, worldZ + noiseOffsetZ, 4, 0.02f);
                    float detail = FBm(worldX + noiseOffsetX, worldZ + noiseOffsetZ, 6, 0.1f);

                    float finalHeight = Mathf.Lerp(seaLevel, maxHeight, continentalness * baseHeight)
                                      + detail * detailAmplitude;

                    float densityNoise = Perlin3D(
                        (worldX + noiseOffsetX) * offsetScale,
                        (worldY + noiseOffsetY) * offsetScale,
                        (worldZ + noiseOffsetZ) * offsetScale);

                    float finalDensity = finalHeight - worldY + densityNoise;
                    bool solid = finalDensity > densityThreshold;

                    bool cave_air = false;
                    if (solid && y > 1 && y < carvingThreshold)
                    {
                        float cx = (worldX + noiseOffsetX) * caveScale;
                        float cy = worldY * caveScale;
                        float cz = (worldZ + noiseOffsetZ) * caveScale;
                        float caveNoise = Perlin3D(cx, cy, cz);
                        if (caveNoise > caveThreshold)
                        {
                            solid = false;
                            cave_air = true;
                        }
                    }

                    if (solid)
                    {
                        if (worldY < stoneHeight)
                        {
                            type = Block.BlockType.STONE;
                        }
                        else
                        {
                            type = Block.BlockType.DIRT;
                        }
                    }
                    else
                    {
                        if (cave_air)
                        {
                            type = Block.BlockType.CAVE_AIR;
                        }
                        else
                        {
                            type = Block.BlockType.AIR;
                        }
                    }

                    chunkData[x, y, z] = new Block(type, new Vector3(x, y, z));
                }
            }
        }

        Vector3Int chunkPos = new Vector3Int(
            (int)transform.position.x / chunkSize,
            (int)transform.position.y / chunkSize,  // was missing Y entirely
            (int)transform.position.z / chunkSize
        );

        Vector3 wormStart = new Vector3(
            transform.position.x + chunkSize / 2f,
            transform.position.y + chunkSize / 2f,
            transform.position.z + chunkSize / 2f
        );

        CarveWorm(
            chunkData,
            chunkSize,
            chunkPos,
            wormStart,
            steps: 25,
            radius: 2f,
            stepSize: 2f,
            directionScale: 0.1f
        );

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    float worldY = transform.position.y + y;

                    if (chunkData[x, y, z].isSolid && !HasSolidNeighbour(x, y + 1, z) && worldY > grassHeight)
                    {
                        chunkData[x, y, z].type = Block.BlockType.GRASS;
                    }

                    if (chunkData[x, y, z].isSolid && !HasSolidNeighbour(x, y + 1, z))
                    {
                        if (NeighbourType(x, y+1, z) == Block.BlockType.AIR)
                        {

                            chunkData[x, y, z].type = Block.BlockType.GRASS;
                        }
                        else if(NeighbourType(x, y + 1, z) == Block.BlockType.CAVE_AIR)
                        {
                            chunkData[x, y, z].type = Block.BlockType.STONE;
                        }
                    }
                }
            }
        }
    }

    public static void CarveWorm(
        Block[,,] chunkData, int chunkSize,
        Vector3Int worldOffset,           // Vector3Int instead of Vector2Int
        Vector3 start, int steps, float radius,
        float stepSize, float directionScale)
    {
        Vector3 pos = start;
        for (int i = 0; i < steps; i++)
        {
            float nx = Perlin3D(pos.x * directionScale, pos.y * directionScale, pos.z * directionScale) * 2f - 1f;
            float ny = Perlin3D(pos.y * directionScale + 100f, pos.z * directionScale + 100f, pos.x * directionScale + 100f) * 2f - 1f;
            float nz = Perlin3D(pos.z * directionScale + 200f, pos.x * directionScale + 200f, pos.y * directionScale + 200f) * 2f - 1f;
            Vector3 dir = new Vector3(nx, ny * 0.5f, nz).normalized;
            pos += dir * stepSize;
            CarveAt(chunkData, chunkSize, worldOffset, pos, radius);
        }
    }

    static void CarveAt(
        Block[,,] chunkData, int chunkSize,
        Vector3Int worldOffset, Vector3 center, float radius)
    {
        int localX = Mathf.RoundToInt(center.x) - worldOffset.x * chunkSize;
        int localY = Mathf.RoundToInt(center.y) - worldOffset.y * chunkSize; // now correct
        int localZ = Mathf.RoundToInt(center.z) - worldOffset.z * chunkSize;
        int r = Mathf.CeilToInt(radius);
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                for (int dz = -r; dz <= r; dz++)
                {
                    if (dx * dx + dy * dy + dz * dz > radius * radius) continue;
                    int bx = localX + dx;
                    int by = localY + dy;
                    int bz = localZ + dz;
                    if (bx >= 0 && bx < chunkSize &&
                        by > 1 && by < chunkSize &&
                        bz >= 0 && bz < chunkSize)
                    {
                        chunkData[bx, by, bz] = new Block(Block.BlockType.CAVE_AIR, new Vector3(bx, by, bz));
                    }
                }
    }

    public void DrawChunk()
    {
        // 1. Criar listas partilhadas (vertices, triangles, uvs)
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // 2. Para cada bloco: adicionar TODAS as 6 faces
        for (int x = 0; x < chunkSize; x++)
            for (int y = 0; y < chunkSize; y++)
                for (int z = 0; z < chunkSize; z++)
                {
                    Block block = chunkData[x, y, z];
                    if (!block.isSolid) continue;

                    if (!HasSolidNeighbour(x, y, z + 1))
                        block.AddFaceToMeshData(Block.CubeFace.Front, vertices, triangles, uvs);
                    if (!HasSolidNeighbour(x, y, z - 1))
                        block.AddFaceToMeshData(Block.CubeFace.Back, vertices, triangles, uvs);
                    if (!HasSolidNeighbour(x, y + 1, z))
                        block.AddFaceToMeshData(Block.CubeFace.Top, vertices, triangles, uvs);
                    if (!HasSolidNeighbour(x, y - 1, z))
                        block.AddFaceToMeshData(Block.CubeFace.Bottom, vertices, triangles, uvs);
                    if (!HasSolidNeighbour(x - 1, y, z))
                        block.AddFaceToMeshData(Block.CubeFace.Left, vertices, triangles, uvs);
                    if (!HasSolidNeighbour(x + 1, y, z))
                        block.AddFaceToMeshData(Block.CubeFace.Right, vertices, triangles, uvs);
                }

        // 3. Criar Mesh, atribuir arrays
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        // 4. RecalculateNormals + RecalculateBounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 5. Atribuir ao MeshFilter e MeshRenderer
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = chunkMaterial;

        drawn = true;
    }

    public static float Perlin3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float yz = Mathf.PerlinNoise(y, z);
        float xz = Mathf.PerlinNoise(x, z);
        float yx = Mathf.PerlinNoise(y, x);
        float zy = Mathf.PerlinNoise(z, y);
        float zx = Mathf.PerlinNoise(z, x);
        return (xy + yz + xz + yx + zy + zx) / 6f;
    }

    public static float FBm(float x, float z, int octaves, float scale,
        float persistence = 0.5f, float lacunarity = 2.0f)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float totalAmplitude = 0f;
        for (int i = 0; i < octaves; i++)
        {
            value += Mathf.PerlinNoise(x * scale * frequency,
            z * scale * frequency) * amplitude;
            totalAmplitude += amplitude;
            amplitude *= persistence; // decresce a cada oitava
            frequency *= lacunarity; // cresce a cada oitava
        }
        return value / totalAmplitude; // normalizar para [0, 1]
    }
}