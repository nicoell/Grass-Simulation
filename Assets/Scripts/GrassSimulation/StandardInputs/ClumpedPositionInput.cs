using System;
using GrassSimulation.Core;
using GrassSimulation.Core.Inputs;
using UnityEngine;
using Random = System.Random;

namespace GrassSimulation.StandardInputs
{
	public class ClumpedPositionInput: PositionInput, IInitializableWithCtx
	{
		private Random _random;
		
		private int _lastId;
		private Vector2 _curPos;
		
		[Range(1, 64)]
		public uint ClumpSize = 4;
		public float ClumpRadiusMin = 0.001f;
		public float ClumpRadiusMax = 0.001f;

		public void Init(SimulationContext context)
		{
			_lastId = -1;
			_random = context.Random;
		}

		public override Vector2 GetPosition(int id)
		{
			if (_lastId == -1 || id > _lastId + ClumpSize)
			{
				_lastId = id;
				_curPos = new Vector2((float) _random.NextDouble(), (float) _random.NextDouble());
			}
			var radius = ClumpRadiusMin + (float) _random.NextDouble() * (ClumpRadiusMax - ClumpRadiusMin);
			var angle = 2 * Mathf.PI * (float) _random.NextDouble();
			var offset = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
			return _curPos + offset;
		}

		public override uint GetRepetitionCount() { return ClumpSize; }
	}
}