using UnityEngine;

public struct HeightMap
{
	public readonly float[,] heightValues;
	public readonly float maxHeight;
	public readonly float minHeight;

	public int mapScale { get { return heightValues.GetLength(0); } }

	public HeightMap(float[,] heightValues, bool computeMinMaxHeights = true)
	{
		this.heightValues = heightValues;
		if(computeMinMaxHeights)
        {
			float maxHeight = Mathf.NegativeInfinity;
			float minHeight = Mathf.Infinity;
			for (int i = 0; i < this.heightValues.GetLength(0); i++)
			{
				for (int j = 0; j < this.heightValues.GetLength(0); j++)
				{
					if (heightValues[i, j] > maxHeight){
						maxHeight = heightValues[i, j];
					}
					if (heightValues[i, j] < minHeight){
						minHeight = heightValues[i, j];
					}
				}
			}
			this.maxHeight = maxHeight;
			this.minHeight = minHeight;
		}
		else {
			maxHeight = 0f;
			minHeight = 0f;
		}
	}
}