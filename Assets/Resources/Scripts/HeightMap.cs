using UnityEngine;

public struct HeightMap
{
	public readonly float[,] heightValues;
	public readonly float maxHeight;
	public readonly float minHeight;

	public int mapScale { get { return heightValues.GetLength(0); } }

	public HeightMap(float[,] HeightValues, bool ComputeMinMaxHeights = true)
	{
		this.heightValues = HeightValues;
		if(ComputeMinMaxHeights)
        {
			float maxHeight = Mathf.NegativeInfinity;
			float minHeight = Mathf.Infinity;
			for (int i = 0; i < this.heightValues.GetLength(0); i++)
			{
				for (int j = 0; j < this.heightValues.GetLength(0); j++)
				{
					if (HeightValues[i, j] > maxHeight){
						maxHeight = HeightValues[i, j];
					}
					if (HeightValues[i, j] < minHeight){
						minHeight = HeightValues[i, j];
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