using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HydraulicManager : MonoBehaviour
{
    private const int MapPreviewComputeScale = 50;
    private const int MapPreviewRenderScale = 150;

    public enum HeightMapPreviewMode
    {
        BaseNoise,
        FractalNoise,
    }

    [SerializeField]
    private int seed;
    [SerializeField]
    private int mapDensityIndex;
    [SerializeField]
    private bool debugEditorInstance;
    [SerializeField]
    private bool drawVertexGizmos;
    [SerializeField]
    private Color gizmoColor = Color.black;
    [SerializeField]
    private float gizmoScale = 0.1f;

    public HeightMapData heightMapData;
    public ShadingData shadingData;

    [SerializeField]
    private HeightMapPreviewMode heightMapPreviewMode;
    [SerializeField]
    private int heightMapPreviewSeed;
    public Texture2D heightMapPreview { get; set; }

    [HideInInspector]
    public bool drawHeightMapPreview;
    [HideInInspector]
    public bool drawHeightMapData;
    [HideInInspector]
    public bool drawHydraulicData;
    [HideInInspector]
    public bool drawShadingData;
    [HideInInspector]
    public bool drawDebugData;

    private HeightMap heightMap;
    private  GameObject editorInstance;

    #if UNITY_EDITOR
    public void OnValidate(){
        UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
    }

    private void NotifyOfUpdatedValues()
    {
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
        OnValuesUpdated();
    }

    private void OnValuesUpdated()
    {
        if(heightMapData != null)
        {
            heightMapData.onValuesUpdated -= OnValuesUpdated;
            heightMapData.onValuesUpdated += OnValuesUpdated;
            UpdateHeightMapPreview();
        }
        UpdateEditorInstanceFlags();
    }
    #endif

    private HeightMap GenerateHeightMap(int seed = 0) {
        return HydraulicHelper.GenerateTerrainHeightMap(mapDensityIndex, heightMapData, seed);
    }

    private Texture2D GenerateHeightMapSampleTexture()
    {
        HeightMap HeightMap = HydraulicHelper.GenerateNoiseMap(MapPreviewComputeScale, heightMapData, heightMapPreviewSeed, heightMapPreviewMode == HeightMapPreviewMode.BaseNoise);
        return HydraulicHelper.TextureFromHeightMap(HeightMap);
    }

    public void UpdateHeightMapPreview()
    {
        if (!drawHeightMapPreview) {
            return;
        }
        heightMapPreview = GenerateHeightMapSampleTexture();
        heightMapPreview = HydraulicHelper.ResizeTexture(heightMapPreview, MapPreviewRenderScale, MapPreviewRenderScale);
    }

    private Mesh GenerateTerrainMesh(int seed = 0)
    {
        heightMap = GenerateHeightMap(seed);

        int mapScale = heightMap.mapScale;
        Vector3[] vertices = new Vector3[mapScale * mapScale];
        int[] triangles = new int[mapScale * mapScale * 6];

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int x = 0; x < mapScale; x++)
        {
            for (int y = 0; y < mapScale; y++)
            {
                float height = heightMap.heightValues[x, y];
                vertices[vertexIndex] = new Vector3(x, height, y);

                bool createTriangle = x < mapScale - 1 && y < mapScale - 1;
                if (createTriangle)
                {
                    triangles[triangleIndex] = vertexIndex + 0;
                    triangles[triangleIndex + 1] = vertexIndex + 1;
                    triangles[triangleIndex + 2] = vertexIndex + mapScale;
                    triangles[triangleIndex + 3] = vertexIndex + mapScale;
                    triangles[triangleIndex + 4] = vertexIndex + 1;
                    triangles[triangleIndex + 5] = vertexIndex + mapScale + 1;
                    triangleIndex += 6;
                }
                vertexIndex++;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;
    }

    private void OnDrawGizmos () {
        if (!drawVertexGizmos || editorInstance == null) {
            return;
        }

        Gizmos.color = gizmoColor;
        Vector3[] vertices = editorInstance.GetComponent<MeshFilter>().sharedMesh.vertices;
		for (int i = 0; i < vertices.Length; i++) {
			Gizmos.DrawSphere(vertices[i], gizmoScale);
		}
	}

    public void CreateEditorInstance()
    {
        DestroyEditorInstance();
        editorInstance = new GameObject("EditorInstance_IDENTIFIER[230543]");
        editorInstance.transform.SetParent(transform);
        UpdateEditorInstanceFlags();

        Mesh terrainMesh = GenerateTerrainMesh(seed);
        MeshFilter MeshFilter = editorInstance.AddComponent<MeshFilter>();
        MeshRenderer MeshRenderer = editorInstance.AddComponent<MeshRenderer>();

        MeshFilter.sharedMesh = terrainMesh;
        MeshRenderer.material = shadingData != null ? shadingData.MeshMaterial : new Material(Shader.Find("Mobile/Unlit (Supports Lightmap)"));
        //editorInstance.transform.localScale = Vector3.one * ((int)MapSize / 10);
    }

    private void UpdateEditorInstanceFlags()
    {
        if (editorInstance != null) {
            editorInstance.hideFlags = debugEditorInstance ? HideFlags.None : HideFlags.HideInHierarchy;
        }
    }

    private void DestroyEditorInstance()
    {
        if (editorInstance == null) {
            editorInstance = GameObject.Find("EditorInstance_IDENTIFIER[230543]");
        }

        if (editorInstance != null)
        {
            if (Application.isPlaying) {
                Destroy(editorInstance);
            }
            else {
                DestroyImmediate(editorInstance);
            }
        }
    }
}