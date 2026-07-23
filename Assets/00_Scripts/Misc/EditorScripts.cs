using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldGenerator))]
public class EditorScripts : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WorldGenerator worldGen = (WorldGenerator)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Bake"))
        {
            worldGen.Bake();
        }
    }
}