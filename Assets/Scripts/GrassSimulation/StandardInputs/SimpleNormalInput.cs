using GrassSimulation.Core.Inputs;
using UnityEngine;

namespace GrassSimulation.StandardInputs
{
	public class SimpleNormalInput : NormalInput
	{
		public override Vector3 GetNormal(float x, float y) { return Vector3.up;}
	}
}