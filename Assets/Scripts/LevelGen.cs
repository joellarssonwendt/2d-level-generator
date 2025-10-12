using NUnit.Framework.Internal;
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

    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase defaultTile;
    [SerializeField] private int width, height;
    private int[,] grid;

    void Start()
    {
        grid = new int[width, height];
        Test();
    }

    private void Test()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y == 0)
                {
                    grid[x, y] = 1;
                    tilemap.SetTile(new Vector3Int(x, y, 0), defaultTile);
                }
            }
        }
    }
}
