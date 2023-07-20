using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HydraulicManager))][CanEditMultipleObjects]
public class HydraulicManager_Inspector : Editor
{
    private HydraulicManager ManagerBase;

    public override void OnInspectorGUI()
    {
        if(ManagerBase == null){
            ManagerBase = target as HydraulicManager;
        }

        Undo.RecordObject(ManagerBase, "Hydraulic manager values changed");
        if (ManagerBase.heightMapData != null){
            Undo.RecordObject(ManagerBase.heightMapData, "Heightmap data values changed");
        }

        if(!DrawOverview()){
            return;
        }

        if(GUI.changed){
            EditorUtility.SetDirty(ManagerBase.heightMapData);
        }
    }

    private bool DrawOverview()
    {
        using (new GUILayout.VerticalScope(EditorHelper.GetColoredStyle(EditorHelper.GroupBoxCol)))
        {
            EditorHelper.Header("Overview");

            HeightMapData heightMapData = ManagerBase.heightMapData;
            EditorGUI.BeginChangeCheck();

            heightMapData = (HeightMapData)EditorGUILayout.ObjectField(heightMapData, typeof(HeightMapData), true);
            if (EditorGUI.EndChangeCheck()){
                ManagerBase.heightMapData = heightMapData;
            }

            if (ManagerBase.heightMapData == null)
            {
                EditorGUILayout.LabelField("A valid HeightMapData object is required!", EditorStyles.boldLabel);
                GUILayout.EndVertical();
                return false;
            }

            using (new GUILayout.HorizontalScope())
            {
                float CachedLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80f;
                ManagerBase.seed = EditorGUILayout.IntField("Map Seed", ManagerBase.seed);
                EditorGUIUtility.labelWidth = CachedLabelWidth;

                if (GUILayout.Button("Generate Seed")){
                    ManagerBase.seed = (int)Random.Range(-10000f, 10000f);
                }

                if (GUILayout.Button("Generate Terrain Base")){
                    ManagerBase.GenerateTerrainBase(ManagerBase.seed);
                }
            }
        }
        return true;
    }
}