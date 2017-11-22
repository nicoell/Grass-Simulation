using UnityEngine;

namespace GrassSimulation.DataProvider {
	public class TerrainNormalProvider : NormalProvider
	{
		[SerializeField]
		private Terrain _terrain;
		
		public override Vector3 GetNormal(float x, float y) { return _terrain.terrainData.GetInterpolatedNormal(x, y); }
	}
}