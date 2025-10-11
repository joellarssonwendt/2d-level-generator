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

    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private int width, height;

    void Start()
    {
        GenerateLevel();
    }

    private void GenerateLevel()
    {
        for (int y = 0; y < height; y++)
        {
            for ( int x = 0; x < width; x++)
            {
                Debug.Log("X = " + x);
                Debug.Log("Y = " + y);
            }
        }
    }
}
