using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
    public static readonly int CHUNK_WIDTH = 16;
    public static readonly int CHUNK_HEIGHT = 9;

    private static readonly string[] tags = { "Enemy", "Hazard" }; // Make sure to add Pickup, etc. later!

    private static WorldGenerator singleton;

    [Header("World Seed")]
    [SerializeField] private string seedString = "";

    [Header("References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Tilemap playground, background, foreground;
    [SerializeField] private ChunkDatabase database;

    private System.Random rng;
    private HashSet<Vector3Int> occupied = new();

    void Awake()
    {
        if (singleton == null) singleton = this;
        else Destroy(gameObject);

        rng = HashSeed(); // Do NOT use member 'rng' before this line. Required for PCG determinism!

        ClearWorld();

        ChunkData current = database.chunks[rng.Next(0, database.chunks.Count - 1)];
        Vector3Int origin = new Vector3Int(0, 0, 0);
        BuildChunk(current, origin);

        for (int i = 0; i < 10; i++)
        {
            (current, origin) = FindNextChunk(current, origin);
            BuildChunk(current, origin);
        }
        
        SpawnPlayer();
    }

    public static System.Random GetRNG() { return singleton.rng; }

    public void Bake()
    {
        ClearConsole();
        Debug.Log("<color=green>REBUILDING CHUNK DATABASE...</color>");

        database.chunks.Clear();

        BoundsInt worldBounds = GetWorldBounds();

        if (worldBounds.xMin != 0 || worldBounds.yMin != 0)
        {
            Debug.LogError("<color=red>ERROR: Tilemap must start at (0,0)!</color>");
            return;
        }

        if (worldBounds.size.x % CHUNK_WIDTH != 0 || worldBounds.size.y % CHUNK_HEIGHT != 0)
        {
            Debug.LogError($"<color=red>ERROR: Tilemap size must be divisible by chunk size ({CHUNK_WIDTH}x{CHUNK_HEIGHT})!</color>");
            return;
        }

        SerializeChunks(worldBounds);

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(database);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        #endif

        string plural = database.chunks.Count != 1 ? "s" : "";
        Debug.Log($"<color=green>REBUILD COMPLETED: Found {database.chunks.Count} chunk{plural}!</color>");
    }

    private void ClearConsole()
    {
        #if UNITY_EDITOR
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear");
        clearMethod.Invoke(null, null);
        #endif
    }

    private BoundsInt GetWorldBounds()
    {
        playground.RefreshAllTiles();
        playground.CompressBounds();
        background.RefreshAllTiles();
        background.CompressBounds();
        foreground.RefreshAllTiles();
        foreground.CompressBounds();

        BoundsInt worldBounds = playground.cellBounds;

        worldBounds.xMin = Mathf.Min(worldBounds.xMin, background.cellBounds.xMin, foreground.cellBounds.xMin);
        worldBounds.yMin = Mathf.Min(worldBounds.yMin, background.cellBounds.yMin, foreground.cellBounds.yMin);

        worldBounds.xMax = Mathf.Max(worldBounds.xMax, background.cellBounds.xMax, foreground.cellBounds.xMax);
        worldBounds.yMax = Mathf.Max(worldBounds.yMax, background.cellBounds.yMax, foreground.cellBounds.yMax);

        return worldBounds;
    }

    private void SerializeChunks(BoundsInt worldBounds)
    {      
        int chunksX = worldBounds.size.x / CHUNK_WIDTH;
        int chunksY = worldBounds.size.y / CHUNK_HEIGHT;

        for (int i = 0; i < chunksY; i++)
        {
            for (int j = 0; j < chunksX; j++)
            {
                ChunkData chunk = new();

                Vector3Int origin = new Vector3Int(j * CHUNK_WIDTH, i * CHUNK_HEIGHT, 0);
                Bounds chunkBounds = new Bounds(new Vector3(origin.x + CHUNK_WIDTH / 2, origin.y + CHUNK_HEIGHT / 2, 0), new Vector3(CHUNK_WIDTH, CHUNK_HEIGHT, 1f));

                List<GameObject> objects = new();

                foreach (string tag in tags)
                {
                    objects.AddRange(GameObject.FindGameObjectsWithTag(tag));
                }

                foreach (GameObject obj in objects)
                {
                    if (chunkBounds.Contains(obj.transform.position))
                    {
                        GameObject prefabAsset = null;

                        #if UNITY_EDITOR
                        prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                        #endif

                        if (prefabAsset == null)
                        {
                            Debug.LogError($"ERROR: {obj.name} MISSING PREFAB!");
                            continue;
                        }

                        Vector3 localPos = obj.transform.position - origin;

                        chunk.gameObjects.Add(new ObjectData { prefab = prefabAsset, localPosition = localPos });
                    }
                }

                Vector3Int pos = new();

                for (int y = 0; y < CHUNK_HEIGHT; y++)
                {
                    pos.y = origin.y + y;

                    for (int x = 0; x < CHUNK_WIDTH; x++)
                    {
                        pos.x = origin.x + x;

                        chunk.playground.Add(new TileData { tile = playground.GetTile(pos) });
                        chunk.background.Add(new TileData { tile = background.GetTile(pos) });
                        chunk.foreground.Add(new TileData { tile = foreground.GetTile(pos) });

                        if (playground.GetTile(pos) == null)
                        {
                            if (y == 0 && x > 0 && x < CHUNK_WIDTH - 1)                 chunk.bottomConnections.Add(x);
                            if (y == CHUNK_HEIGHT - 1 && x > 0 && x < CHUNK_WIDTH - 1)  chunk.topConnections.Add(x);
                            if (x == 0 && y > 0 && y < CHUNK_HEIGHT - 1)                chunk.leftConnections.Add(y);
                            if (x == CHUNK_WIDTH - 1 && y > 0 && y < CHUNK_HEIGHT - 1)  chunk.rightConnections.Add(y);
                        }
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

    private void ClearWorld()
    {
        playground.ClearAllTiles();
        background.ClearAllTiles();
        foreground.ClearAllTiles();

        List<GameObject> objects = new();

        foreach (string tag in tags)
        {
            objects.AddRange(GameObject.FindGameObjectsWithTag(tag));
        }

        foreach (GameObject obj in objects)
        {
            obj.SetActive(false);
            Destroy(obj);
        }
    }

    private (ChunkData chunk, Vector3Int position) FindNextChunk(ChunkData current, Vector3Int origin)
    {
        List<(ChunkData candidate, Vector3Int position)> candidates = new();
        ChunkData candidate;

        for (int i = 0; i < 100; i++)
        {
            candidate = database.chunks[rng.Next(0, database.chunks.Count - 1)];

            if (current.rightConnections.Intersect(candidate.leftConnections).Any())
            {
                Vector3Int pos = new Vector3Int(CHUNK_WIDTH, 0) + origin;

                if (!occupied.Contains(pos))
                {
                    candidates.Add((candidate, pos));
                }
            }

            if (current.leftConnections.Intersect(candidate.rightConnections).Any())
            {
                Vector3Int pos = new Vector3Int(-CHUNK_WIDTH, 0) + origin;

                if (!occupied.Contains(pos))
                {
                    candidates.Add((candidate, pos));
                }
            }

            if (current.bottomConnections.Intersect(candidate.topConnections).Any())
            {
                Vector3Int pos = new Vector3Int(0, -CHUNK_HEIGHT) + origin;

                if (!occupied.Contains(pos))
                {
                    candidates.Add((candidate, pos));
                }
            }

            if (current.topConnections.Intersect(candidate.bottomConnections).Any())
            {
                Vector3Int pos = new Vector3Int(0, CHUNK_HEIGHT) + origin;

                if (!occupied.Contains(pos))
                {
                    candidates.Add((candidate, pos));
                }
            }
        }

        return candidates[rng.Next(0, candidates.Count - 1)];
    }

    private void BuildChunk(ChunkData chunk, Vector3Int origin)
    {
        occupied.Add(origin);

        int i = 0;

        for (int y = 0; y < CHUNK_HEIGHT; y++)
        {
            for (int x = 0; x < CHUNK_WIDTH; x++)
            {
                Vector3Int pos = new Vector3Int(x, y);

                playground.SetTile(origin + pos, chunk.playground[i].tile);
                background.SetTile(origin + pos, chunk.background[i].tile);
                foreground.SetTile(origin + pos, chunk.foreground[i].tile);

                i++;
            }
        }

        foreach (ObjectData objectData in chunk.gameObjects)
        {
            Instantiate(objectData.prefab, objectData.localPosition + origin, Quaternion.identity);
        }
    }

    private void SpawnPlayer()
    {
        Vector3Int position = new Vector3Int(0, 0, 0);
        Vector3Int below = new Vector3Int(0, -1, 0);

        for (int y = 0; y < CHUNK_HEIGHT; y++)
        {
            position.y = y;

            for (int x = 0; x < CHUNK_WIDTH; x++)
            {
                position.x = x;

                if (playground.GetTile(position) == null && playground.GetTile(position + below) != null)
                {
                    Instantiate(playerPrefab, new Vector3(x + 0.5f, y, 0), Quaternion.identity);
                    return;
                }
            }
        }
    }
}
