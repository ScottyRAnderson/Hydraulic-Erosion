using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseLayer
{
    [SerializeField]
    private bool Enabled = true;
    [SerializeField]
    private NoiseFunction NoiseFunction = NoiseFunction.Perlin;

    [Space]

    [SerializeField][Tooltip("The distance at which the noise is viewed from.")]
    private float NoiseScale = 8f;
    [SerializeField][Range(1, 8)][Tooltip("The number of levels of detail.")]
    private int Octaves = 2;
    [SerializeField][Range(0, 1)][Tooltip("How much each octave contributes to the overall shape. [Adjusts amplitude]")]
    private float Persistance = 0.4f;
    [SerializeField][Tooltip("How much detail is added or removed at each octave. [Adjusts frequency]")]
    private float Lacunarity = 1f;
    [SerializeField][Tooltip("How quickly the noise converges.")]
    private float Gain = 1f;
    [SerializeField][Tooltip("Multiplies the overall height by the specified amount.")]
    private float HeightScalar = 1f;
    public bool enabled { get { return Enabled; } }
    public NoiseFunction noiseFunction { get { return NoiseFunction; } }
    public float noiseScale { get { return NoiseScale; } }
    public int octaves { get { return Octaves; } }
    public float persistance { get { return Persistance; } }
    public float lacunarity { get { return Lacunarity; } }
    public float gain { get { return Gain; } }
    public float heightScalar { get { return HeightScalar; } }

    public void ValidateConfig()
    {
        NoiseScale = Mathf.Max(NoiseScale, 0.0001f);
        Lacunarity = Mathf.Max(Lacunarity, 1f);
        Gain = Mathf.Max(Gain, 1f);
        HeightScalar = Mathf.Max(HeightScalar, 0f);
    }

    public NoiseLayer(NoiseLayer NoiseLayer)
    {
        Enabled = NoiseLayer.Enabled;
        NoiseFunction = NoiseLayer.NoiseFunction;
        NoiseScale = NoiseLayer.NoiseScale;
        Octaves = NoiseLayer.Octaves;
        Persistance = NoiseLayer.Persistance;
        Lacunarity = NoiseLayer.Lacunarity;
        Gain = NoiseLayer.Gain;
        HeightScalar = NoiseLayer.HeightScalar;
    }

    public NoiseLayer(bool Enabled, NoiseFunction NoiseFunction, float NoiseScale, int Octaves, float Persistance, float Lacunarity, float Gain, float HeightScalar)
    {
        this.Enabled = Enabled;
        this.NoiseFunction = NoiseFunction;
        this.NoiseScale = NoiseScale;
        this.Octaves = Octaves;
        this.Persistance = Persistance;
        this.Lacunarity = Lacunarity;
        this.Gain = Gain;
        this.HeightScalar = HeightScalar;
    }
}

public struct NoiseOctave {
    public Vector2[] OctaveOffsets;
}