using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public static class HydraulicHelper
{
    // Applies an erosion algorithm to a supplied HeightMap object
    // Implementation reference: https://www.firespark.de/resources/downloads/implementation%20of%20a%20methode%20for%20hydraulic%20erosion.pdf
    public static HeightMap GenerateErosionMap(HeightMap heightMap, Vector2[] dropletPositions, HydraulicData data)
    {
		int mapScale = heightMap.mapScale;
		for (int i = 0; i < data.DropletCount; i++)
		{
            // Initialize particle
            Particle particle = new Particle();
            particle.Initialize(dropletPositions[i], 1f, 1f, 0f, data.CarryCapacity);
			for (int lifetime = 0; lifetime < data.MaxSteps; lifetime++)
			{
				// Retrieve position of particle on node grid
				int nodeX = (int)particle.pos.x;
                int nodeY = (int)particle.pos.y;

				// Calculate droplet offset within the cell, (0,0) = at NW node, (1,1) = at SE node
                float cellOffsetX = particle.pos.x - nodeX;
                float cellOffsetY = particle.pos.y - nodeY;

                // Step 1: Compute bilinear interpolated gradient
                Vector2Int p00 = new Vector2Int(nodeX, nodeY);
                Vector2Int p10 = p00 + new Vector2Int(1, 0);
                Vector2Int p01 = p00 + new Vector2Int(0, 1);
                Vector2Int p11 = p00 + new Vector2Int(1, 1);

                Vector3 gh = GetMapGradientHeightData(heightMap, particle.pos);
                Vector2 g = new Vector2(gh.x, gh.y);
                float h = gh.z;

                // Step 2: Compute the droplets new direction based on the gradient
                Vector2 dir = particle.dir * data.Inertia - g * (1 - data.Inertia);
				if (dir.magnitude <= 0f) {
					dir = new Vector2(Random.Range(0f, 360f), Random.Range(0f, 360f));
				}
				dir.Normalize();
				particle.dir = dir;

                // Step 3: Compute the droplets new position based on it's direction
				particle.pos += particle.dir;

                // If the particle is outside of the map, move onto the next particle
                if (!ValidPos((int)particle.pos.x, (int)particle.pos.y, mapScale)) {
                    break;
                }

                /// Step 4: Height difference between the droplets old and new position is computed
                /// This is used to determine if the drop moved up or down,
                /// If the droplet moved up, deposit sediment at the droplets old position into the pit it supposedly ran through. (If the drop carries enough sediment, it fills the pit, otherwise it drops all sediment)
                /// If the droplet moved down, the droplets carry capacity is calculated by the height difference, velocity, water content and capacity parameter.
                /// c = max(-Hdif, PminSlope) * velocity * waterContent * Pcapacity;
                gh = GetMapGradientHeightData(heightMap, particle.pos);
                float hDiff = gh.z - h;

                // Calculate new carry capacity
                particle.carryCapacity = Mathf.Max(-hDiff, data.MinSlope) * particle.velocity * particle.waterContent * data.CarryCapacity;

                /// Step 5: If the droplet is carrying more sediment than it has capacity for, a percentage (defined by Pdeposition) of the surplus is deposited at its old position.
                /// Step 6: If the droplet is carrying less sediment than it has capacity for, it takes a percentage of its remaining capacity (defined by Perosion) from the map at its old position.
                ///	Importantly, the droplet never takes more sediment than the height difference to prevent holes.
                if (particle.IsOverloaded() || hDiff > 0) {
					// Carrying too much sediment therefore deposit a percentage of the surplace material
                    // ... or if we have gone up hill we need to deposit our sediment
                    float droppedSurplus = hDiff > 0 ? Mathf.Min(hDiff, particle.sedimentContent) : (particle.sedimentContent - particle.carryCapacity) * data.DepositionRate;
                    particle.sedimentContent -= droppedSurplus;

                    // Bilinear interpolated distribution across four surrounding node points
                    // Bilinear Interpolation (weight computation): https://en.wikipedia.org/wiki/Bilinear_interpolation
                    heightMap.heightValues[p00.x, p00.y] += droppedSurplus * (1 - cellOffsetX) * (1 - cellOffsetY);
                    heightMap.heightValues[p10.x, p10.y] += droppedSurplus * (1 - cellOffsetY) * cellOffsetX;
                    heightMap.heightValues[p01.x, p01.y] += droppedSurplus * (1 - cellOffsetX) * cellOffsetY;
                    heightMap.heightValues[p11.x, p11.y] += droppedSurplus * cellOffsetX * cellOffsetY;
                }
                else {

                    /// Take a percentage of remaining sediment from the map
                    /// Ensure eroded material isn't greater than the height difference to avoid pits 
                    float erodedMaterial = Mathf.Min((particle.carryCapacity - particle.sedimentContent) * data.ErosionRate, -hDiff);
					particle.sedimentContent += erodedMaterial;

                    // Update affected grid points within Pradius
                    List<Vector2Int> erosionPoints = new List<Vector2Int>();
                    List<float> erosionWeights = new List<float>();
                    float weightSum = 0f;

                    int radius = data.ErosionRadius;
                    for (int x = p00.x - radius; x < p00.x + 2 + radius; x++)
                    {
                        for (int y = p00.y - radius; y < p00.y + 2 + radius; y++)
                        {
                            Vector2Int point = new Vector2Int(x, y);
                            float dist = Vector2.Distance(particle.pos, point);
                            if (ValidPos(point.x, point.y, mapScale) && dist < data.ErosionRadius) {
                                erosionPoints.Add(point);

                                // Compute point distance weight
                                float weight = Mathf.Max(0f, data.ErosionRadius - (point - particle.pos).magnitude);
                                weightSum += weight;
                                erosionWeights.Add(weight);
                            }
                        }
                    }

                    for (int p = 0; p < erosionPoints.Count; p++)
                    {
                        Vector2Int point = erosionPoints[p];
                        float weight = erosionWeights[p] / weightSum; // If an erosion factor is required, it should be applied here
                        heightMap.heightValues[point.x, point.y] -= erodedMaterial * weight;
                    }
                }

                // Step 7: Compute the droplets new velocity and water content
                particle.velocity = Mathf.Sqrt(Mathf.Pow(particle.velocity, 2f) + hDiff * data.Gravity);
				particle.waterContent *= 1 - data.ErosionRate;

                // Step 8: Repeat steps 1 - 7 until the droplet moves out of the map or dies in a pit. A maximum step count should also be specified.
            }
        }

        return heightMap;
    }

    /// <summary>
    /// Computes the bilinear computed gradient and height at a given particle position on a heightmap.
    /// </summary>
    /// <param name="heightMap"></param>
    /// <param name="pos"></param>
    /// <returns>
    /// A vector containing: 
    /// <list type="bullet">
    /// <item><description>x: gradient (dx)</description></item>
    /// <item><description>y: gradient (dy)</description></item>
    /// <item><description>z: height</description></item>
    /// </list>
    /// </returns>
    public static Vector3 GetMapGradientHeightData(HeightMap heightMap, Vector2 pos) {
        // Retrieve position of particle on node grid
        int nodeX = (int)pos.x;
        int nodeY = (int)pos.y;

        // Calculate droplet offset within the cell, (0,0) = at NW node, (1,1) = at SE node
        float cellOffsetX = pos.x - nodeX;
        float cellOffsetY = pos.y - nodeY;

        // Step 1: Compute bilinear interpolated gradient
        Vector2Int p00 = new Vector2Int(nodeX, nodeY);
        Vector2Int p10 = p00 + new Vector2Int(1, 0);
        Vector2Int p01 = p00 + new Vector2Int(0, 1);
        Vector2Int p11 = p00 + new Vector2Int(1, 1);

        float h00 = heightMap.heightValues[p00.x, p00.y];
        float h10 = heightMap.heightValues[p10.x, p10.y];
        float h01 = heightMap.heightValues[p01.x, p01.y];
        float h11 = heightMap.heightValues[p11.x, p11.y];

        // Bilinear interpolation to get height based gradient
        float gx = (h10 - h00) * (1 - cellOffsetY) + (h11 - h01) * cellOffsetY;
        float gy = (h01 - h00) * (1 - cellOffsetX) + (h11 - h10) * cellOffsetX;

        // Bilinear interpolation to get height of point on map
        float height = h00 * (1 - cellOffsetX) * (1 - cellOffsetY) + h10 * cellOffsetX * (1 - cellOffsetY) + h01 * (1 - cellOffsetX) * cellOffsetY + h11 * cellOffsetX * cellOffsetY;

        // Pack results into vector
        return new Vector3(gx, gy, height);
    }

    public static bool ValidPos(int x, int y, int scale) {
        return x >= 0 && y >= 0 && x < scale - 1 && y < scale - 1;
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