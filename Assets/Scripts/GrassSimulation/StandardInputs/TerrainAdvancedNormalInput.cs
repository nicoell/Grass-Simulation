using GrassSimulation.Core;
using GrassSimulation.Core.Inputs;
using UnityEngine;
using Random = System.Random;

namespace GrassSimulation.StandardInputs
{
	public class TerrainAdvancedNormalInput : NormalInput, IIntializableWithCtx
	{
		private Random _random;
		[SerializeField]
		private Terrain _terrain;
		private Vector3 _upVector;

		public bool Init(SimulationContext context)
		{
			_random = context.Random;
			_upVector = context.Transform.up;
			return true;
		}

		public override Vector3 GetNormal(float x, float y)
		{
			return Vector3.Lerp(_terrain.terrainData.GetInterpolatedNormal(x, y), _upVector, (float) _random.NextDouble());
		}
	}
}