using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBuilder : MonoBehaviour
{
    [Header("Tilemap to draw on")]
    public Tilemap targetTilemap;

    [Header("Level Settings")]
    public int numberOfChunks = 10;

    [Header("Chunk Settings")]
    public int chunkWidth = 3;
    public int chunkHeight = 31;

    [Header("Optional: manually assign chunks")]
    public List<TileChunkData> availableChunks;

    void Start()
    {
        // Auto-load chunks from Resources if none are assigned
        if (availableChunks == null || availableChunks.Count == 0)
        {
            availableChunks = new List<TileChunkData>(Resources.LoadAll<TileChunkData>("Chunks"));
        }

        if (availableChunks.Count == 0)
        {
            Debug.LogError("No TileChunkData found in Resources/Chunks! Cannot generate level.");
            return;
        }

        GenerateLevel();
    }

    void GenerateLevel()
    {
        targetTilemap.ClearAllTiles();

        Vector3Int currentPos = new Vector3Int(0, 0, 0);

        for (int i = 0; i < numberOfChunks; i++)
        {
            var chunk = availableChunks[Random.Range(0, availableChunks.Count)];

            PlaceChunk(chunk, new Vector3Int(currentPos.x, 0, 0));

            currentPos.x += chunk.width;
        }
    }

    void PlaceChunk(TileChunkData chunk, Vector3Int startPos)
    {
        for (int x = 0; x < chunk.width; x++)
        {
            for (int y = 0; y < chunk.height; y++)
            {
                int index = y * chunk.width + x;
                if (index >= chunk.tiles.Length) continue; // safety check

                TileBase tile = chunk.tiles[index];
                if (tile != null)
                    targetTilemap.SetTile(new Vector3Int(startPos.x + x, startPos.y + y, 0), tile);
            }
        }
    }
}
