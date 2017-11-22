using UnityEngine;

namespace GrassSimulation.DataProvider
{
	public class SimpleNormalProvider : NormalProvider
	{
		public override Vector3 GetNormal(float x, float y) { return Vector3.up;}
	}
}