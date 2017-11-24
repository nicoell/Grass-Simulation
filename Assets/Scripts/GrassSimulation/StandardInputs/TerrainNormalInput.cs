using GrassSimulation.Core.Inputs;
using UnityEngine;

namespace GrassSimulation.StandardInputs {
	public class TerrainNormalInput : NormalInput
	{
		[SerializeField]
		private Terrain _terrain;
		
		public override Vector3 GetNormal(float x, float y) { return _terrain.terrainData.GetInterpolatedNormal(x, y); }
	}
}