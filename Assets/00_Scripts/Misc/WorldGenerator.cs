using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
    private class Node
    {
        public readonly ChunkData chunkData;
        public readonly Vector3Int position;
        public Node up, down, left, right;

        public Node(ChunkData chunkData, Vector3Int position)
        {
            this.chunkData = chunkData;
            this.position = position;
        }
    }

    private class Graph
    {
        private readonly Dictionary<Vector3Int, Node> nodes = new();

        public void AddNode(Node node)
        {
            if (!nodes.TryAdd(node.position, node))
            {
                return;
            }

            node.up = nodes.TryGetValue(node.position + Vector3Int.up, out Node up) ? up : null;
            if (node.up != null) node.up.down = node;

            node.down = nodes.TryGetValue(node.position + Vector3Int.down, out Node down) ? down : null;
            if (node.down != null) node.down.up = node;

            node.left = nodes.TryGetValue(node.position + Vector3Int.left, out Node left) ? left : null;
            if (node.left != null) node.left.right = node;

            node.right = nodes.TryGetValue(node.position + Vector3Int.right, out Node right) ? right : null;
            if (node.right != null) node.right.left = node;
        }

        public IEnumerable<Node> GetNodes()
        {
            return nodes.Values;
        }

        public bool IsOccupied(Vector3Int position)
        {
            return nodes.TryGetValue(position, out Node node);
        }
    }

    public static readonly int CHUNK_WIDTH = 10;
    public static readonly int CHUNK_HEIGHT = 10;

    private static readonly string[] tags = { "Enemy", "Hazard", "Decoration" }; // Make sure to add Pickup, etc. later!

    private static WorldGenerator singleton;

    [Header("Settings")]
    [SerializeField] private bool showChunkGrid = true;

    [Header("World Seed")]
    [SerializeField] private string seedString = "";

    [Header("References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Tilemap playground, background, foreground;
    [SerializeField] private ChunkDatabase database;

    private System.Random rng;
    private Graph graph = new();

    void Awake()
    {
        if (singleton == null) singleton = this;
        else Destroy(gameObject);

        rng = HashSeed(); // Do NOT use member 'rng' before this line. Required for PCG determinism!

        ClearWorld();
        GenerateGraph();
        
        foreach (Node node in graph.GetNodes())
        {
            BuildChunk(node.chunkData, node.position);
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

                        Vector3 localPosition = obj.transform.position - origin;
                        Quaternion rotation = obj.transform.rotation;
                        Vector3 scale = obj.transform.localScale;

                        chunk.gameObjects.Add(new ObjectData(prefabAsset, localPosition, rotation, scale));
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
                            if (y == 0 && x > 0 && x < CHUNK_WIDTH - 1)                 chunk.downSockets.Add(x);
                            if (y == CHUNK_HEIGHT - 1 && x > 0 && x < CHUNK_WIDTH - 1)  chunk.upSockets.Add(x);
                            if (x == 0 && y > 0 && y < CHUNK_HEIGHT - 1)                chunk.leftSockets.Add(y);
                            if (x == CHUNK_WIDTH - 1 && y > 0 && y < CHUNK_HEIGHT - 1)  chunk.rightSockets.Add(y);
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

    private void GenerateGraph()
    {
        graph.AddNode(new Node(database.chunks[rng.Next(0, database.chunks.Count)], Vector3Int.zero));

        for (int i = 0; i < 5; i++)
        {
            List<Node> newNodes = new();

            foreach (Node node in graph.GetNodes())
            {
                Node next = null;

                if (node.up == null) next = FindNextNode(node, Vector3Int.up);
                if (next != null) newNodes.Add(next);

                if (node.down == null) next = FindNextNode(node, Vector3Int.down);
                if (next != null) newNodes.Add(next);

                if (node.left == null) next = FindNextNode(node, Vector3Int.left);
                if (next != null) newNodes.Add(next);

                if (node.right == null) next = FindNextNode(node, Vector3Int.right);
                if (next != null) newNodes.Add(next);
            }

            foreach (Node node in newNodes)
            {
                graph.AddNode(node);
            }
        }
    }

    private Node FindNextNode(Node node, Vector3Int direction)
    {
        List<Node> candidates = new();
        Vector3Int position = node.position + direction;

        if (graph.IsOccupied(position)) return null;

        for (int i = 0; i < 100; i++)
        {
            ChunkData chunkData = database.chunks[rng.Next(0, database.chunks.Count)];

            if (direction == Vector3Int.up)
            {
                if (node.chunkData.upSockets.Intersect(chunkData.downSockets).Any())
                {
                    candidates.Add(new Node(chunkData, position));
                }
            }

            if (direction == Vector3Int.down)
            {
                if (node.chunkData.downSockets.Intersect(chunkData.upSockets).Any())
                {
                    candidates.Add(new Node(chunkData, position));
                }
            }

            if (direction == Vector3Int.left)
            {
                if (node.chunkData.leftSockets.Intersect(chunkData.rightSockets).Any())
                {
                    candidates.Add(new Node(chunkData, position));
                }
            }

            if (direction == Vector3Int.right)
            {
                if (node.chunkData.rightSockets.Intersect(chunkData.leftSockets).Any())
                {
                    candidates.Add(new Node(chunkData, position));
                }
            }
        }

        if (candidates.Count > 0) return candidates[rng.Next(0, candidates.Count)];
        else return null;
    }

    private void BuildChunk(ChunkData chunk, Vector3Int chunkPosition)
    {
        Vector3Int actualPosition = new Vector3Int(chunkPosition.x * CHUNK_WIDTH, chunkPosition.y * CHUNK_HEIGHT, 0);

        int i = 0;

        for (int y = 0; y < CHUNK_HEIGHT; y++)
        {
            for (int x = 0; x < CHUNK_WIDTH; x++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y);

                playground.SetTile(actualPosition + tilePosition, chunk.playground[i].tile);
                background.SetTile(actualPosition + tilePosition, chunk.background[i].tile);
                foreground.SetTile(actualPosition + tilePosition, chunk.foreground[i].tile);

                i++;
            }
        }

        foreach (ObjectData objectData in chunk.gameObjects)
        {
            GameObject instance = Instantiate(objectData.prefab, objectData.position + actualPosition, objectData.rotation);
            instance.transform.localScale = objectData.scale;
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

    void OnDrawGizmos()
    {
        if (!showChunkGrid) return;

        Gizmos.color = Color.green;
        int chunks = database.chunks.Count + 1;

        for (int i = 0; i <= chunks; i++)
        {
            Vector3 from = new Vector3(i * CHUNK_WIDTH, 0, 0);
            Vector3 to = new Vector3(i * CHUNK_WIDTH, chunks * CHUNK_HEIGHT, 0);
            Gizmos.DrawLine(from, to);

            from = new Vector3(0, i * CHUNK_HEIGHT, 0);
            to = new Vector3(chunks * CHUNK_WIDTH, i * CHUNK_HEIGHT, 0);
            Gizmos.DrawLine(from, to);
        }
    }
}
