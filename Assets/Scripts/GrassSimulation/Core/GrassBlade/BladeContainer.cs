using System;
using System.Linq;
using UnityEngine;

namespace GrassSimulation.Core.GrassBlade
{
//TODO: Cap Blade Array Count to 256 values
	public class BladeContainer : ScriptableObject, IInitializableWithCtx
	{
		private const int TextureHeight = 64;
		private const int GrassTextureWidth = 128;
		private const int GrassTextureHeight = 1024;
		private const int BlossomTextureWidth = 512;
		private const int BlossomTextureHeight = 512;
		private const float SampleStep = 1f / TextureHeight;
		private SimulationContext _ctx;
		[SerializeField]
		public Blade[] Blades;
		protected float[] BladeDistribution;

		public void Init(SimulationContext context)
		{
			_ctx = context;
			BladeDistribution = GetBladeDistribution();
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

		public static float MultiSampleAnimationCurve(AnimationCurve curve, float sampleLoc, float sampleSize)
		{
			var output = 0.0f;
			var count = 0;
			return curve.Evaluate(sampleLoc);
			if (sampleLoc == 0) return curve.Evaluate(0);
			if (sampleLoc == 1) return curve.Evaluate(1f);
			for (var l = sampleLoc - sampleSize; l < sampleLoc + sampleSize; l += SampleStep)
			{
				var t = Mathf.Clamp01(l + SampleStep / 2f);
				output += curve.Evaluate(t);
				count++;
			}
			return output / count;
		}

		public float[] GetBladeDistribution()
		{
			if (Blades == null || Blades.Length <= 0) return null;
			float accumulatedProbability = Blades.Sum(t => t.Probability);
			var bladeDistribution = new float[Blades.Length];
			bladeDistribution[0] = Blades[0].Probability / accumulatedProbability;
			for (int i = 1; i < Blades.Length; i++)
			{
				bladeDistribution[i] = Blades[i].Probability / accumulatedProbability + bladeDistribution[i-1];
			}
			return bladeDistribution;
		}

		/// <summary>
		///     <para>Gets the type of grass for a given random number in range 0..1</para>
		/// </summary>
		/// <returns>The index corresponding to the array position in the GrassContainer representing the grass type.</returns> 
		public int GetGrassTypeByDistribution(float x)
		{
			int i = Array.BinarySearch(BladeDistribution, x);
			if (i >= 0) return i;
			i = ~i;
			return i;
		}
		
		public int GetBlossomCount()
		{
			return Blades.TakeWhile(t => t.HasBlossom).Count();
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
				case 1:
					width = 1;
					height = TextureHeight;
					break;
				case 2:
					width = GrassTextureWidth;
					height = GrassTextureHeight;
					break;
				case 3:
					width = BlossomTextureWidth;
					height = BlossomTextureWidth;
					break;
				default:
					width = GrassTextureWidth;
					height = GrassTextureHeight;
					break;
			}

			var texDepth = 0;
			if (id == 0 || id == 2) texDepth = Blades.Length;
			else texDepth = GetBlossomCount();
			if (texDepth == 0) return null;

			var tex2DArray = new Texture2DArray(width, height, texDepth,
				id == 1 ? TextureFormat.RGBAFloat : TextureFormat.RGBA32, true, true)
			{
				name = "BladeTextures",
				wrapMode = id == 1 ? TextureWrapMode.Mirror : TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
				anisoLevel = 8
				
				//mipMapBias = -0.5f
			};

			for (var i = 0; i < texDepth; i++)
			{
				int miplevel = 0, mipWidth, mipHeight;
				float blossomBetaAverage = 0, blossomGammaAverage = 0, blossomDeltaAverage = 0;
				var blade = Blades[i];
				
				if (id == 1)
				{
					blossomBetaAverage = MultiSampleAnimationCurve(blade.BlossomBeta, 0.5f, 0.5f);
					blossomGammaAverage = MultiSampleAnimationCurve(blade.BlossomGamma, 0.5f, 0.5f);
					blossomDeltaAverage = MultiSampleAnimationCurve(blade.BlossomDelta, 0.5f, 0.5f);
				}
				
				do
				{
					mipWidth = Mathf.Max(1, tex2DArray.width >> miplevel);
					mipHeight = Mathf.Max(1, tex2DArray.height >> miplevel);
					var samplingInterval = 1f / mipHeight;

					var uvCenter = new Vector2(0.5f / mipWidth, 0.5f / mipHeight);
					var colors = new Color[mipHeight * mipWidth];
					for (var y = 0; y < mipHeight; y++)
					for (var x = 0; x < mipWidth; x++)
					{
//TODO: Add multisampling of animationcurve
						float r = 0, g = 0, b = 0, a = 0;
						float sampleLoc;
						switch (id)
						{
							case 0: //GrassBlade Data
								sampleLoc = mipHeight == 1 ? 0.5f : (float) y / (mipHeight - 1);
								
								var edgeCurve = blade.EdgeCurve.Evaluate(sampleLoc) * blade.WidthModifier;
								edgeCurve = Mathf.SmoothStep(edgeCurve, blade.WidthModifier,
									miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);
								var midTranslation = blade.MidTranslation.Evaluate(sampleLoc) * blade.WidthModifier;
								midTranslation = Mathf.SmoothStep(midTranslation, 0.0f, miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);


								r = edgeCurve;
								g = midTranslation;
								b = blade.DiffuseReflectance;
								a = blade.Translucency;
								break;
							case 1: //Blossom Data
								/*var blossomBeta = blade.BlossomBeta.Evaluate((float) y / mipHeight);
								blossomBeta = Mathf.SmoothStep(blossomBeta, 1,
									miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);
								var blossomGamma = blade.BlossomGamma.Evaluate((float) y / mipHeight);
								var blossomDelta = blade.BlossomDelta.Evaluate((float) y / mipHeight);*/

								sampleLoc = mipHeight == 1 ? 0.5f : (float) y / (mipHeight - 1);
								
								var blossomBeta = MultiSampleAnimationCurve(blade.BlossomBeta, sampleLoc, samplingInterval);
								blossomBeta = Mathf.SmoothStep(blossomBeta, 1, miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);
								var blossomGamma = MultiSampleAnimationCurve(blade.BlossomGamma, sampleLoc, samplingInterval);
								//blossomGamma = Mathf.SmoothStep(blossomGamma, blossomGammaAverage, miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);
								var blossomDelta = MultiSampleAnimationCurve(blade.BlossomDelta, sampleLoc, samplingInterval);
								//blossomDelta = Mathf.SmoothStep(blossomDelta, blossomDeltaAverage, miplevel / _ctx.Settings.BladeTextureMaxMipmapLevel);
								
								r = blossomBeta;
								g = blossomGamma;
								b = blossomDelta;
								a = blade.BlossomDiffuseReflectance;
								break;
							case 2: //GrassBlade Texture
								var uv = new Vector2((float) x / mipWidth + uvCenter.x, (float) y / mipHeight + uvCenter.y);
								var color = blade.GrassTexture.GetPixelBilinear(uv.x, uv.y);
								r = color.r;
								g = color.g;
								b = color.b;
								a = color.a;
								break;
							case 3: //Blossom Texture
								var uvBlossom = new Vector2((float) x / mipWidth + uvCenter.x, (float) y / mipHeight + uvCenter.y);
								var colorBlossom = blade.BlossomTexture.GetPixelBilinear(uvBlossom.x, uvBlossom.y);
								r = colorBlossom.r;
								g = colorBlossom.g;
								b = colorBlossom.b;
								a = colorBlossom.a;
								break;
							default: //GrassBlade Texture
								break;
						}

						var index = mipWidth * y + x;
						colors[index] = new Color(r, g, b, a);
					}
					tex2DArray.SetPixels(colors, i, miplevel);

					miplevel++;
				} while (mipHeight != 1 || mipWidth != 1);
			}
			tex2DArray.Apply(id == 2 || id == 3);

			return tex2DArray;
		}

		public byte GetTypeCount() { return (byte) Blades.Length; }
	}
}