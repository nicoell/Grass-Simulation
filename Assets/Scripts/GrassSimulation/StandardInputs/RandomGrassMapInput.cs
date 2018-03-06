using System;
using GrassSimulation.Core;
using GrassSimulation.Core.Inputs;

namespace GrassSimulation.StandardInputs
{
	public class RandomGrassMapInput : GrassMapInput, IInitializableWithCtx
	{
		private Random _random;
		private SimulationContext _context;
		private byte _typeCount;

		public void Init(SimulationContext context)
		{
			_context = context;
			_random = context.Random;
			_typeCount = context.BladeContainer.GetTypeCount();
		}

		public override int GetGrassType(float x, float y, float z) { return _context.BladeContainer.GetGrassTypeByDistribution((float) _random.NextDouble()); }

		public override float GetDensity(float x, float y, float z) { return 1f; }

		public override float GetHeightModifier(float x, float y, float z) { return 1f; }
	}
}