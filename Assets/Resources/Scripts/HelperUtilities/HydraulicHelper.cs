using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HydraulicHelper
{
    // Applies an erosion algorithm to a supplied HeightMap object
    public static HeightMap GenerateErosionMap(HeightMap heightMap)
    {
		
		return heightMap;
    }

    public static HeightMap GenerateTerrainHeightMap(int vertsPerLine, float vertexSpacing, MapSize mapSize, HeightMapData HeightMapSettings, int Seed = 0)
	{
		float[,] HeightValues = NoiseHelper.GenerateNoiseMap(vertsPerLine, vertexSpacing, mapSize, HeightMapSettings, Seed);
		return new HeightMap(HeightValues);
	}

	public static HeightMap GenerateTerrainHeightMap(int vertsPerLine, HeightMapData HeightMapSettings, HydraulicManager.HeightMapPreviewMode previewMode, int Seed = 0)
	{
		float[,] HeightValues = NoiseHelper.GenerateNoiseMap(vertsPerLine, 1, MapSize._64, HeightMapSettings, Seed, previewMode == HydraulicManager.HeightMapPreviewMode.BaseNoise);
		return new HeightMap(HeightValues);
	}

	public static Texture2D TextureFromHeightMap(HeightMap heightMap)
	{
		int mapScale = heightMap.heightValues.GetLength(0);
		Color[] colourMap = new Color[mapScale * mapScale];
		for (int y = 0; y < mapScale; y++)
		{
			for (int x = 0; x < mapScale; x++){
				colourMap[y * mapScale + x] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(heightMap.minHeight, heightMap.maxHeight, heightMap.heightValues[x, y]));
			}
		}
		return TextureFromColourMap(colourMap, mapScale);
	}

	public static Texture2D TextureFromColourMap(Color[] colourMap, int mapScale)
	{
		Texture2D texture = new Texture2D(mapScale, mapScale);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(colourMap);
		texture.Apply();
		return texture;
	}

	public static Texture2D ResizeTexture(Texture2D texture, int scaleX, int scaleY)
	{
		RenderTexture renderTexture = new RenderTexture(scaleX, scaleY, 24);
		RenderTexture cachedRendTex = RenderTexture.active;
		RenderTexture.active = renderTexture;
		Graphics.Blit(texture, renderTexture);
		Texture2D scaledTexture = new Texture2D(scaleX, scaleY);
		scaledTexture.ReadPixels(new Rect(0, 0, scaleX, scaleY), 0, 0);
		scaledTexture.Apply();
		RenderTexture.active = cachedRendTex;
		return scaledTexture;
	}
}