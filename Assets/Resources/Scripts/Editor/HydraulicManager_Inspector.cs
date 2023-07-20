using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HydraulicManager))][CanEditMultipleObjects]
public class HydraulicManager_Inspector : Editor
{
    private HydraulicManager managerBase;
    private Editor heightMapDataEditor;
    private Editor shadingDataEditor;

    private SerializedProperty seed;
    private SerializedProperty mapSize;
    private SerializedProperty mapDensity;
    private SerializedProperty debugEditorInstance;
    private SerializedProperty drawVertexGizmos;
    private SerializedProperty gizmoColor;
    private SerializedProperty gizmoScale;
    private SerializedProperty heightMapPreviewMode;
    private SerializedProperty heightMapPreviewSeed;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        seed = serializedObject.FindProperty("seed");
        mapSize = serializedObject.FindProperty("mapSize");
        mapDensity = serializedObject.FindProperty("mapDensity");
        debugEditorInstance = serializedObject.FindProperty("debugEditorInstance");
        drawVertexGizmos = serializedObject.FindProperty("drawVertexGizmos");
        gizmoColor = serializedObject.FindProperty("gizmoColor");
        gizmoScale = serializedObject.FindProperty("gizmoScale");
        heightMapPreviewMode = serializedObject.FindProperty("heightMapPreviewMode");
        heightMapPreviewSeed = serializedObject.FindProperty("heightMapPreviewSeed");
        
        if(managerBase == null) {
            managerBase = target as HydraulicManager;
        }

        Undo.RecordObject(managerBase, "Hydraulic manager values changed");
        if (managerBase.heightMapData != null) {
            Undo.RecordObject(managerBase.heightMapData, "Heightmap data values changed");
        }
        if (managerBase.shadingData != null) {
            Undo.RecordObject(managerBase.shadingData, "Shading data values changed");
        }

