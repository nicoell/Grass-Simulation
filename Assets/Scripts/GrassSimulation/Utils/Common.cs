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
		
		//YUV->RGB Colorspace conversion
		//from https://www.fourcc.org/fccyvrgb.php

		public static Vector3 RgbToYuv(Color rgb)
		{
			var y =  0.299f * rgb.r + 0.587f * rgb.g + 0.114f * rgb.b;
			var u = (rgb.b - y) * 0.585f;
			var v = (rgb.r - y) * 0.713f;
			return new Vector3(y,u,v);
		}
	}
}