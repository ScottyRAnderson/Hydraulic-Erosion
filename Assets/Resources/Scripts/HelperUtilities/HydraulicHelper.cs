using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HydraulicHelper
{
    // Applies an erosion algorithm to a supplied HeightMap object
    // Implementation reference: https://www.firespark.de/resources/downloads/implementation%20of%20a%20methode%20for%20hydraulic%20erosion.pdf
    public static HeightMap GenerateErosionMap(HeightMap heightMap, Vector2[] dropletPositions, HydraulicData data)
    {
		int mapScale = heightMap.mapScale;
		Particle[] particles = new Particle[data.DropletCount];

        int[][] erosionIndicies;
		float[] erosionWeights;
        //InitializeErosionWeights(mapScale, data.ErosionRadius, out erosionIndicies, out erosionWeights);

		for (int i = 0; i < particles.Length; i++)
		{
            Particle particle = particles[i];

            // Initialize particle
            particle = new Particle();
            particle.Initialize(dropletPositions[i], data.Intertia, 1f, 1f, data.CarryCapacity);
			for (int lifetime = 0; lifetime < data.MaxSteps; lifetime++)
			{
				// Retrieve position of particle on node grid
				int nodeX = (int)particle.pos.x;
                int nodeY = (int)particle.pos.y;

				// If the particle is outside of the map, move onto the next particle
                if (nodeX + 1 >= mapScale || nodeY + 1 >= mapScale) {
                    break;
                }

				// Calculate droplet offset within the cell, (0,0) = at NW node, (1,1) = at SE node
                float cellOffsetX = particle.pos.x - nodeX;
                float cellOffsetY = particle.pos.y - nodeY;

                // Step 1: Compute bilinear interpolated gradient
                Vector2Int p00 = new Vector2Int(nodeX, nodeY);
                Vector2Int p01 = p00 + new Vector2Int(0, 1);
                Vector2Int p10 = p00 + new Vector2Int(1, 0);
                Vector2Int p11 = p00 + new Vector2Int(1, 1);
				
				float h00 = heightMap.heightValues[p00.x, p00.y];
                float h01 = heightMap.heightValues[p01.x, p01.y];
                float h10 = heightMap.heightValues[p10.x, p10.y];
                float h11 = heightMap.heightValues[p11.x, p11.y];

				// Bilinear interpolation to get height based gradient
                float gx = (h10 - h00) * (1 - cellOffsetY) + (h11 - h01) * cellOffsetY;
                float gy = (h01 - h00) * (1 - cellOffsetX) + (h11 - h10) * cellOffsetX;
				Vector2 g = new Vector2(gx, gy);

                // Step 2: Compute the droplets new direction based on the gradient
                Vector2 dir = particle.dir * data.Intertia - g * (1 - data.Intertia);
				if (dir.magnitude <= 0f) {
					dir = new Vector2(Random.Range(0f, 360f), Random.Range(0f, 360f));
				}
				dir.Normalize();
				particle.dir = dir;

                // Step 3: Compute the droplets new position based on it's direction
				particle.pos = particle.pos + particle.dir;
				nodeX = (int)particle.pos.x;
				nodeY = (int)particle.pos.y;
                if (particle.dir.magnitude == 0 || nodeX < 0 || nodeY < 0 || nodeX >= mapScale - 1 || nodeY >= mapScale - 1) {
                    break;
                }

                cellOffsetX = particle.pos.x - nodeX;
                cellOffsetY = particle.pos.y - nodeY;

                p00 = new Vector2Int(nodeX, nodeY);
                p01 = p00 + new Vector2Int(0, 1);
                p10 = p00 + new Vector2Int(1, 0);
                p11 = p00 + new Vector2Int(1, 1);

                /// Step 4: Height difference between the droplets old and new position is computed
                /// This is used to determine if the drop moved up or down,
                /// If the droplet moved up, deposit sediment at the droplets old position into the pit it supposedly ran through. (If the drop carries enough sediment, it fills the pit, otherwise it drops all sediment)
                /// If the droplet moved down, the droplets carry capacity is calculated by the height difference, velocity, water content and capacity parameter.
                /// c = max(-Hdif, PminSlope) * velocity * waterContent * Pcapacity;
				float h = heightMap.heightValues[nodeX, nodeY];
				float hDiff = h - h00;

                particle.carryCapacity = Mathf.Max(-hDiff, data.MinSlope) * particle.velocity * particle.waterContent * data.CarryCapacity;

                /// Step 5: If the droplet is carrying more sediment than it has capacity for, a percentage (defined by Pdeposition) of the surplus is deposited at its old position.
                /// Step 6: If the droplet is carrying less sediment than it has capacity for, it takes a percentage of its remaining capacity (defined by Perosion) from the map at its old position.
                ///	Importantly, the droplet never takes more sediment than the height difference to prevent holes.
                if (particle.IsOverloaded() || hDiff > 0) {
					// Carrying too much sediment therefore deposit a percentage of the surplace material
                    float droppedSurplus = (particle.sedimentContent - particle.carryCapacity) * data.DepositionRate;
					particle.sedimentContent -= droppedSurplus;

                    // Bilinear interpolated distribution across four surrounding node points
                    // Bilinear Interpolation (weight computation): https://en.wikipedia.org/wiki/Bilinear_interpolation
                    heightMap.heightValues[p00.x, p00.y] += droppedSurplus * cellOffsetX * cellOffsetY;
                    heightMap.heightValues[p01.x, p01.y] += droppedSurplus * (1 - cellOffsetX) * cellOffsetY;
                    heightMap.heightValues[p10.x, p10.y] += droppedSurplus * (1 - cellOffsetY) * cellOffsetX;
                    heightMap.heightValues[p11.x, p11.y] += droppedSurplus * (1 - cellOffsetX) * (1 - cellOffsetY);
				}
				else {
					/// Take a percentage of remaining sediment from the map
					/// Ensure eroded material isn't greater than the height difference to avoid pits 
					float erodedMaterial = Mathf.Min((particle.carryCapacity - particle.sedimentContent) * data.ErosionRate, -hDiff);
					particle.sedimentContent += erodedMaterial;

                    heightMap.heightValues[p00.x, p00.y] -= erodedMaterial / 4;
                    heightMap.heightValues[p01.x, p01.y] -= erodedMaterial / 4;
                    heightMap.heightValues[p10.x, p10.y] -= erodedMaterial / 4;
                    heightMap.heightValues[p11.x, p11.y] -= erodedMaterial / 4;

					/*
                    // Update affected grid points within Pradius
					int radius = data.ErosionRadius;
					float weightSum = 0f;
                    for (int x = -radius; x <= radius + 1; x++)
                    {
                        for (int y = -radius; y <= radius + 1; y++)
                        {
                            float dist = Mathf.Abs((float)x - 0.5f) + Mathf.Abs((float)y - 0.5f);
                            int coordX = p00.x + x;
                            int coordY = p00.y + y;

                            if (dist >= radius + radius ||
                                (coordX < 0 || coordY < 0 || coordX >= mapScale - 1 || coordY >= mapScale - 1)) {
								continue;
							}


                            float weight = 1 - dist / radius;
							weightSum += weight;
                        }
                    }

                    heightMap.heightValues[p00.x, p00.y] = 15.1f;
					heightMap.heightValues[p01.x, p01.y] = 15.1f;
					heightMap.heightValues[p10.x, p10.y] = 15.1f;
					heightMap.heightValues[p11.x, p11.y] = 15.1f;
					*/
				}

                // Step 7: Compute the droplets new velocity and water content
                particle.velocity = Mathf.Sqrt(Mathf.Pow(particle.velocity, 2f) + hDiff * data.Gravity);
				particle.waterContent = particle.waterContent * (1 - data.ErosionRate);

                // Step 8: Repeat steps 1 - 7 until the droplet moves out of the map or dies in a pit. A maximum step count should also be specified.

                // Step 9: If sediment is taken from the map and added to the droplet, all map points within Pradius are taken into account. (Pradius determines the area in which the drop erodes terrain)
            }
        }

        return heightMap;
    }

	/*
	public static void InitializeErosionWeights(int mapScale, int radius, out int[][] indicies, out float[] weights)
	{
        for (int i = 0; i < mapScale; i++)
		{
            float weightSum = 0f;
            for (int x = -radius; x <= radius + 1; x++)
            {
                for (int y = -radius; y <= radius + 1; y++)
                {
                    float dist = Mathf.Abs((float)x - 0.5f) + Mathf.Abs((float)y - 0.5f);
                    int coordX = p00.x + x;
                    int coordY = p00.y + y;

                    if (dist >= radius + radius || (coordX < 0 || coordY < 0 || coordX >= mapScale - 1 || coordY >= mapScale - 1)) {
                        continue;
                    }

                    float weight = 1 - dist / radius;
                    weightSum += weight;
                }
            }
		}
	}
	*/

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