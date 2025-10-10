#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public static class TilemapChunkExtractorTool
{
    [MenuItem("Tools/Extract Chunks From Selected Tilemap")]
    public static void ExtractChunksFromSelectedTilemap()
    {
        // Use currently selected GameObject
        GameObject selectedGO = Selection.activeGameObject;
        if (selectedGO == null)
        {
            Debug.LogError("Select a Tilemap GameObject in the hierarchy!");
            return;
        }

        Tilemap tilemap = selectedGO.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("Selected GameObject does not have a Tilemap component!");
            return;
        }

        int chunkWidth = 3;
        int chunkHeight = 31;

        BoundsInt bounds = tilemap.cellBounds;

        int horizontalChunks = bounds.size.x / chunkWidth;
        int verticalChunks = bounds.size.y / chunkHeight;

        string folder = "Assets/Resources/Chunks";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Chunks");

        int count = 0;

        for (int cx = 0; cx < horizontalChunks; cx++)
        {
            for (int cy = 0; cy < verticalChunks; cy++)
            {
                TileChunkData asset = ScriptableObject.CreateInstance<TileChunkData>();
                asset.width = chunkWidth;
                asset.height = chunkHeight;
                asset.tiles = new TileBase[chunkWidth * chunkHeight];

                for (int x = 0; x < chunkWidth; x++)
                {
                    for (int y = 0; y < chunkHeight; y++)
                    {
                        Vector3Int pos = new Vector3Int(
                            bounds.xMin + cx * chunkWidth + x,
                            bounds.yMin + cy * chunkHeight + y,
                            0
                        );

                        asset.tiles[y * chunkWidth + x] = tilemap.GetTile(pos);
                    }
                }

                string path = $"{folder}/Chunk_{System.Guid.NewGuid()}.asset";
                AssetDatabase.CreateAsset(asset, path);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Chunk extraction complete! Created {count} chunks in {folder}.");
    }
}
#endif
