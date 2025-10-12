using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGen : MonoBehaviour
{
    public static LevelGen Singleton;

    private void Awake()
    {
        if (LevelGen.Singleton != null && LevelGen.Singleton != this)
        {
            Destroy(gameObject);
        }
        else
        {
            LevelGen.Singleton = this;
        }
    }

    [SerializeField] private string seedString = "";
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase defaultTile;
    [SerializeField] private int width, height;

    private int[,] grid;
    private System.Random rng;

    void Start()
    {
        HashSeed();
        GenerateGrid();
        DrawTiles();
    }

    private void HashSeed()
    {
        int seed;

        if (string.IsNullOrEmpty(seedString))
        {
            seed = Random.Range(1, int.MaxValue);

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

        Debug.Log($"Hidden seed: {seed}");
        rng = new System.Random(seed);
    }

    private void GenerateGrid()
    {
        grid = new int[width, height];

        float f = (float)rng.NextDouble();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noise = Mathf.PerlinNoise(x * f, y * f);

                if (noise > 0.5f)
                {
                    grid[x, y] = 1;
                }

                if (y == 0)
                {
                    grid[x, y] = 1;
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
                    TileBase tile = defaultTile;
                    tilemap.SetTile(pos, tile);
                }
                else
                {
                    tilemap.SetTile(pos, null);
                }
            }
        }
    }
}
