using UnityEngine;
using UnityEditor;

public class MapCopierWindow : EditorWindow
{
    private string newMapName = "Floor_New";

    [MenuItem("Tools/Map Editor/Copy Current Map")]
    public static void ShowWindow()
    {
        GetWindow<MapCopierWindow>("Copy Map");
    }

    void OnGUI()
    {
        GUILayout.Label("Duplicate Current Map", EditorStyles.boldLabel);
        
        MapManager mapManager = FindObjectOfType<MapManager>();

        if (mapManager == null)
        {
            EditorGUILayout.HelpBox("MapManager is not active in the scene!", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Current Map:", mapManager.currentMapName);
        newMapName = EditorGUILayout.TextField("New Map Name:", newMapName);

        if (GUILayout.Button("Copy Now"))
        {
            mapManager.CopyCurrentMap(newMapName);
            EditorUtility.DisplayDialog("Success", $"Map successfully duplicated to {newMapName}! It is now loaded and ready to edit.", "OK");
            Close();
        }
    }
}
