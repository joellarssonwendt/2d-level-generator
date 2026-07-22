using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelGenerator))]
public class EditorScripts : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelGenerator levelGenerator = (LevelGenerator)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Bake"))
        {
            levelGenerator.Bake();
        }
    }
}