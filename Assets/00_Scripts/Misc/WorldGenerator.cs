using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class TileData
{
    public TileBase tile;
}

[System.Serializable]
public class ObjectData
{
    public GameObject obj;
    public Vector3 pos;
}

[System.Serializable]
public class ChunkData
{
    public List<TileData> tilemap = new();
    public List<TileData> background = new();
    public List<TileData> foreground = new();
    public List<ObjectData> gameObjects = new();
}

public class WorldGenerator : MonoBehaviour
{
    private static readonly int CHUNK_WIDTH = 30;
    private static readonly int CHUNK_HEIGHT = 20;
    private static readonly string[] tags = { "Enemy", "Hazard" }; // Make sure to add Pickup, etc. later!

    private static WorldGenerator singleton;

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
    }

    private void Start()
    {
        rng = HashSeed(); // Do NOT use member 'rng' before this line. Required for PCG determinism!
        ClearWorld();

        Vector3Int origin = new Vector3Int(0, 0, 0);
        foreach (ChunkData chunk in database.chunks)
        {
            BuildChunk(chunk, origin);

            origin.x += CHUNK_WIDTH;
            origin.y += CHUNK_HEIGHT;
        }

        SpawnPlayer();
    }

    public void Bake()
    {
        Debug.Log("Baking...");

        database.chunks.Clear();
        SerializeChunks();

        Debug.Log($"Baking completed, found {database.chunks.Count} chunks!");
    }

    private void SerializeChunks()
    {
        Debug.Assert(
            tilemap.cellBounds.xMin == 0 && tilemap.cellBounds.yMin == 0,
            "ERROR: Tilemap must start at (0,0)!"
        );

        Debug.Assert(
            tilemap.cellBounds.size.x % CHUNK_WIDTH == 0 &&
            tilemap.cellBounds.size.y % CHUNK_HEIGHT == 0,
            "ERROR: World size must be divisible by chunk size!"
        );

        int chunksX = tilemap.cellBounds.size.x / CHUNK_WIDTH;
        int chunksY = tilemap.cellBounds.size.y / CHUNK_HEIGHT;

        for (int i = 0; i < chunksY; i++)
        {
            for (int j = 0; j < chunksX; j++)
            {
                ChunkData chunk = new();

                Vector3Int origin = new Vector3Int(j * CHUNK_WIDTH, i * CHUNK_HEIGHT, 0);
                Bounds bounds = new Bounds(new Vector3(origin.x + CHUNK_WIDTH / 2, origin.y + CHUNK_HEIGHT / 2, 0), new Vector3(CHUNK_WIDTH, CHUNK_HEIGHT, 1f));

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

                        chunk.gameObjects.Add(new ObjectData { obj = prefab, pos = localPos });
                    }
                }

                Vector3Int pos = new();

                for (int y = 0; y < CHUNK_HEIGHT; y++)
                {
                    pos.y = origin.y + y;

                    for (int x = 0; x < CHUNK_WIDTH; x++)
                    {
                        pos.x = origin.x + x;

                        chunk.tilemap.Add(new TileData { tile = tilemap.GetTile(pos) });
                        chunk.background.Add(new TileData { tile = background.GetTile(pos) });
                        chunk.foreground.Add(new TileData { tile = foreground.GetTile(pos) });
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
        tilemap.ClearAllTiles();
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

    private void BuildChunk(ChunkData chunk, Vector3Int origin)
    {
        int i = 0;

        for (int y = 0; y < CHUNK_HEIGHT; y++)
        {
            for (int x = 0; x < CHUNK_WIDTH; x++)
            {
                Vector3Int pos = new Vector3Int(x, y);

                tilemap.SetTile(origin + pos, chunk.tilemap[i].tile);
                background.SetTile(origin + pos, chunk.background[i].tile);
                foreground.SetTile(origin + pos, chunk.foreground[i].tile);

                i++;
            }
        }

        foreach (ObjectData objectData in chunk.gameObjects)
        {
            Instantiate(objectData.obj, objectData.pos, Quaternion.identity);
        }
    }

    private void SpawnPlayer()
    {
        // if (space above and ground below) Instantiate(playerPrefab, new Vector3(x + 0.5f, y, 0), Quaternion.identity);
    }
}
