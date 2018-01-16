using GrassSimulation.Core;
using GrassSimulation.Core.Inputs;
using UnityEngine;
using Random = System.Random;

namespace GrassSimulation.StandardInputs
{
	public class TextureGrassMapInput : GrassMapInput, IInitializableWithCtx
	{
		private Random _random;
		private byte _typeCount;
		public Texture2D GrassMap;

		public void Init(SimulationContext context)
		{
			_random = context.Random;
			_typeCount = context.BladeContainer.GetTypeCount();
		}

		public override byte GetGrassType(float x, float y, float z)
		{
			//return (byte) Mathf.Min(GrassMap.GetPixelBilinear(x, z).r * 255, _typeCount);
			return (byte) (_random.NextDouble() * _typeCount);
		}

		public override float GetDensity(float x, float y, float z) { return GrassMap.GetPixelBilinear(x, z).g; }
		public override float GetHeightModifier(float x, float y, float z) { return GrassMap.GetPixelBilinear(x, z).b; }
	}
}