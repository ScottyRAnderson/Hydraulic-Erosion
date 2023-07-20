using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeightMapData", menuName = "Hydraulic/HeightMapData")]
public class HeightMapData : UpdatableData
{
    [SerializeField]
    [Tooltip("The initial/base height of the terrain.")]
    private float TerrainBaseHeight = 0f;
    [SerializeField][Tooltip("Layers of noise applied to the mesh, array order is equal to additive order.")]
    private NoiseLayer[] NoiseLayers;

    public float terrainBaseHeight { get { return TerrainBaseHeight; } }
    public NoiseLayer[] noiseLayers { get { return NoiseLayers; } }

    #if UNITY_EDITOR
    protected override void OnValidate()
    {
        for (int i = 0; i < NoiseLayers.Length; i++) {
            NoiseLayers[i].ValidateConfig();
        }
        base.OnValidate();
    }
    #endif
}