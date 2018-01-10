using System;
using UnityEngine;

namespace GrassSimulation.Core.Wind
{
	public enum WindType : int
	{
		Whirlwind,
		WindBlast,
		GentleBreeze,
		ModerateBreeze
	}

	public struct WindForce
	{
		public float Radius;
		public Vector3 Position;
		public WindType Type;
		public float Strength;

		public static int GetSize() { return sizeof(float) * 5 + sizeof(int); }
	}

	public class ExternalForce : MonoBehaviour
	{
		public GrassSimulationController GrassSimulationController;
		private Bounds _bounds;
		public float Radius;
		public WindType Type;
		public float Strength;

		private void Start()
		{
			if (GrassSimulationController && GrassSimulationController.Context)
			{
				_bounds = new Bounds(transform.position, new Vector3(Radius, Radius, Radius) * 2);
			} else
			{
				enabled = false;
			}
		}

		private void Update()
		{
			_bounds.center = transform.position;
			_bounds.extents = new Vector3(Radius, Radius, Radius);
			if (_bounds.Intersects(GrassSimulationController.Context.PatchContainer.GetBounds()))
			{
				//GrassSimulationController.Context.WindFieldRenderer.RegisterWindForce(AsWindForce());
			}
		}

		public WindForce AsWindForce()
		{
			return new WindForce
			{
				Position = transform.position,
				Radius = Radius,
				Strength = Strength,
				Type = Type
			};
		}
	}
	
}