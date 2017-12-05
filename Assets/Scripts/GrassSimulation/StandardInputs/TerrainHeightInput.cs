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
			if (Mathf.Approximately(x, 1f) || Mathf.Approximately(y, 1f)) return _terrain.terrainData.GetInterpolatedHeight(x, y) / _terrain.terrainData.size.y;
			int x0 = Mathf.FloorToInt(x * (_terrain.terrainData.heightmapWidth - 1)),
				y0 = Mathf.FloorToInt(y * (_terrain.terrainData.heightmapHeight - 1));
			float t0 = Mathf.SmoothStep(0f, 1f, x * (_terrain.terrainData.heightmapWidth - 1) - x0),
				t1 = Mathf.SmoothStep(0f, 1f, y * (_terrain.terrainData.heightmapHeight - 1) - y0);
			var h = _terrain.terrainData.GetHeights(x0, y0, 2, 2);
			return Mathf.LerpUnclamped(Mathf.LerpUnclamped(h[0, 0], h[0, 1], t0), Mathf.LerpUnclamped(h[1, 0], h[1, 1], t0), t1);
			
		}

		public override Vector2 GetSamplingRate()
		{
			return new Vector2(1.0f / (_terrain.terrainData.heightmapWidth - 1),
				1.0f / (_terrain.terrainData.heightmapHeight - 1));
		}
	}
}