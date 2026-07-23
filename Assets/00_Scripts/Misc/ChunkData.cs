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
    public GameObject prefab;
    public Vector3 localPosition;
}

[System.Serializable]
public class ChunkData
{
    public List<TileData> tilemap = new();
    public List<TileData> background = new();
    public List<TileData> foreground = new();
    public List<ObjectData> gameObjects = new();

    public List<int> topConnections = new();
    public List<int> bottomConnections = new();
    public List<int> leftConnections = new();
    public List<int> rightConnections = new();
}