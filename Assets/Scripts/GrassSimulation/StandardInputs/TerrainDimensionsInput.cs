using GrassSimulation.Core.Inputs;
using UnityEngine;

namespace GrassSimulation.StandardInputs
{
	public class TerrainDimensionsInput : DimensionsInput
	{
		[SerializeField]
		private Terrain _terrain;
		
		public override float GetWidth() { return _terrain.terrainData.size.x; }
		public override float GetDepth() { return _terrain.terrainData.size.z; }
		public override float GetHeight() { return _terrain.terrainData.size.y; }
		public override Bounds GetBounds() { return _terrain.terrainData.bounds; }
	}
}