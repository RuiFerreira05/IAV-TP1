using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Chunk))]
public class ChunkEditor : Editor
{
    public bool autoUpdate = true;

    public override void OnInspectorGUI()
    {
        Chunk chunk = (Chunk)target;

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
            if (autoUpdate)
            {
                // Check if this chunk is part of an editor-generated region
                WorldManager3D wm = chunk.GetComponentInParent<WorldManager3D>();
                if (wm != null && wm.activeChunks != null && wm.activeChunks.Count > 1)
                {
                    SyncAndRebuildRegion(chunk, wm);
                }
                else
                {
                    GenerateInEditor(chunk);
                }
            }
        }

        GUILayout.Space(10);
        autoUpdate = GUILayout.Toggle(autoUpdate, "Auto-Update in Editor");

        GUILayout.Space(5);
        if (GUILayout.Button("Generate Chunk"))
        {
            WorldManager3D wm = chunk.GetComponentInParent<WorldManager3D>();
            if (wm != null && wm.activeChunks != null && wm.activeChunks.Count > 1)
                SyncAndRebuildRegion(chunk, wm);
            else
                GenerateInEditor(chunk);
        }
    }

    private void GenerateInEditor(Chunk chunk)
    {
        if (chunk.chunkSize <= 0) return;

        // Use the new 3-pass generation system
        chunk.GenerateVoxelData();
        chunk.DecorateChunk();
        chunk.DrawChunk();
    }

    private void SyncAndRebuildRegion(Chunk sourceChunk, WorldManager3D wm)
    {
        // 1. Copy the new settings from the edited chunk to all other chunks in the region via Reflection
        var fields = typeof(Chunk).GetFields();
        foreach (var chunkObj in wm.activeChunks.Values)
        {
            if (chunkObj == null) continue;
            Chunk c = chunkObj.GetComponent<Chunk>();
            if (c == sourceChunk) continue;

            foreach (var field in fields)
            {
                field.SetValue(c, field.GetValue(sourceChunk));
            }
        }

        // 2. PASS 1: Generate Data for ALL chunks
        foreach (var chunkObj in wm.activeChunks.Values)
        {
            if (chunkObj != null) chunkObj.GetComponent<Chunk>().GenerateVoxelData();
        }

        // 3. PASS 2: Decorate ALL chunks (calculates grass now that all neighbors exist)
        foreach (var chunkObj in wm.activeChunks.Values)
        {
            if (chunkObj != null) chunkObj.GetComponent<Chunk>().DecorateChunk();
        }

        // 4. PASS 3: Redraw all meshes
        foreach (var chunkObj in wm.activeChunks.Values)
        {
            if (chunkObj != null) chunkObj.GetComponent<Chunk>().DrawChunk();
        }
    }
}