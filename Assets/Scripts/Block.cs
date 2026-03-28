using System.Collections.Generic;
using UnityEngine;

public class Block
{
    public enum CubeFace { Front, Back, Top, Bottom, Left, Right }
    public Vector3 position;
    public bool isSolid;

    // Os 8 vertices (mesmos da aula01)
    static readonly Vector3 v0 = new Vector3(-0.5f, -0.5f, 0.5f);
    static readonly Vector3 v1 = new Vector3(0.5f, -0.5f, 0.5f);
    static readonly Vector3 v2 = new Vector3(0.5f, -0.5f, -0.5f);
    static readonly Vector3 v3 = new Vector3(-0.5f, -0.5f, -0.5f);
    static readonly Vector3 v4 = new Vector3(-0.5f, 0.5f, 0.5f);
    static readonly Vector3 v5 = new Vector3(0.5f, 0.5f, 0.5f);
    static readonly Vector3 v6 = new Vector3(0.5f, 0.5f, -0.5f);
    static readonly Vector3 v7 = new Vector3(-0.5f, 0.5f, -0.5f);

    public enum BlockType { GRASS, DIRT, STONE, AIR, BEDROCK, CAVE_AIR, NONE, SNOW, WATER}
    public BlockType type;
    public Block(BlockType type, Vector3 position)
    {
        this.type = type;
        this.position = position;
        isSolid = (type != BlockType.AIR && type != BlockType.CAVE_AIR);
    }

    public static Vector2[] GetUVs(CubeFace face, BlockType type)
    {
        // Canto inferior-esquerdo de cada textura no atlas (coluna, linha) / 16
        Vector2 lbc;

        switch (type)
        {
            case BlockType.GRASS:
                if (face == CubeFace.Top) lbc = new Vector2(2f, 6f) / 16;
                else if (face == CubeFace.Bottom) lbc = new Vector2(2f, 15f) / 16;
                else lbc = new Vector2(3f, 15f) / 16;
                break;
            case BlockType.DIRT:
                lbc = new Vector2(2f, 15f) / 16;
                break;
            case BlockType.STONE:
                lbc = new Vector2(1f, 15f) / 16;
                break;
            case BlockType.BEDROCK:
                lbc = new Vector2(1f, 14f) / 16;
                break;
            case BlockType.SNOW:
                if (face == CubeFace.Top) lbc = new Vector2(2f, 11f) / 16;
                else if (face == CubeFace.Bottom) lbc = new Vector2(2f, 15f) / 16;
                else lbc = new Vector2(4f, 11f) / 16;
                break;
            case BlockType.WATER:
                lbc = new Vector2(15f, 2f);
                break;
            default:
                lbc = new Vector2(0f, 1f);
                break;
        }

        Vector2 uv00 = lbc; // inferior-esquerdo
        Vector2 uv10 = lbc + new Vector2(1f, 0f) / 16; // inferior-direito
        Vector2 uv01 = lbc + new Vector2(0f, 1f) / 16; // superior-esquerdo
        Vector2 uv11 = lbc + new Vector2(1f, 1f) / 16; // superior-direito
        
        return new[] { uv11, uv01, uv00, uv10 };
    }

    // TODO: implementar
    public void AddFaceToMeshData(CubeFace face, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        int vertexIndex = vertices.Count;

        // 2. Obter os 4 vértices da face (ver tabela da aula01)
        Vector3[] faceVertices = getFaceVertices(face);

        // 3. Somar this.position a cada vértice
        for (int i = 0; i < faceVertices.Length; i++)
        {
            faceVertices[i] += this.position;
        }

        // 4. Adicionar vértices e UVs às listas
        vertices.AddRange(faceVertices);
        
        uvs.AddRange(GetUVs(face, this.type));

        // 5. Adicionar triângulos COM OFFSET (vertexIndex + ...)
        triangles.AddRange(new int[]
        {
            vertexIndex + 3, vertexIndex + 1, vertexIndex + 0,
            vertexIndex + 3, vertexIndex + 2, vertexIndex + 1
        });
    }

    Vector3[] getFaceVertices(CubeFace face)
    {
        switch (face)
        {
            case CubeFace.Front:
                return new Vector3[] { v4, v5, v1, v0 };
            case CubeFace.Back:
                return new Vector3[] { v6, v7, v3, v2 };
            case CubeFace.Top:
                return new Vector3[] { v7, v6, v5, v4 };
            case CubeFace.Bottom:
                return new Vector3[] { v0, v1, v2, v3 };
            case CubeFace.Left:
                return new Vector3[] { v7, v4, v0, v3 };
            case CubeFace.Right:
                return new Vector3[] { v5, v6, v2, v1 };
            default:
                return null;
        }
    }
}