using System.Collections;
using UnityEngine;

namespace GrassSimulation.Core
{
	//TODO: Cap Blade Array Count to 256 values
	public class BladeContainer : ScriptableObject
	{
		public Blade[] Blades;

		public Texture2DArray GetGeoemetryTexture2DArray(int id)
		{
			if (Blades == null || Blades.Length <= 0) return null;
			var tex2DArray = new Texture2DArray(2, 64, Blades.Length,
				TextureFormat.RGBAFloat, true, true)
			{
				name = "BladeTextures",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
				anisoLevel = 10
			};
			//TODO: Custom mipmap filtering
			for (var i = 0; i < Blades.Length; i++)
			{
				var colors = new Color[tex2DArray.height * 2];
				for (var y = 0; y < tex2DArray.height; y++)
				{
					var blade = Blades[i];
					switch (id)
					{
						case 0:
							var leftEdgeCurve = blade.LeftEdgeCurve.Evaluate((float) y / tex2DArray.height);
							var rightEdgeCurve = blade.RightEdgeCurve.Evaluate((float) y / tex2DArray.height);
							//var g0 = blade.LeftEdgeRotation.Evaluate((float) y / tex2DArray.height);
							//var g1 = blade.RightEdgeRotation.Evaluate((float) y / tex2DArray.height);
							var leftColor = blade.LeftColorGradient.Evaluate((float) y / tex2DArray.height);
							//var rightColor = blade.RightColorGradient.Evaluate((float) y / tex2DArray.height);
							//TODO: Reenable maybe
							var rightColor = leftColor;
							colors[2 * y] = new Color(leftEdgeCurve, leftColor.r, leftColor.g, leftColor.b);
							colors[2 * y + 1] = new Color(rightEdgeCurve, rightColor.r, rightColor.g, rightColor.b);
							break;
						case 1:
							var leftEdgeRotation = blade.LeftEdgeRotation.Evaluate((float) y / tex2DArray.height);
							var rightEdgeRotation = blade.RightEdgeRotation.Evaluate((float) y / tex2DArray.height);

							colors[2 * y] = new Color(leftEdgeRotation, 0, 0, 0);
							colors[2 * y + 1] = new Color(rightEdgeRotation, 0, 0, 0);
							break;
					}
				}
				tex2DArray.SetPixels(colors, i);
			}
			tex2DArray.Apply(true);

			return tex2DArray;
		}

		public Texture2DArray GetBillboardTexture2DArray()
		{
			return null;
		}

		public byte GetTypeCount()
		{
			return (byte) Blades.Length;
		}
	}
}