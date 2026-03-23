// WorldManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	// Changed from Vector2Int to Vector3Int
	private Dictionary<Vector3Int, GameObject> activeChunks = new();
	private Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);

	private Coroutine buildRoutine;

	private int noiseOffsetX_random;
	private int noiseOffsetY_random;
	private int noiseOffsetZ_random;

	public int gridSize = 5;

	void Start()
	{
		noiseOffsetX_random = Random.Range(-10000, 10000);
		noiseOffsetY_random = Random.Range(-10000, 10000);
		noiseOffsetZ_random = Random.Range(-10000, 10000);

		for (int cx = -gridSize / 2; cx < gridSize / 2; cx++)
			for (int cz = -gridSize / 2; cz < gridSize / 2; cz++)
				for (int cy = chunksDown; cy <= chunksUp; cy++)
					SpawnChunk(new Vector3Int(cx, cy, cz));

		DrawActiveChunks();
	}

	void Update()
	{
		Vector2Int current = GetPlayerChunk();
		if (current != lastPlayerChunk)
		{
			lastPlayerChunk = current;
			if (buildRoutine != null)
				StopCoroutine(buildRoutine);
			RemoveDistantChunks(current);
			buildRoutine = StartCoroutine(BuildChunks(GetNeededChunks(current)));
		}
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
		HashSet<Vector3Int> needed = GetNeededChunks(current);
		foreach (var chunk in activeChunks)
		{
			if (!needed.Contains(chunk.Key))
				toRemove.Add(chunk.Key);
		}
		foreach (var coord in toRemove)
		{
			Destroy(activeChunks[coord]);
			activeChunks.Remove(coord);
		}
	}

	// Now returns Vector3Int coords including all Y levels
	HashSet<Vector3Int> GetNeededChunks(Vector2Int center)
	{
		HashSet<Vector3Int> needed = new();
		for (int dx = -renderDistance; dx <= renderDistance; dx++)
			for (int dz = -renderDistance; dz <= renderDistance; dz++)
				for (int dy = chunksDown; dy <= chunksUp; dy++)
					needed.Add(new Vector3Int(center.x + dx, dy, center.y + dz));
		return needed;
	}

	void SpawnChunk(Vector3Int coord)
	{
		Vector3 worldPos = new Vector3(coord.x * chunkSize, coord.y * chunkSize, coord.z * chunkSize);
		GameObject chunkObj = Instantiate(chunkPrefab, worldPos, Quaternion.identity);
		Chunk chunk = chunkObj.GetComponent<Chunk>();
		chunk.chunkMaterial = chunkMaterial;
		chunk.noiseOffsetX = noiseOffsetX_random;
		chunk.noiseOffsetY = noiseOffsetY_random;
		chunk.noiseOffsetZ = noiseOffsetZ_random;
		chunk.Generate(this);
		activeChunks[coord] = chunkObj;

        Vector3Int[] directions = {
			Vector3Int.right, Vector3Int.left,
			Vector3Int.up, Vector3Int.down,
			new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
		};
        foreach (var dir in directions)
        {
            if (activeChunks.TryGetValue(coord + dir, out GameObject neighborObj))
            {
                Chunk neighbor = neighborObj.GetComponent<Chunk>();
                neighbor.drawn = false; // will be redrawn in DrawActiveChunks
            }
        }
    }

	void DrawActiveChunks()
	{
		foreach (var chunk in activeChunks.Values)
		{
			Chunk c = chunk.GetComponent<Chunk>();
			if (!c.drawn) c.DrawChunk();
		}
	}

	IEnumerator BuildChunks(HashSet<Vector3Int> needed)
	{
		int count = 0;
		foreach (var coord in needed)
		{
			if (!activeChunks.ContainsKey(coord))
			{
				SpawnChunk(coord);
				count++;
				if (count % chunksPerFrame == 0)
					yield return null;
			}
		}

		foreach (var chunk in activeChunks.Values)
		{
			Chunk c = chunk.GetComponent<Chunk>();
			if (!c.drawn) c.DrawChunk();
		}
	}
}