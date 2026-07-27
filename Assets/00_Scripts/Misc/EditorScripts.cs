using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGenBakeButton : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WorldGenerator worldGen = (WorldGenerator)target;

        GUILayout.Space(10);

        if (GUILayout.Button($"Rebuild Chunk Database ({WorldGenerator.CHUNK_WIDTH}x{WorldGenerator.CHUNK_HEIGHT})"))
        {            
            worldGen.Bake();
        }
    }
}