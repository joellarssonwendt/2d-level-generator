using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGen : MonoBehaviour
{
    public static LevelGen Singleton;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string seedString = "";
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase ruleTile;
    [SerializeField] private int width, height;
    [SerializeField] private GameObject spikesPrefab;
    [SerializeField] private GameObject lavaPrefab;
    [SerializeField] private GameObject frogPrefab;

    private int[,] grid;
    private System.Random rng;

    private void Awake()
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

        HashSeed();
        GenerateGrid();
        DrawTiles();
        SpawnPlayer();
        AddHazards();
        SpawnEnemies();
    }

    private void HashSeed()
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
        rng = new System.Random(seed);
    }

    private void GenerateGrid()
    {
        grid = new int[width, height];

        float a = (float)rng.NextDouble() * 0.05f;
        float b = (float)rng.NextDouble() * 0.2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y < 5)
                {
                    float noise = Mathf.PerlinNoise(x * a, y * a);

                    if (noise > 0.4f)
                    {
                        grid[x, y] = 1;
                    }

                    noise = Mathf.PerlinNoise(x * b, y * b);

                    if (noise > 0.5f)
                    {
                        grid[x, y] = 0;
                    }
                }

                if (y == 3 && x > 2 && x % 5 == 0)
                {
                    int i = rng.Next(-2, 2);

                    grid[x, y+i] = 1;
                    grid[x, y-1+i] = 1;
                    grid[x-1, y+i] = 1;
                    grid[x-1, y-1+i] = 1;
                }

                if (x == 1 && y == 1)
                {
                    grid[x, y] = 1;
                    grid[x-1, y] = 1;
                    grid[x, y-1] = 1;
                    grid[x-1, y-1] = 1;
                }
            }
        }
    }

    private void DrawTiles()
    {
        Vector3Int pos = new();

        for (int y = 0; y < height; y++)
        {
            pos.y = y;

            for (int x = 0; x < width; x++)
            {
                pos.x = x;

                if (grid[x, y] == 1)
                {
                    TileBase tile = ruleTile;
                    tilemap.SetTile(pos, tile);
                }
                else
                {
                    tilemap.SetTile(pos, null);
                }
            }
        }
    }

    private void SpawnPlayer()
    {
        for (int y = 0; y < height; y++)
        {
            if (grid[0, y] == 0)
            {
                Instantiate(playerPrefab, new Vector3(0.5f, y, 0), Quaternion.identity);
                return;
            }
        }
    }

    private void AddHazards()
    {
        int i = rng.Next(2, 10);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y > 0 && y < 4 && x > 0 && x % i == 0 && grid[x, y+1] == 0 && grid[x, y] == 1)
                {
                    Instantiate(spikesPrefab, new Vector3(x + 0.5f, y + 1.5f, 0), Quaternion.identity);
                }
            }
        }
    }

    private void SpawnEnemies()
    {
        int i = rng.Next(2, 10);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y > 0 && y < height - 1 && x > 0 && x % i == 0 && grid[x, y + 1] == 0 && grid[x, y] == 1)
                {
                    Instantiate(frogPrefab, new Vector3(x + 0.5f, y + 1f, 0), Quaternion.identity);
                }
            }
        }
    }
}
