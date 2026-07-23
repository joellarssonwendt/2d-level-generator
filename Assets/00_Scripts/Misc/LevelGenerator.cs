using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class ChunkData
{
    public List<TileBase> tilemap = new();
    public List<TileBase> background = new();
    public List<TileBase> foreground = new();
    public Dictionary<Vector3, GameObject> gameObjects = new();
}

public class LevelGenerator : MonoBehaviour
{
    private static LevelGenerator singleton;

    [Header("World Seed")]
    [SerializeField] private string seedString = "";

    [Header("References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Tilemap tilemap, background, foreground;
    [SerializeField] private ChunkDatabase database;

    private System.Random rng;

    void Awake()
    {
        if (singleton == null) singleton = this;
        else Destroy(gameObject);

        rng = HashSeed(); // Do NOT use member 'rng' before this line. Required for PCG determinism!
        SpawnPlayer();
    }

    public void Bake()
    {
        Debug.Log("LevelGenerator.cs | Baking...");

        database.chunks.Clear();
        SerializeChunks();

        Debug.Log($"LevelGenerator.cs | Baking completed, found {database.chunks.Count} chunks!");
    }

    private void SerializeChunks()
    {
        int width = 30;
        int height = 20;

        Debug.Assert(
            tilemap.cellBounds.xMin == 0 && tilemap.cellBounds.yMin == 0,
            "ERROR: Tilemap must start at (0,0)!"
        );

        Debug.Assert(
            tilemap.cellBounds.size.x % width == 0 &&
            tilemap.cellBounds.size.y % height == 0,
            "ERROR: World size must be divisible by chunk size!"
        );

        int chunksX = tilemap.cellBounds.size.x / width;
        int chunksY = tilemap.cellBounds.size.y / height;

        for (int i = 0; i < chunksY; i++)
        {
            for (int j = 0; j < chunksX; j++)
            {
                ChunkData chunk = new();

                Vector3 origin = new Vector3(j * width, i * height, 0);
                Bounds bounds = new Bounds(new Vector3(origin.x + width / 2, origin.y + height / 2, 0), new Vector3(width, height, 1f));

                string[] tags = { "Enemy", "Hazard" }; // Make sure to add Pickup, etc. later!

                List<GameObject> objects = new();

                foreach (string tag in tags)
                {
                    objects.AddRange(GameObject.FindGameObjectsWithTag(tag));
                }

                foreach (GameObject obj in objects)
                {
                    if (bounds.Contains(obj.transform.position))
                    {
                        GameObject prefab = null;

                        #if UNITY_EDITOR
                        prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                        #endif

                        if (prefab == null)
                        {
                            Debug.LogError($"ERROR: {obj.name} MISSING PREFAB!");
                            continue;
                        }

                        Vector3 localPos = obj.transform.position - origin;

                        chunk.gameObjects.Add(localPos, prefab);
                    }
                }

                Vector3Int pos = new();

                for (int y = 0; y < height; y++)
                {
                    pos.y = (int)origin.y + y;

                    for (int x = 0; x < width; x++)
                    {
                        pos.x = (int)origin.x + x;

                        chunk.tilemap.Add(tilemap.GetTile(pos));
                        chunk.background.Add(background.GetTile(pos));
                        chunk.foreground.Add(foreground.GetTile(pos));
                    }
                }

                database.chunks.Add(chunk);
            }
        }
    }

    private System.Random HashSeed()
    {
        int seed;

        if (string.IsNullOrEmpty(seedString))
        {
            seed = Random.Range(1, int.MaxValue);

            Debug.Log($"World seed: {seed}");
        }
        else if (int.TryParse(seedString, out seed))
        {
            Debug.Log($"World seed: {seed}");
        }
        else
        {
            seed = 23;

            unchecked // Allow overflow
            {
                foreach (char c in seedString)
                {
                    seed = seed * 31 + c;
                }
            }

            if (seed < 0)
            {
                seed = seed & 0x7FFFFFFF; // Remove sign bit
            }

            Debug.Log($"World seed: {seedString}");
        }

        //Debug.Log($"Internal seed: {seed}");
        return new System.Random(seed);
    }

    private void SpawnPlayer()
    {
        // if (space above and ground below) Instantiate(playerPrefab, new Vector3(x + 0.5f, y, 0), Quaternion.identity);
    }
}