        if(!DrawOverview()) {
            return;
        }
        EditorHelper.Space();
        DrawHeightMapData();
        EditorHelper.Space();
        DrawHydraulicData();
        EditorHelper.Space();
        DrawShadingData();
        EditorHelper.Space();
        DrawDebugData();
        serializedObject.ApplyModifiedProperties();
    }

    private bool DrawOverview()
    {
        using (new GUILayout.VerticalScope(EditorHelper.GetColoredStyle(EditorHelper.GroupBoxCol)))
        {
            EditorHelper.Header("Overview");
            HeightMapData heightMapData = managerBase.heightMapData;
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                heightMapData = (HeightMapData)EditorGUILayout.ObjectField(heightMapData, typeof(HeightMapData), true);
                if (check.changed) {
                    managerBase.heightMapData = heightMapData;
                }
            }

            if (managerBase.heightMapData == null) {
                EditorGUILayout.LabelField("A valid HeightMapData object is required!", EditorStyles.boldLabel);
                return false;
            }

            EditorGUILayout.PropertyField(mapSize);
            EditorGUILayout.PropertyField(mapDensity);

            GUI.enabled = false;
            int numVerts = (int)Mathf.Pow(managerBase.NumVertsPerLine, 2);
            bool vertexLimitReached = numVerts > 65535;
            EditorGUILayout.LabelField("Vertex Count: " + Mathf.Pow(managerBase.NumVertsPerLine, 2), EditorStyles.boldLabel);
            GUI.enabled = true;

            if (vertexLimitReached)
            {
                GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = Color.red;
                EditorGUILayout.LabelField("Max vertex limit reached. Reduce Map Size or Map Density!", style);
            }

            EditorHelper.Space();

            using (new GUILayout.HorizontalScope())
            {
                float CachedLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80f;
                EditorGUILayout.PropertyField(seed);
                EditorGUIUtility.labelWidth = CachedLabelWidth;

                if (GUILayout.Button("Generate Seed")){
                    seed.intValue = (int)Random.Range(-10000f, 10000f);
                }

                GUI.enabled = !vertexLimitReached;
                if (GUILayout.Button("Generate Terrain")){
                    managerBase.CreateEditorInstance();
                }
                GUI.enabled = true;
            }
        }
        return true;
    }

    private void DrawHeightMapData()
    {
        using (new GUILayout.VerticalScope(EditorHelper.GetColoredStyle(EditorHelper.GroupBoxCol)))
        {
            EditorGUI.indentLevel++;
            managerBase.drawHeightMapData = EditorHelper.Foldout(managerBase.drawHeightMapData, "Height Map", -(EditorGUIUtility.singleLineHeight / 2), 0f);
            if (managerBase.drawHeightMapData)
            {
                EditorGUI.indentLevel++;
                CreateCachedEditor(managerBase.heightMapData, null, ref heightMapDataEditor);
                heightMapDataEditor.OnInspectorGUI();

                EditorHelper.Space();

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    managerBase.drawHeightMapPreview = EditorHelper.Foldout(managerBase.drawHeightMapPreview, "Noise Preview", EditorStyles.label);
                    if (check.changed) {
                        managerBase.UpdateHeightMapPreview();
                    }
                }

                if (managerBase.drawHeightMapPreview)
                {
                    EditorGUI.indentLevel++;
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            float CachedLabelWidth = EditorGUIUtility.labelWidth;
                            EditorGUIUtility.labelWidth = 130f;
                            EditorGUILayout.PropertyField(heightMapPreviewMode, new GUIContent("Display Mode"));
                            EditorGUILayout.PropertyField(heightMapPreviewSeed, new GUIContent("Preview Seed"));
                            EditorGUIUtility.labelWidth = CachedLabelWidth;
                        }
                        if (check.changed){
                            managerBase.UpdateHeightMapPreview();
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope()) {
                        GUILayout.Space((EditorGUIUtility.currentViewWidth / 2) - 100);
                        GUILayout.Label(managerBase.heightMapPreview);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        if (GUI.changed) {
            EditorUtility.SetDirty(managerBase.heightMapData);
        }
    }

    private void DrawHydraulicData()
    {
        using (new GUILayout.VerticalScope(EditorHelper.GetColoredStyle(EditorHelper.GroupBoxCol)))
        {
            EditorGUI.indentLevel++;
            managerBase.drawHydraulicData = EditorHelper.Foldout(managerBase.drawHydraulicData, "Hydraulic Data", -(EditorGUIUtility.singleLineHeight / 2), 0f);
            if (managerBase.drawHydraulicData)
            {

            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawShadingData()
    {
        using (new GUILayout.VerticalScope(EditorHelper.GetColoredStyle(EditorHelper.GroupBoxCol)))
        {
            EditorGUI.indentLevel++;
            managerBase.drawShadingData = EditorHelper.Foldout(managerBase.drawShadingData, "Shading Data", -(EditorGUIUtility.singleLineHeight / 2), 0f);
            if (managerBase.drawShadingData)
            {
                EditorGUI.indentLevel++;
                ShadingData shadingData = managerBase.shadingData;
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    shadingData = (ShadingData)EditorGUILayout.ObjectField(shadingData, typeof(ShadingData), true);
                    if (check.changed) {
                        managerBase.shadingData = shadingData;
                    }
                }

                if (managerBase.shadingData == null) {
                    EditorGUILayout.LabelField("A valid ShadingData object is required!", EditorStyles.boldLabel);
                    EditorGUI.indentLevel -= 2;
                    return;
                }

                CreateCachedEditor(managerBase.shadingData, null, ref shadingDataEditor);
                shadingDataEditor.OnInspectorGUI();
                EditorGUI.indentLevel--;

                if (GUI.changed) {
                    EditorUtility.SetDirty(managerBase.shadingData);
                }
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawDebugData()
    {
        using (new GUILayout.VerticalScope(EditorHelper.GetColoredStyle(EditorHelper.GroupBoxCol)))
        {
            EditorGUI.indentLevel++;
            managerBase.drawDebugData = EditorHelper.Foldout(managerBase.drawDebugData, "Debug Data", -(EditorGUIUtility.singleLineHeight / 2), 0f);
            if (managerBase.drawDebugData)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(debugEditorInstance);
                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.PropertyField(drawVertexGizmos);
                    EditorGUILayout.PropertyField(gizmoScale);
                }
                EditorGUILayout.PropertyField(gizmoColor);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }
    }
}