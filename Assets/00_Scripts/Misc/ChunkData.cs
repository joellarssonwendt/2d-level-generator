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
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public ObjectData(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        this.prefab = prefab;
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
    }
}

[System.Serializable]
public class ChunkData
{
    public List<TileData> playground = new();
    public List<TileData> background = new();
    public List<TileData> foreground = new();
    public List<ObjectData> gameObjects = new();

    public List<int> topConnections = new();
    public List<int> bottomConnections = new();
    public List<int> leftConnections = new();
    public List<int> rightConnections = new();
}