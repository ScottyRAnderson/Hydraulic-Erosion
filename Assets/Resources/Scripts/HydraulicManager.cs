using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HydraulicManager : MonoBehaviour
{
    public int seed;
    public int mapDensityIndex;
    public HeightMapData heightMapData;

    private HeightMap HeightMap;

    public void GenerateTerrainBase(int seed = 0)
    {
        HeightMap = GenerateHeightMap(seed);
    }

    private HeightMap GenerateHeightMap(int seed = 0){
        return HydraulicHelper.GenerateTerrainHeightMap(mapDensityIndex, heightMapData, seed);
    }
}