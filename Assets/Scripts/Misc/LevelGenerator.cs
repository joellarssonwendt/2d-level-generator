using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGenerator : MonoBehaviour
{
    private class LevelChunk
    {
        public List<TileBase> tilemap = new();
        public List<TileBase> background = new();
        public List<TileBase> foreground = new();
        public Dictionary<Vector3, GameObject> gameObjects;
    }

    private static LevelGenerator singleton;

    [Header("World Seed")]
    [SerializeField] private string seedString = "";

    [Header("References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Tilemap tilemap, background, foreground;

    List<LevelChunk> chunks = new();
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

        SerializeChunks();

        Debug.Log("LevelGenerator.cs | Baking completed!");
    }

    private void SerializeChunks()
    {
        LevelChunk chunk = new();
        Vector3Int pos = new();

        for (int y = 0; y < 18; y++)
        {
            pos.z = y;

            for (int x = 0; x < 32; x++)
            {
                pos.x = x;

                chunk.tilemap.Add(tilemap.GetTile(pos));
                chunk.background.Add(background.GetTile(pos));
                chunk.foreground.Add(foreground.GetTile(pos));
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
