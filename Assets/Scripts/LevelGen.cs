using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGen : MonoBehaviour
{
    public static LevelGen Singleton;

    [SerializeField] private GameObject playerPrefab, lavaPrefab;
    [SerializeField] private string seedString = "";
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase ruleTile;
    [SerializeField] private int width, height;

    private int[,] grid;
    private System.Random rng;

    void Awake()
    {
        if (LevelGen.Singleton != null && LevelGen.Singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            LevelGen.Singleton = this;
        }

        rng = HashSeed(); // Do NOT use 'rng' before this line. Required for PCG determinism!
        GenerateGrid();
        DrawTilemap();
        SpawnPlayer();
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

    private void GenerateGrid()
    {
        grid = new int[width, height];

        float granularity = (float)rng.NextDouble() * 0.05f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noise = Mathf.PerlinNoise(x * granularity, y * granularity);

                if (noise > 0.5f)
                {
                    grid[x, y] = 1;
                }
            }
        }
    }

    private void DrawTilemap()
    {
        float r = (float)rng.NextDouble();
        float g = (float)rng.NextDouble();
        float b = (float)rng.NextDouble();
        tilemap.color = new Color(r, g, b);

        Vector3Int pos = new();

        for (int y = 0; y < height; y++)
        {
            pos.y = y;

            for (int x = 0; x < width; x++)
            {
                pos.x = x;

                if (grid[x, y] == 1)
                {
                    tilemap.SetTile(pos, ruleTile);
                }
                else
                {
                    tilemap.SetTile(pos, null);
                }
            }
        }

        pos = new();

        for (int x = -50; x < width + 50; x++)
        {
            if (x % 10 == 0)
            {
                pos.x = x + 1;
                Instantiate(lavaPrefab, pos, Quaternion.identity);
            }
        }
    }

    private void SpawnPlayer()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height; y++)
            {
                if (grid[x, y] == 0 && grid[x, y - 1] == 1)
                {
                    Instantiate(playerPrefab, new Vector3(x + 0.5f, y, 0), Quaternion.identity);
                    return;
                }
            }
        }
    }
}
