using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainToHeightmap))]
public class TerrainToHeightmapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var targetScript = (TerrainToHeightmap) target;
        DrawDefaultInspector();
        if (GUILayout.Button("Extract Heightmap"))
        {
            targetScript.Convert();
        }
        
    }
}