using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HydraulicManager : MonoBehaviour
{
    private const int baseMapDensityDivisor = 8; // Must be divisible by 4
    private const int mapPreviewComputeScale = 50;
    private const int mapPreviewRenderScale = 150;
    public const UnityEngine.Rendering.IndexFormat indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

    public enum HeightMapPreviewMode
    {
        BaseNoise,
        FractalNoise,
    }

    [SerializeField]
    private int seed;
    [SerializeField]
    private MapSize mapSize = MapSize._256;
    [SerializeField][Range(1, 500)]
    private int mapDensity;
    [SerializeField]
    private bool debugEditorInstance;
    [SerializeField]
    private bool drawVertexGizmos;
    [SerializeField]
    private Color gizmoColor = Color.black;
    [SerializeField]
    private float gizmoScale = 0.1f;

    public HeightMapData heightMapData;
    public HydraulicData hydraulicData;
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

    public int NumVertsPerLine { get { return (int)((float)mapSize / baseMapDensityDivisor) * mapDensity; } }
    public float VertexSpacing { 
        get {
            float spacing = (float)mapSize / NumVertsPerLine;
            return spacing + ((spacing / NumVertsPerLine) * (1 - mapDensity)); 
        } 
    }
    public Vector3 MapOffset { get { return new Vector3(-(float)mapSize / 2, 0, -(float)mapSize / 2) + transform.position; } }

    #if UNITY_EDITOR
    public void OnValidate() {
        UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
    }

    private void NotifyOfUpdatedValues()
    {
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
        OnValuesUpdated();
    }

    private void OnValuesUpdated()
    {
        if (heightMapData != null)
        {
            heightMapData.onValuesUpdated -= OnValuesUpdated;
            heightMapData.onValuesUpdated += OnValuesUpdated;
            UpdateHeightMapPreview();
        }
        UpdateEditorInstanceFlags();
        gizmoScale = Mathf.Max(gizmoScale, 0f);
    }
    #endif

    private HeightMap GenerateErosionMap(HeightMap heightMap)
    {
        // Generate random droplet starting positions across the map
        Vector2[] dropletPositions = new Vector2[hydraulicData.DropletCount];
        System.Random RNG = new System.Random(seed);
        for (int i = 0; i < dropletPositions.Length; i++) {
            float x = RNG.Next(0, heightMap.mapScale - 1);
            float y = RNG.Next(0, heightMap.mapScale - 1);
            dropletPositions[i] = new Vector2(x, y);
        }
        return HydraulicHelper.GenerateErosionMap(heightMap, dropletPositions, hydraulicData);
    }

    private HeightMap GenerateHeightMap(int seed = 0) {
        return HydraulicHelper.GenerateTerrainHeightMap(NumVertsPerLine, VertexSpacing, mapSize, heightMapData, seed);
    }

    private Texture2D GenerateHeightMapSampleTexture()
    {
        HeightMap HeightMap = HydraulicHelper.GenerateTerrainHeightMap(mapPreviewComputeScale, heightMapData, heightMapPreviewMode, heightMapPreviewSeed);
        return HydraulicHelper.TextureFromHeightMap(HeightMap);
    }

    public void UpdateHeightMapPreview()
    {
        if (!drawHeightMapPreview) {
            return;
        }
        heightMapPreview = GenerateHeightMapSampleTexture();
        heightMapPreview = HydraulicHelper.ResizeTexture(heightMapPreview, mapPreviewRenderScale, mapPreviewRenderScale);
    }

    private Mesh GenerateTerrainMesh(int seed = 0)
    {
        heightMap = GenerateHeightMap(seed);
        heightMap = GenerateErosionMap(heightMap);

        int numVerts = heightMap.mapScale;
        Vector3[] vertices = new Vector3[numVerts * numVerts];
        int[] triangles = new int[vertices.Length * 6];
        Vector2[] uv = new Vector2[vertices.Length];

        int vertexIndex = 0;
        int triangleIndex = 0;

        float spacing = VertexSpacing;
        Vector3 offset = MapOffset;
        for (int x = 0; x < numVerts; x++)
        {
            for (int y = 0; y < numVerts; y++)
            {
                float height = heightMap.heightValues[x, y] * heightMapData.HeightScalar;
                vertices[vertexIndex] = new Vector3((float)x * spacing, height, (float)y * spacing) + offset;
                uv[vertexIndex] = new Vector2((float)x / numVerts, (float)y / numVerts);

                bool createTriangle = x < numVerts - 1 && y < numVerts - 1;
                if (createTriangle)
                {
                    triangles[triangleIndex] = vertexIndex + 0;
                    triangles[triangleIndex + 1] = vertexIndex + 1;
                    triangles[triangleIndex + 2] = vertexIndex + numVerts;
                    triangles[triangleIndex + 3] = vertexIndex + numVerts;
                    triangles[triangleIndex + 4] = vertexIndex + 1;
                    triangles[triangleIndex + 5] = vertexIndex + numVerts + 1;
                    triangleIndex += 6;
                }
                vertexIndex++;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = indexFormat;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;
    }

    public void CreateEditorInstance()
    {
        DestroyEditorInstance();
        editorInstance = new GameObject("EditorInstance_IDENTIFIER["+gameObject.GetHashCode()+"]");
        editorInstance.transform.SetParent(transform);
        UpdateEditorInstanceFlags();

        Mesh terrainMesh = GenerateTerrainMesh(seed);
        MeshFilter MeshFilter = editorInstance.AddComponent<MeshFilter>();
        MeshRenderer MeshRenderer = editorInstance.AddComponent<MeshRenderer>();

        MeshFilter.sharedMesh = terrainMesh;
        MeshRenderer.material = shadingData != null ? shadingData.MeshMaterial : new Material(Shader.Find("Mobile/Unlit (Supports Lightmap)"));
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
            editorInstance = GameObject.Find("EditorInstance_IDENTIFIER["+gameObject.GetHashCode()+"]");
        }

        for (int i = 0; i < transform.childCount; i++) {
            DestroyImmediate(transform.GetChild(i).gameObject);
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

    private void OnDrawGizmos()
    {
        if (!drawVertexGizmos || editorInstance == null) {
            return;
        }
        Gizmos.color = gizmoColor;
        Vector3[] vertices = editorInstance.GetComponent<MeshFilter>().sharedMesh.vertices;
        for (int i = 0; i < vertices.Length; i++) {
            Gizmos.DrawSphere(vertices[i], gizmoScale);
        }
    }
}