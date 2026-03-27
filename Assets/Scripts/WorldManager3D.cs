// WorldManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class WorldManager3D : MonoBehaviour
{
	[Header("Referências")]
	public Transform player;
	public GameObject chunkPrefab;
	public Material chunkMaterial;

	[Header("Configuração")]
	public int renderDistance = 3;
	public int chunkSize = 16;
	public int chunksPerFrame = 2;
	public int chunksDown = 0;   // how many chunk layers below Y=0
	public int chunksUp = 3;     // how many chunk layers above Y=0
	public bool removeFarChunks = true;

    // Changed from Vector2Int to Vector3Int
    public Dictionary<Vector3Int, GameObject> activeChunks = new();
	private Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);


	private float noiseOffsetX_random;
	private float noiseOffsetY_random;
	private float noiseOffsetZ_random;

	public int gridSize = 5;

	private Coroutine buildRoutine;
    private Coroutine decorateRoutine;
    private Coroutine drawRoutine;

    private List<Vector3Int> neededChunks = new List<Vector3Int>();
    private List<Chunk> spawnedChunks = new List<Chunk>();
    private List<Chunk> decoratedChunks = new List<Chunk>();

    void Start()
	{
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        activeChunks.Clear();

        noiseOffsetX_random = Random.Range(-10000f, 10000f);
        noiseOffsetY_random = Random.Range(-10000f, 10000f);
        noiseOffsetZ_random = Random.Range(-10000f, 10000f);

        genStartingGrid();
    }

    void genStartingGrid()
    {
        for (int cx = -gridSize / 2; cx < gridSize / 2; cx++)
            for (int cz = -gridSize / 2; cz < gridSize / 2; cz++)
                for (int cy = -chunksDown; cy <= chunksUp; cy++)
                    SpawnChunk(new Vector3Int(cx, cy, cz));

        foreach (var chunkObj in activeChunks.Values)
            if (!chunkObj.GetComponent<Chunk>().isEmpty)
                chunkObj.GetComponent<Chunk>().DecorateChunk();

        foreach (var chunkObj in activeChunks.Values)
            if (!chunkObj.GetComponent<Chunk>().isEmpty)
                chunkObj.GetComponent<Chunk>().DrawChunk();
    }

	void Update()
	{
		Vector2Int current = GetPlayerChunk();
		if (current != lastPlayerChunk)
		{
			lastPlayerChunk = current;
			//if (buildRoutine != null)
			//	StopCoroutine(buildRoutine);
			//if (removeFarChunks) RemoveDistantChunks(current);
			//buildRoutine = StartCoroutine(BuildChunks(GetNeededChunks(current)));
			neededChunks.AddRange(GetNeededChunks(current));
            if (removeFarChunks) RemoveDistantChunks(current);
        }

		if (neededChunks.Count > 0 && buildRoutine == null)
		{
			buildRoutine = StartCoroutine(BuildChunks());
        }
		if (spawnedChunks.Count > 0 && decorateRoutine == null)
		{
			decorateRoutine = StartCoroutine(DecorateChunks());
        }
		if (decoratedChunks.Count > 0 && drawRoutine == null)
		{
			drawRoutine = StartCoroutine(DrawChunks());
        }

	}

    IEnumerator BuildChunks()
    {
        Vector3Int[] internalNeededChunks;

        lock (neededChunks)
        {
            var chunkNum = Mathf.Min(chunksPerFrame, neededChunks.Count);
            internalNeededChunks = neededChunks.Take(chunkNum).ToArray();

            neededChunks.RemoveRange(0, chunkNum);
        }
        foreach (var coord in internalNeededChunks)
        {
            var newChunk = SpawnChunk(coord);

            if (!newChunk.isEmpty)
            {
                lock (spawnedChunks)
                {
                    spawnedChunks.Add(newChunk);
                }
            }
        }

        buildRoutine = null;
        yield break;
    }

    IEnumerator DecorateChunks()
    {
        Chunk[] internalspawnedChunks;

        lock (spawnedChunks)
        {
            var chunkNum = Mathf.Min(chunksPerFrame, spawnedChunks.Count);
            internalspawnedChunks = spawnedChunks.Take(chunkNum).ToArray();

            spawnedChunks.RemoveRange(0, chunkNum);
        }
        foreach (var chunk in internalspawnedChunks)
        {
            chunk.DecorateChunk();

            lock (decoratedChunks)
            {
                decoratedChunks.Add(chunk);
            }
        }

        decorateRoutine = null;
        yield break;
    }

    IEnumerator DrawChunks()
    {
        Chunk[] internaldecoratedChunks;

        lock (decoratedChunks)
        {
            var chunkNum = Mathf.Min(chunksPerFrame, decoratedChunks.Count);
            internaldecoratedChunks = decoratedChunks.Take(chunkNum).ToArray();

            decoratedChunks.RemoveRange(0, chunkNum);
        }
        foreach (var chunk in internaldecoratedChunks)
        {
            chunk.DrawChunk();
        }

        drawRoutine = null;
        yield break;
    }

    HashSet<Vector3Int> GetNeededChunks(Vector2Int center)
    {
        HashSet<Vector3Int> needed = new();
        for (int dx = -renderDistance; dx <= renderDistance; dx++)
            for (int dz = -renderDistance; dz <= renderDistance; dz++)
                for (int dy = -chunksDown; dy <= chunksUp; dy++)
                {
                    Vector3Int coord = new Vector3Int(center.x + dx, dy, center.y + dz);
                    if (activeChunks.ContainsKey(coord)) continue;
                    needed.Add(coord);
                }
        return needed;
    }

    // Updated to Vector3Int
    public Chunk GetChunk(Vector3Int coord)
	{
		if (activeChunks.TryGetValue(coord, out GameObject go))
			return go.GetComponent<Chunk>();
		return null;
	}

	Vector2Int GetPlayerChunk()
	{
		Vector3 pos = player.position;
		return new Vector2Int(
			Mathf.FloorToInt(pos.x / chunkSize),
			Mathf.FloorToInt(pos.z / chunkSize));
	}

    void RemoveDistantChunks(Vector2Int current)
    {
        List<Vector3Int> toRemove = new();

        foreach (var chunk in activeChunks)
        {
            // Check if the chunk is outside the render distance bounds
            bool isOutsideX = Mathf.Abs(chunk.Key.x - current.x) > renderDistance;
            bool isOutsideZ = Mathf.Abs(chunk.Key.z - current.y) > renderDistance; // Note: current.y is actually the Z coordinate from GetPlayerChunk

            if (isOutsideX || isOutsideZ)
            {
                toRemove.Add(chunk.Key);
            }
        }

        foreach (var coord in toRemove)
        {
            Destroy(activeChunks[coord]);
            activeChunks.Remove(coord);
        }
    }

    Chunk SpawnChunk(Vector3Int coord)
    {
        Vector3 worldPos = new Vector3(coord.x * chunkSize, coord.y * chunkSize, coord.z * chunkSize);
        GameObject chunkObj = Instantiate(chunkPrefab, worldPos, Quaternion.identity);
        Chunk chunk = chunkObj.GetComponent<Chunk>();

        chunk.chunkMaterial = chunkMaterial;
        chunk.noiseOffsetX = noiseOffsetX_random;
        chunk.noiseOffsetY = noiseOffsetY_random;
        chunk.noiseOffsetZ = noiseOffsetZ_random;
        chunk.worldManager = this;

        // ONLY generate data, do not decorate or draw yet!
        chunk.GenerateVoxelData();

        activeChunks[coord] = chunkObj;

        // Early exit so we don't add neighboors
        if (chunk.isEmpty)
        {
            return chunk;
        }

        Vector3Int[] directions = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down, new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        foreach (var dir in directions)
        {
            if (activeChunks.TryGetValue(coord + dir, out GameObject neighborObj))
            {
                var neighboorChunk = neighborObj.GetComponent<Chunk>();
                neighboorChunk.drawn = false;
                decoratedChunks.Add(neighboorChunk);
            }
        }
        return chunk;
    }
}