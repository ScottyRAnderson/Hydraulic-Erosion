using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseHelper
{
    public static float[,] GenerateNoiseMap(int vertsPerLine, float vertexSpacing, MapSize mapSize, HeightMapData heightMapData, int seed = 0, bool isolateBase = false)
	{
		float[,] noiseMap = new float[vertsPerLine, vertsPerLine];

		System.Random RNG = new System.Random(seed);
		NoiseOctave[] noiseOctaves = new NoiseOctave[heightMapData.noiseLayers.Length];
		for (int i = 0; i < heightMapData.noiseLayers.Length; i++)
		{
			NoiseLayer noiseConfig = heightMapData.noiseLayers[i];
			noiseOctaves[i] = new NoiseOctave();
			noiseOctaves[i].OctaveOffsets = new Vector2[noiseConfig.octaves];

			float amplitude = 1;
			for (int o = 0; o < noiseConfig.octaves; o++)
			{
				float offsetX = RNG.Next(-100000, 100000);
				float offsetY = RNG.Next(-100000, 100000);
				noiseOctaves[i].OctaveOffsets[o] = new Vector2(offsetX, offsetY);
				amplitude *= noiseConfig.persistance;
			}
		}

		for (int y = 0; y < vertsPerLine; y++)
        {
            for (int x = 0; x < vertsPerLine; x++)
            {
				float sampleX = (x * vertexSpacing) - ((float)mapSize / 2);
				float sampleY = (y * vertexSpacing) - ((float)mapSize / 2);
				float heightSum = 0f;
				if(isolateBase)
                {
					for (int i = 0; i < heightMapData.noiseLayers.Length; i++)
					{
						NoiseLayer noiseConfig = new NoiseLayer(heightMapData.noiseLayers[i].enabled, heightMapData.noiseLayers[i].noiseFunction,
							heightMapData.noiseLayers[i].noiseScale, 1, heightMapData.noiseLayers[i].persistance, heightMapData.noiseLayers[i].lacunarity, heightMapData.noiseLayers[i].gain,
							heightMapData.noiseLayers[i].heightScalar);
						if (!noiseConfig.enabled){
							continue;
						}

						float NoiseHeight = GetNoiseValue(sampleX, sampleY, noiseConfig, noiseOctaves[i], noiseConfig.noiseFunction);
						heightSum += NoiseHeight * noiseConfig.heightScalar;
					}
				}
				else
                {
					for (int i = 0; i < heightMapData.noiseLayers.Length; i++)
					{
						NoiseLayer noiseConfig = heightMapData.noiseLayers[i];
						if (!noiseConfig.enabled){
							continue;
						}

						float noiseHeight = GetNoiseValue(sampleX, sampleY, noiseConfig, noiseOctaves[i], noiseConfig.noiseFunction);
						heightSum += noiseHeight * noiseConfig.heightScalar;
					}
				}
				noiseMap[x, y] = heightSum;
			}
        }
		return noiseMap;
	}

    public static float GetNoiseValue(float sampleX, float sampleY, NoiseLayer noiseConfig, NoiseOctave octaveData, NoiseFunction noiseFunction)
    {
		float noiseValue = 0f;
		switch(noiseFunction)
        {
			case NoiseFunction.Perlin:
				noiseValue = ComputeFractalNoise2D(sampleX, sampleY, noiseConfig, octaveData);
				break;
			case NoiseFunction.Ridge:
				noiseValue = ComputeRidgeNoise2D(sampleX, sampleY, noiseConfig, octaveData);
				break;
        }
		return noiseValue;
    }

	public static float ComputeFractalNoise2D(float sampleX, float sampleY, NoiseLayer noiseConfig, NoiseOctave octaveData)
    {
		float perlinValue = 0f;
		float amplitude = 1f;
		float frequency = 1f;
		for (int o = 0; o < noiseConfig.octaves; o++)
		{
			float offsetSampleX = (sampleX + octaveData.OctaveOffsets[o].x) / noiseConfig.noiseScale * frequency;
			float offsetSampleY = (sampleY + octaveData.OctaveOffsets[o].y) / noiseConfig.noiseScale * frequency;

			float noiseValue = Mathf.PerlinNoise(offsetSampleX, offsetSampleY) * 2 - 1;
			perlinValue += noiseValue * amplitude;

			amplitude *= noiseConfig.persistance;
			frequency *= noiseConfig.lacunarity;
		}
		return perlinValue;
    }

	public static float ComputeRidgeNoise2D(float sampleX, float sampleY, NoiseLayer noiseConfig, NoiseOctave octaveData)
    {
		float ridgeValue = 0f;
		float amplitude = 1f;
		float frequency = 1f;
		for (int o = 0; o < noiseConfig.octaves; o++)
		{
			float offsetSampleX = (sampleX + octaveData.OctaveOffsets[o].x) / noiseConfig.noiseScale * frequency;
			float offsetSampleY = (sampleY + octaveData.OctaveOffsets[o].y) / noiseConfig.noiseScale * frequency;

			float noiseValue = 1 - Mathf.Abs(Mathf.PerlinNoise(offsetSampleX, offsetSampleY));
			noiseValue = Mathf.Pow(Mathf.Abs(noiseValue), noiseConfig.gain);
			ridgeValue += noiseValue * amplitude;

			amplitude *= noiseConfig.persistance;
			frequency *= noiseConfig.lacunarity;
		}
		return ridgeValue;
	}
}