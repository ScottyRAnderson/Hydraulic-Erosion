using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HydraulicHelper
{
    public static HeightMap GenerateTerrainHeightMap(int MapScale, HeightMapData HeightMapSettings, int Seed = 0)
	{
		float[,] HeightValues = NoiseHelper.GenerateTerrainMap(MapScale, HeightMapSettings, Seed);
		return new HeightMap(HeightValues);
	}
}