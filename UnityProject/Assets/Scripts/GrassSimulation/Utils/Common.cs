using UnityEngine;

namespace GrassSimulation.Utils
{
	public static class Common
	{
		public static float Smoothstep(float from, float to, float t)
		{
			t = Mathf.Clamp01((t - from) / (to - from));
			return t * t * (3 - 2*t);
		}

		public static float Smootherstep(float from, float to, float t)
		{
			t = Mathf.Clamp01((t - from) / (to - from));
			return t * t * t * (t * (t * 6 - 15) + 10);
		}
	}
}