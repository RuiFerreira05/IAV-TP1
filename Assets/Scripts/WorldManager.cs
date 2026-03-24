using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class WorldManager : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public GameObject chunkPrefab;
    public Material chunkMaterial;

    [Header("Configuracao")]
    public int renderDistance = 3;
    public int chunkSize = 16;
    public int chunksPerFrame = 2;
    private Dictionary<Vector2Int, GameObject> activeChunks = new();
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
            {
                SpawnChunk(new Vector2Int(cx, cz));
            }

        DrawActiveChunks();
    }

    void Update()
    {
        Vector2Int current = GetPlayerChunk();
        if (current != lastPlayerChunk)
        {
            lastPlayerChunk = current;
            // Cancelar a coroutine anterior (se ainda estiver a correr)
            if (buildRoutine != null)
                StopCoroutine(buildRoutine);
            // Remover chunks fora do range (isto continua s�ncrono)
            RemoveDistantChunks(current);
            // Lan�ar nova coroutine para gerar os novos
            buildRoutine = StartCoroutine(BuildChunks(GetNeededChunks(current)));
        }
    }

    public Chunk GetChunk(Vector2Int coord)
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
        List<Vector2Int> toRemove = new();
        HashSet<Vector2Int> needed = GetNeededChunks(current);
        foreach (var chunk in activeChunks)
        {
            if (!needed.Contains(chunk.Key))
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

    HashSet<Vector2Int> GetNeededChunks(Vector2Int center)
    {
        HashSet<Vector2Int> needed = new();
        for (int dx = -renderDistance; dx <= renderDistance; dx++)
            for (int dz = -renderDistance; dz <= renderDistance; dz++)
            {
                Vector2Int coord = center + new Vector2Int(dx, dz);
                needed.Add(coord);
            }
        return needed;
    }

    void UpdateChunks()
    {
        // TODO: 1. Construir HashSet<Vector2Int> com os chunks necessarios
        // (todos os (cx,cz) dentro de renderDistance do centro)
        // TODO: 2. Remover chunks que j� n�o s�o necessarios
        // Aten��o: n�o modificar o Dictionary enquanto se itera!
        // Sugest�o: recolher as chaves a remover numa lista separada
        // TODO: 3. Spawnar os chunks de 'needed' que ainda n�o existem

        HashSet<Vector2Int> needed = new();
        for (int dx = -renderDistance; dx <= renderDistance; dx++)
            for (int dz = -renderDistance; dz <= renderDistance; dz++)
            {
                Vector2Int coord = lastPlayerChunk + new Vector2Int(dx, dz);
                needed.Add(coord);
            }

        List<Vector2Int> toRemove = new();
        foreach (var chunk in activeChunks)
        {
            if (!needed.Contains(chunk.Key))
            {
                toRemove.Add(chunk.Key);
            }
        }

        foreach (var coord in toRemove)
        {
            Destroy(activeChunks[coord]);
            activeChunks.Remove(coord);
        }

        foreach (var coord in needed)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                SpawnChunk(coord);
            }
        }

        DrawActiveChunks();
    }

    void SpawnChunk(Vector2Int coord)
    {
        // TODO: Calcular posi��o world: coord * chunkSize
        // TODO: Instantiate do prefab, obter Chunk, chamar Initialize
        // TODO: Registar no Dictionary activeChunks

        Vector2 worldPos = coord * chunkSize;
        GameObject chunkObj = Instantiate(chunkPrefab, new Vector3(worldPos.x, 0, worldPos.y), Quaternion.identity);
        Chunk chunk = chunkObj.GetComponent<Chunk>();
        chunk.chunkMaterial = chunkMaterial;
        chunk.noiseOffsetX = noiseOffsetX_random;
        chunk.noiseOffsetY = noiseOffsetY_random;
        chunk.noiseOffsetZ = noiseOffsetZ_random;
        //chunk.Generate(this);
        activeChunks[coord] = chunkObj;
    }

    void DrawActiveChunks()
    {
        foreach (var chunk in activeChunks.Values)
        {
            if (!chunk.GetComponent<Chunk>().drawn)
                chunk.GetComponent<Chunk>().DrawChunk();
        }
    }

    IEnumerator BuildChunks(HashSet<Vector2Int> needed)
    {
        int count = 0;
        foreach (var coord in needed)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                SpawnChunk(coord);
                count++;
                if (count % chunksPerFrame == 0)
                    yield return null; // pausa at� ao pr�ximo frame
            }
        }

        foreach (var chunk in activeChunks.Values)
        {
            if (!chunk.GetComponent<Chunk>().drawn)
                chunk.GetComponent<Chunk>().DrawChunk();
        }
    }
}