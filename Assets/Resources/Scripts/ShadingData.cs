using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShadingData", menuName = "Hydraulic/ShadingData")]
public class ShadingData : ScriptableObject
{
    [SerializeField]
    private Material meshMaterial;

    public Material MeshMaterial { get { return meshMaterial; } }
}