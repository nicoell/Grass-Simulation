using System;
using GrassSimulation.Core;
using GrassSimulation.Core.Inputs;

namespace GrassSimulation.StandardInputs
{
	public class RandomGrassMapInput : GrassMapInput, IInitializableWithCtx
	{
		private Random _random;
		private int _typeCount;

		public bool Init(SimulationContext context)
		{
			_random = context.Random;
			_typeCount = context.BladeContainer.GetTypeCount();
			return true;
		}

		public override byte GetGrassType(float x, float y, float z)
		{
			return (byte) (_random.NextDouble() * _typeCount);
		}
	}
}