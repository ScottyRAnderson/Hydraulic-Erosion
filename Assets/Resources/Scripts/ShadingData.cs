using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ShadingData", menuName = "Hydraulic/ShadingData")]
public class ShadingData : UpdatableData
{
    [SerializeField]
    private Material meshMaterial;
    [SerializeField][Range(0f, 1f)]
    private float smoothness;
    [SerializeField][Range(0f, 1f)]
    private float metallic;
    [SerializeField]
    private Color flatColor;
    [SerializeField]
    private Color steepColor;
    [SerializeField][Range(0f, 1f)]
    private float steepnessThreshold = 0.8f;
    [SerializeField][Range(0f, 1f)]
    private float colorBlend = 0.1f;
    [SerializeField][Range(0f, 1f)]
    private float rimFactor = 0.1f;
    [SerializeField]
    private float rimPower = 1f;
    [SerializeField]
    private Color rimColor;

    public Material MeshMaterial { get { return meshMaterial; } }

    #if UNITY_EDITOR
    protected override void OnValidate()
    {
        rimPower = Mathf.Max(0f, rimPower);
        ApplyProperties();
        base.OnValidate();
    }
    #endif

    public void ApplyProperties() {
        if (meshMaterial == null) {
            return;
        }

        Undo.RecordObject(meshMaterial, "Updated material shading properties");
        meshMaterial.SetFloat("smoothness", smoothness);
        meshMaterial.SetFloat("metallic", metallic);
        meshMaterial.SetColor("flatColor", flatColor);
        meshMaterial.SetColor("steepColor", steepColor);
        meshMaterial.SetFloat("steepnessThreshold", steepnessThreshold);
        meshMaterial.SetFloat("colorBlend", colorBlend);
        meshMaterial.SetFloat("rimFactor", rimFactor);
        meshMaterial.SetFloat("rimPower", rimPower);
        meshMaterial.SetColor("rimColor", rimColor);
        EditorUtility.SetDirty(meshMaterial);
    }
}