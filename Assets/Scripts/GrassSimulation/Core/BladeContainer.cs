using UnityEngine;

namespace GrassSimulation.Core
{
	//TODO: Cap Blade Array Count to 256 values
	public class BladeContainer : ScriptableObject, IInitializableWithCtx
	{
		private SimulationContext _ctx;
		public Blade[] Blades;

		public bool Init(SimulationContext context)
		{
			_ctx = context;
			return true;
		}

		public Texture2DArray GetGeoemetryTexture2DArray(int id)
		{
			if (Blades == null || Blades.Length <= 0) return null;
			var tex2DArray = new Texture2DArray(2, 64, Blades.Length,
				TextureFormat.RGBA32, true, true)
			{
				name = "BladeTextures",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
				anisoLevel = 0,
				mipMapBias = -0.5f
			};
			
			//TODO: Custom mipmap filtering for better color preservation
			for (var i = 0; i < Blades.Length; i++)
			{
				int miplevel = 0, mipWidth, mipHeight, debug = 0;
				var blade = Blades[i];
				do
				{
					debug++;

					mipWidth = Mathf.Max(1, tex2DArray.width >> miplevel);
					mipHeight = Mathf.Max(1, tex2DArray.height >> miplevel);
					var samplingInterval = 1.0f / mipHeight;
					var colors = new Color[mipHeight * mipWidth];
					for (var y = 0; y < mipHeight; y++)
					{
						float r0 = 0, r1 = 0, g0 = 0, g1 = 0, b0 = 0, b1 = 0, a0 = 0, a1 = 0;
						switch (id)
						{
							case 0:
								var leftEdgeCurve = blade.LeftEdgeCurve.Evaluate((float) y / mipHeight);
								leftEdgeCurve = Mathf.SmoothStep(leftEdgeCurve, 1.0f,
									miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);
								var rightEdgeCurve = blade.RightEdgeCurve.Evaluate((float) y / mipHeight);
								rightEdgeCurve = Mathf.SmoothStep(rightEdgeCurve, 1.0f, miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);
								var leftColor = blade.LeftColorGradient.Evaluate((float) y / mipHeight);
								//TODO: Reenable maybe
								//var rightColor = blade.RightColorGradient.Evaluate((float) y / tex2DArray.height);
								var rightColor = leftColor;

								r0 = leftEdgeCurve;
								r1 = rightEdgeCurve;
								g0 = leftColor.r;
								g1 = rightColor.r;
								b0 = leftColor.g;
								b1 = rightColor.g;
								a0 = leftColor.b;
								a1 = rightColor.b;
								break;
							case 1:
								var leftEdgeRotation = blade.LeftEdgeRotation.Evaluate((float) y / mipHeight);
								leftEdgeRotation = Mathf.SmoothStep(leftEdgeRotation, 0.0f, miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);
								var rightEdgeRotation = blade.RightEdgeRotation.Evaluate((float) y / mipHeight);
								rightEdgeRotation = Mathf.SmoothStep(rightEdgeRotation, 0.0f, miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);

								r0 = leftEdgeRotation;
								r1 = rightEdgeRotation;
								break;
						}

						var index = mipWidth == 2 ? 2 * y : y;
						colors[index] = new Color(r0, g0, b0, a0);
						if (mipWidth == 2) colors[index + 1] = new Color(r1, g1, b1, a1);
					}
					tex2DArray.SetPixels(colors, i, miplevel);

					miplevel++;
				} while (mipHeight != 1 || mipWidth != 1);

			}
			tex2DArray.Apply(false);

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