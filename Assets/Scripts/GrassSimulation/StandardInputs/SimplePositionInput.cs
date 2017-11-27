using System;
using GrassSimulation.Core;
using GrassSimulation.Core.Inputs;
using UnityEngine;
using Random = System.Random;

namespace GrassSimulation.StandardInputs
{
	public class SimplePositionInput : PositionInput, IInitializableWithCtx
	{
		private Random _random;

		public bool Init(SimulationContext context)
		{
			_random = context.Random;
			return true;
		}

		public override Vector2 GetPosition(int id)
		{
			return new Vector2((float) _random.NextDouble(), (float) _random.NextDouble());
		}
	}
}