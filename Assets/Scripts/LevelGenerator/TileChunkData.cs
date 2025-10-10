using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileChunkData", menuName = "Scriptable Objects/TileChunkData")]
public class TileChunkData : ScriptableObject
{
    public int width;
    public int height;
    public TileBase[] tiles;
}
