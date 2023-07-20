using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseHelper
{
    public static float[,] GenerateTerrainMap(int MapScale, HeightMapData HeightMapData, int Seed = 0)
	{
		float[,] NoiseMap = new float[MapScale, MapScale];

		System.Random RNG = new System.Random(Seed);
		NoiseOctave[] NoiseOctaves = new NoiseOctave[HeightMapData.noiseLayers.Length];
		for (int i = 0; i < HeightMapData.noiseLayers.Length; i++)
		{
			NoiseLayer NoiseConfig = HeightMapData.noiseLayers[i];
			NoiseOctaves[i] = new NoiseOctave();
			NoiseOctaves[i].OctaveOffsets = new Vector2[NoiseConfig.octaves];

			float Amplitude = 1;
			for (int o = 0; o < NoiseConfig.octaves; o++)
			{
				float offsetX = RNG.Next(-100000, 100000);
				float offsetY = RNG.Next(-100000, 100000);
				NoiseOctaves[i].OctaveOffsets[o] = new Vector2(offsetX, offsetY);
				Amplitude *= NoiseConfig.persistance;
			}
		}

		float halfScale = MapScale / 2f;
		for (int y = 0; y < MapScale; y++)
        {
            for (int x = 0; x < MapScale; x++)
            {
				float sampleX = x - halfScale;
				float sampleY = y - halfScale;
				float HeightSum = HeightMapData.terrainBaseHeight;
				for (int i = 0; i < HeightMapData.noiseLayers.Length; i++)
				{
					NoiseLayer NoiseConfig = HeightMapData.noiseLayers[i];
					if (!NoiseConfig.enabled){
						continue;
					}

					float NoiseHeight = GetNoiseValue(sampleX, sampleY, NoiseConfig, NoiseOctaves[i], NoiseConfig.noiseFunction);
					HeightSum += NoiseHeight * NoiseConfig.heightScalar;
				}
				NoiseMap[x, y] = HeightSum;
			}
        }
		return NoiseMap;
	}

    public static float GetNoiseValue(float SampleX, float SampleY, NoiseLayer NoiseConfig, NoiseOctave OctaveData, NoiseFunction NoiseFunction)
    {
		float NoiseValue = 0f;
		switch(NoiseFunction)
        {
			case NoiseFunction.Perlin:
				NoiseValue = ComputeFractalNoise2D(SampleX, SampleY, NoiseConfig, OctaveData);
				break;
			case NoiseFunction.Ridge:
				NoiseValue = ComputeRidgeNoise2D(SampleX, SampleY, NoiseConfig, OctaveData);
				break;
        }
		return NoiseValue;
    }

	public static float ComputeFractalNoise2D(float SampleX, float sampleY, NoiseLayer NoiseConfig, NoiseOctave OctaveData)
    {
		float PerlinValue = 0f;
		float Amplitude = 1f;
		float Frequency = 1f;
		for (int o = 0; o < NoiseConfig.octaves; o++)
		{
			float offsetSampleX = (SampleX + OctaveData.OctaveOffsets[o].x) / NoiseConfig.noiseScale * Frequency;
			float offsetSampleY = (sampleY + OctaveData.OctaveOffsets[o].y) / NoiseConfig.noiseScale * Frequency;

			float NoiseValue = Mathf.PerlinNoise(offsetSampleX, offsetSampleY) * 2 - 1;
			PerlinValue += NoiseValue * Amplitude;

			Amplitude *= NoiseConfig.persistance;
			Frequency *= NoiseConfig.lacunarity;
		}
		return PerlinValue;
    }

	public static float ComputeRidgeNoise2D(float SampleX, float SampleY, NoiseLayer NoiseConfig, NoiseOctave OctaveData)
    {
		float RidgeValue = 0f;
		float Amplitude = 1f;
		float Frequency = 1f;
		for (int o = 0; o < NoiseConfig.octaves; o++)
		{
			float offsetSampleX = (SampleX + OctaveData.OctaveOffsets[o].x) / NoiseConfig.noiseScale * Frequency;
			float offsetSampleY = (SampleY + OctaveData.OctaveOffsets[o].y) / NoiseConfig.noiseScale * Frequency;

			float NoiseValue = 1 - Mathf.Abs(Mathf.PerlinNoise(offsetSampleX, offsetSampleY));
			NoiseValue = Mathf.Pow(Mathf.Abs(NoiseValue), NoiseConfig.gain);
			RidgeValue += NoiseValue * Amplitude;

			Amplitude *= NoiseConfig.persistance;
			Frequency *= NoiseConfig.lacunarity;
		}
		return RidgeValue;
	}
}