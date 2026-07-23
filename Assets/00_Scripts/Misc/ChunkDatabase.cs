using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChunkDatabase", menuName = "ChunkDatabase")]
public class ChunkDatabase : ScriptableObject
{
    public List<ChunkData> chunks = new();
}