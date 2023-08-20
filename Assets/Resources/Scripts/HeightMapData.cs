using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeightMapData", menuName = "Hydraulic/HeightMapData")]
public class HeightMapData : UpdatableData
{
    [SerializeField][Tooltip("Multiplies the overall height by the specified amount.")]
    private float heightScalar = 10f;
    [SerializeField][Tooltip("Layers of noise applied to the mesh, array order is equal to additive order.")]
    private NoiseLayer[] NoiseLayers;

    public float HeightScalar { get { return heightScalar; } }
    public NoiseLayer[] noiseLayers { get { return NoiseLayers; } }

    #if UNITY_EDITOR
    protected override void OnValidate()
    {
        heightScalar = Mathf.Max(0f, heightScalar);
        for (int i = 0; i < NoiseLayers.Length; i++) {
            NoiseLayers[i].ValidateConfig();
        }
        base.OnValidate();
    }
    #endif
}