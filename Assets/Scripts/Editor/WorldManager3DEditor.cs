using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(WorldManager3D))]
public class WorldManager3DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WorldManager3D manager = (WorldManager3D)target;

        GUILayout.Space(15);
        if (GUILayout.Button("Generate Region in Editor", GUILayout.Height(30)))
        {
            GenerateRegion(manager);
        }

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear Region", GUILayout.Height(25)))
        {
            ClearRegion(manager);
        }
        GUI.backgroundColor = Color.white;
    }

    private void ClearRegion(WorldManager3D manager)
    {
        if (manager.activeChunks == null) manager.activeChunks = new Dictionary<Vector3Int, GameObject>();
        manager.activeChunks.Clear();

        // Safely destroy all child objects in the Editor
        while (manager.transform.childCount > 0)
        {
            DestroyImmediate(manager.transform.GetChild(0).gameObject);
        }
    }

    private void GenerateRegion(WorldManager3D manager)
    {
        if (manager.chunkPrefab == null)
        {
            Debug.LogWarning("Please assign a Chunk Prefab to the WorldManager3D before generating.");
            return;
        }

        ClearRegion(manager);

        // 1. Spawn all chunks as children of WorldManager3D
        for (int cx = -manager.gridSize / 2; cx <= manager.gridSize / 2; cx++)
        {
            for (int cz = -manager.gridSize / 2; cz <= manager.gridSize / 2; cz++)
            {
                for (int cy = manager.chunksDown; cy <= manager.chunksUp; cy++)
                {
                    Vector3Int coord = new Vector3Int(cx, cy, cz);
                    Vector3 worldPos = new Vector3(coord.x * manager.chunkSize, coord.y * manager.chunkSize, coord.z * manager.chunkSize);

                    // Instantiate keeping the prefab link intact
                    GameObject chunkObj = (GameObject)PrefabUtility.InstantiatePrefab(manager.chunkPrefab, manager.transform);
                    chunkObj.transform.position = worldPos;

                    Chunk chunk = chunkObj.GetComponent<Chunk>();
                    chunk.chunkMaterial = manager.chunkMaterial;
                    chunk.noiseOffsetX = 5523636; // Fix offset to 0 for consistent previews
                    chunk.noiseOffsetY = 6236632;
                    chunk.noiseOffsetZ = 3664646;
                    chunk.worldManager = manager;

                    manager.activeChunks[coord] = chunkObj;
                }
            }
        }

        // 1. Generate Data for ALL chunks
        foreach (var chunkObj in manager.activeChunks.Values)
            chunkObj.GetComponent<Chunk>().GenerateVoxelData();

        // 2. Decorate ALL chunks
        foreach (var chunkObj in manager.activeChunks.Values)
            if (!chunkObj.GetComponent<Chunk>().isEmpty)
                chunkObj.GetComponent<Chunk>().DecorateChunk();

        foreach (var chunkObj in manager.activeChunks.Values)
            if (!chunkObj.GetComponent<Chunk>().isEmpty)
                chunkObj.GetComponent<Chunk>().DrawChunk();
    }
}