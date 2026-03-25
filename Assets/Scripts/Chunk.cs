using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    [Header("Core Settings")]
    public int chunkSize = 16;
    public Material chunkMaterial;

    [HideInInspector] public Block[,,] chunkData;
    [HideInInspector] public WorldManager3D worldManager;
    [HideInInspector] public bool drawn = false;

    [Header("Terrain Heights")]
    public float seaLevel = 4f;
    public float maxHeight = 80f;
    public float dirtThickness = 3f;

    [Header("Base 3D Noise (Density)")]
    public float densityThreshold = -0.3f;
    public float scale = 0.05f;
    public int octaves = 4;
    public float offsetScale = 1;
    [HideInInspector] public float noiseOffsetX = 14721647f;
    [HideInInspector] public float noiseOffsetY = 46895169f;
    [HideInInspector] public float noiseOffsetZ = 55897124f;

    [Header("2D Height Map Noise")]
    public float heightNoiseExponent = 1.5f;
    public int continentalnessOctaves = 2;
    public float continentalnessScale = 0.005f;
    public int baseHeightOctaves = 4;
    public float baseHeightScale = 0.02f;
    public int detailOctaves = 6;
    public float detailScale = 0.1f;
    public float detailAmplitude = 1.5f;

    [Header("Cave System Noise")]
    public float caveScale = 0.1f;
    public float caveThreshold = 0.65f;
    public int minCarvingHeight = 1;
    public int carvingThreshold = 5;

    [Header("Cave Worm Settings")]
    public int wormSteps = 25;
    public float wormRadius = 2f;
    public float wormStepSize = 2f;
    public float wormDirectionScale = 0.1f;
    public float wormVerticalBias = 0.5f;
    public float wormNoiseOffsetNy = 100f;
    public float wormNoiseOffsetNz = 200f;

    bool HasSolidNeighbour(int x, int y, int z)
    {
        if (x >= 0 && x < chunkSize &&
            y >= 0 && y < chunkSize &&
            z >= 0 && z < chunkSize)
            return chunkData[x, y, z].isSolid;

        if (worldManager == null) return false;

        // Out of bounds - look up the neighboring chunk
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

        if (worldManager == null) return Block.BlockType.NONE;

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

    public void GenerateVoxelData()
    {
        chunkData = new Block[chunkSize, chunkSize, chunkSize];

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float worldX = transform.position.x + x;
                float worldZ = transform.position.z + z;

                float continentalness = FBm(worldX + noiseOffsetX, worldZ + noiseOffsetZ, continentalnessOctaves, continentalnessScale);
                float baseHeight = FBm(worldX + noiseOffsetX, worldZ + noiseOffsetZ, baseHeightOctaves, baseHeightScale);
                float detail = FBm(worldX + noiseOffsetX, worldZ + noiseOffsetZ, detailOctaves, detailScale);

                float finalHeight = Mathf.Lerp(seaLevel, maxHeight, continentalness * baseHeight) + detail * detailAmplitude;

                for (int y = 0; y < chunkSize; y++)
                {
                    Block.BlockType type;
                    float worldY = transform.position.y + y;

                    float densityNoise = Perlin3D((worldX + noiseOffsetX) * offsetScale, (worldY + noiseOffsetY) * offsetScale, (worldZ + noiseOffsetZ) * offsetScale);

                    float finalDensity = finalHeight - worldY + densityNoise;
                    bool solid = finalDensity > densityThreshold;

                    bool cave_air = false;
                    if (solid && y > minCarvingHeight && y < carvingThreshold)
                    {
                        float cx = (worldX + noiseOffsetX) * caveScale;
                        float cy = worldY * caveScale;
                        float cz = (worldZ + noiseOffsetZ) * caveScale;
                        if (Perlin3D(cx, cy, cz) > caveThreshold)
                        {
                            solid = false;
                            cave_air = true;
                        }
                    }

                    if (solid)
                    {
                        if (finalDensity > densityThreshold + dirtThickness)
                        {
                            type = Block.BlockType.STONE;
                        }
                        else
                        {
                            type = Block.BlockType.DIRT;
                        }
                    } 
                    else type = cave_air ? Block.BlockType.CAVE_AIR : Block.BlockType.AIR;

                    chunkData[x, y, z] = new Block(type, new Vector3(x, y, z));
                }
            }
        }

        Vector3Int chunkPos = new Vector3Int((int)transform.position.x / chunkSize, (int)transform.position.y / chunkSize, (int)transform.position.z / chunkSize);

        // Calculate how far a worm can possibly travel (in chunks)
        int searchRadius = Mathf.CeilToInt((wormSteps * wormStepSize) / chunkSize);

        // Simulate worms starting from THIS chunk, AND all chunks within travel radius!
        for (int cx = -searchRadius; cx <= searchRadius; cx++)
        {
            for (int cy = -searchRadius; cy <= searchRadius; cy++)
            {
                for (int cz = -searchRadius; cz <= searchRadius; cz++)
                {
                    // Calculate the world grid position of this specific neighbor
                    Vector3Int neighborWormChunkPos = chunkPos + new Vector3Int(cx, cy, cz);

                    // Start the worm in the center of that neighbor chunk
                    Vector3 wormStart = new Vector3(
                        neighborWormChunkPos.x * chunkSize + chunkSize / 2f,
                        neighborWormChunkPos.y * chunkSize + chunkSize / 2f,
                        neighborWormChunkPos.z * chunkSize + chunkSize / 2f
                    );

                    // CarveWorm will trace the path, but CarveAt naturally ignores 
                    // any blocks that fall outside of OUR local chunkData array!
                    CarveWorm(
                        chunkData, chunkSize, chunkPos, wormStart,
                        wormSteps, wormRadius, wormStepSize,
                        wormDirectionScale, wormVerticalBias,
                        wormNoiseOffsetNy, wormNoiseOffsetNz
                    );
                }
            }
        }
    }

    public void DecorateChunk()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if (!chunkData[x, y, z].isSolid) continue;

                    float worldY = transform.position.y + y;
                    Block.BlockType aboveType = NeighbourType(x, y + 1, z);

                    // If a DIRT block is exposed to the sky, it grows GRASS.
                    if (aboveType == Block.BlockType.AIR && chunkData[x, y, z].type == Block.BlockType.DIRT)
                    {
                        if (worldY >= seaLevel)
                        {
                            chunkData[x, y, z].type = Block.BlockType.GRASS;
                        }
                    }
                    // Prevent cave walls/roofs from generating as dirt
                    else if (aboveType == Block.BlockType.CAVE_AIR ||
                             NeighbourType(x - 1, y, z) == Block.BlockType.CAVE_AIR ||
                             NeighbourType(x + 1, y, z) == Block.BlockType.CAVE_AIR ||
                             NeighbourType(x, y, z - 1) == Block.BlockType.CAVE_AIR ||
                             NeighbourType(x, y, z + 1) == Block.BlockType.CAVE_AIR)
                    {
                        chunkData[x, y, z].type = Block.BlockType.STONE;
                    }
                }
            }
        }
    }

    public static void CarveWorm(
        Block[,,] chunkData, int chunkSize,
        Vector3Int worldOffset,
        Vector3 start, int steps, float radius,
        float stepSize, float directionScale,
        float verticalBias, float offsetNy, float offsetNz)
    {
        Vector3 pos = start;
        for (int i = 0; i < steps; i++)
        {
            float nx = Perlin3D(pos.x * directionScale, pos.y * directionScale, pos.z * directionScale) * 2f - 1f;
            float ny = Perlin3D(pos.y * directionScale + offsetNy, pos.z * directionScale + offsetNy, pos.x * directionScale + offsetNy) * 2f - 1f;
            float nz = Perlin3D(pos.z * directionScale + offsetNz, pos.x * directionScale + offsetNz, pos.y * directionScale + offsetNz) * 2f - 1f;
            Vector3 dir = new Vector3(nx, ny * verticalBias, nz).normalized;
            pos += dir * stepSize;
            CarveAt(chunkData, chunkSize, worldOffset, pos, radius);
        }
    }

    static void CarveAt(
        Block[,,] chunkData, int chunkSize,
        Vector3Int worldOffset, Vector3 center, float radius)
    {
        int localX = Mathf.RoundToInt(center.x) - worldOffset.x * chunkSize;
        int localY = Mathf.RoundToInt(center.y) - worldOffset.y * chunkSize;
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
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

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

        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (Application.isPlaying)
        {
            meshFilter.mesh = mesh;
        }
        else
        {
            meshFilter.sharedMesh = mesh;
        }

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
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        return value / totalAmplitude;
    }
}