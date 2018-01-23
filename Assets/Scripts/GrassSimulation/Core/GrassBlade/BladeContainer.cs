using UnityEngine;

namespace GrassSimulation.Core.GrassBlade
{
	//TODO: Cap Blade Array Count to 256 values
	public class BladeContainer : ScriptableObject, IInitializableWithCtx
	{
		private const int TextureHeight = 64;
		private const int GrassTextureWidth = 128;
		private const int GrassTextureHeight = 1024;
		private const float SampleStep = 1f / TextureHeight;
		private SimulationContext _ctx;
		[SerializeField]
		public Blade[] Blades;

		public void Init(SimulationContext context)
		{
			_ctx = context;
		}

		private Color MultiSampleGradient(Gradient gradient, float sampleLoc, float sampleSize)
		{
			var output = new Color(0, 0, 0, 0);
			var count = 0;
			for (var l = sampleLoc - sampleSize; l < sampleLoc + sampleSize; l += SampleStep)
			{
				var t = Mathf.Clamp01(l + SampleStep / 2f);
				output += gradient.Evaluate(t);
				count++;
			}
			return output / count;
		}
		
		public Texture2DArray GetGeoemetryTexture2DArray(int id)
		{
			if (Blades == null || Blades.Length <= 0) return null;
			int width, height;
			switch (id)
			{
					case 0:
						width = 1;
						height = TextureHeight;
						break;
					default:
						width = GrassTextureWidth;
						height = GrassTextureHeight;
						break;
			}
			var tex2DArray = new Texture2DArray(width, height, Blades.Length,
				TextureFormat.RGBA32, true, true)
			{
				name = "BladeTextures",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
				anisoLevel = 0,
				mipMapBias = -0.5f
			};

			for (var i = 0; i < Blades.Length; i++)
			{
				int miplevel = 0, mipWidth, mipHeight;
				var blade = Blades[i];
				do
				{
					mipWidth = Mathf.Max(1, tex2DArray.width >> miplevel);
					mipHeight = Mathf.Max(1, tex2DArray.height >> miplevel);
					var samplingInterval = 1.0f / mipHeight;
					var uvCenter = new Vector2(0.5f / mipWidth, 0.5f / mipHeight);
					var colors = new Color[mipHeight * mipWidth];
					for (var y = 0; y < mipHeight; y++)
					for (var x = 0; x < mipWidth; x++)
					{
						//TODO: Add multisampling of animationcurve
						float r = 0, g = 0, b = 0, a = 0;
						switch (id) {
							case 0:
								var edgeCurve = blade.EdgeCurve.Evaluate((float) y / mipHeight) * blade.WidthModifier;
								edgeCurve = Mathf.SmoothStep(edgeCurve, blade.WidthModifier,
								miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);
								var midTranslation = blade.MidTranslation.Evaluate((float) y / mipHeight)  * blade.WidthModifier;
								midTranslation = Mathf.SmoothStep(midTranslation, 0.0f, miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);

								//var color = MultiSampleGradient(blade.ColorGradient, (float) y / mipHeight, samplingInterval);

								r = edgeCurve;
								g = midTranslation;
								b = blade.DiffuseReflectance;
								a = blade.Translucency;
								break;
							default:
								var uv = new Vector2((float) x / mipWidth + uvCenter.x, (float) y / mipHeight + uvCenter.y);
								var color = blade.GrassTexture.GetPixelBilinear(uv.x, uv.y);
								r = color.r;
								g = color.g;
								b = color.b;
								a = color.a;
								break;
						}

						var index = mipWidth * y + x;
						colors[index] = new Color(r, g, b, a);
					}
					tex2DArray.SetPixels(colors, i, miplevel);

					miplevel++;
				} while (mipHeight != 1 || mipWidth != 1);
			}
			tex2DArray.Apply(id != 0);

			return tex2DArray;
		}

		public byte GetTypeCount()
		{
			return (byte) Blades.Length;
		}
	}
}