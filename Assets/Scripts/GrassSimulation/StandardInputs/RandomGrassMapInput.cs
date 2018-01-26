using System;
using GrassSimulation.Core;
using GrassSimulation.Core.Inputs;

namespace GrassSimulation.StandardInputs
{
	public class RandomGrassMapInput : GrassMapInput, IInitializableWithCtx
	{
		private Random _random;
		private byte _typeCount;

		public void Init(SimulationContext context)
		{
			_random = context.Random;
			_typeCount = context.BladeContainer.GetTypeCount();
		}

		public override int GetGrassType(float x, float y, float z) { return (int) (_random.NextDouble() * _typeCount); }

		public override float GetDensity(float x, float y, float z) { return x * z; }

		public override float GetHeightModifier(float x, float y, float z) { return x * z; }
	}
}