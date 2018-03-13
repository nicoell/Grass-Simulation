using GrassSimulation.Core.Inputs;
using UnityEngine;

namespace GrassSimulation.StandardInputs
{
	public class TerrainHeightInput : HeightInput
	{
		[SerializeField]
		private Terrain _terrain;

		public override float GetHeight(float x, float y)
		{
			return _terrain.terrainData.GetInterpolatedHeight(x, y);
		}

		public override Vector2 GetSamplingRate()
		{
			return new Vector2(1.0f / (_terrain.terrainData.heightmapWidth - 1),
				1.0f / (_terrain.terrainData.heightmapHeight - 1));
		}
	}
}