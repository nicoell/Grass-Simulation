using UnityEngine;

namespace GrassSimulation.Core.Lod
{
	public struct UvData
	{
		public Vector2 Position;
	}

	public class GrassInstance : ContextRequirement
	{
		public Texture2D GrassMapTexture;
		public Texture2D NormalHeightTexture;
		public Texture2D ParameterTexture;
		public ComputeBuffer UvBuffer;

		public GrassInstance(SimulationContext ctx) : base(ctx)
		{
			//Create and fill UvData
			UvData = new UvData[Ctx.Settings.GetSharedBufferLength()];

			for (var i = 0; i < Ctx.Settings.GetSharedBufferLength(); i++) UvData[i].Position = Ctx.PositionInput.GetPosition(i);
			UvBuffer = new ComputeBuffer((int) Ctx.Settings.GetSharedBufferLength(), 2 * sizeof(float),
				ComputeBufferType.Default);
			UvBuffer.SetData(UvData);

			//Create and fill ParameterTexture
			ParameterTexture = new Texture2D(Ctx.Settings.GetSharedTextureWidthHeight(),
				Ctx.Settings.GetSharedTextureWidthHeight(),
				TextureFormat.RGBAFloat, false, true)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Mirror
			};
			var parameterData = new Color[Ctx.Settings.GetSharedTextureLength()];
			for (var i = 0; i < Ctx.Settings.GetSharedTextureLength(); i++)
				parameterData[i] = new Color(
					(float) (Ctx.Settings.BladeMinWidth +
					         Ctx.Random.NextDouble() * (Ctx.Settings.BladeMaxWidth - Ctx.Settings.BladeMinWidth)),
					(float) (Ctx.Settings.BladeMinBend +
					         Ctx.Random.NextDouble() * (Ctx.Settings.BladeMaxBend - Ctx.Settings.BladeMinBend)),
					(float) (Ctx.Settings.BladeMinHeight +
					         Ctx.Random.NextDouble() * (Ctx.Settings.BladeMaxHeight - Ctx.Settings.BladeMinHeight)),
					(float) (Ctx.Random.NextDouble() * Mathf.PI * 2f));

			ParameterTexture.SetPixels(parameterData);
			ParameterTexture.Apply();

			//Create and fill NormalHeightTexture
			//Float texture to save final height to reduce errors
			NormalHeightTexture = new Texture2D(Ctx.Settings.GrassMapResolution,
				Ctx.Settings.GrassMapResolution,
				TextureFormat.RGBAFloat, false, true)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			
			var normalHeightData = new Color[Ctx.Settings.GrassMapResolution * Ctx.Settings.GrassMapResolution];
			var uvGlobal = new Vector2(0, 0);
			var pixelCenter = new Vector2(0.5f / Ctx.Settings.GrassMapResolution , 0.5f / Ctx.Settings.GrassMapResolution); 
			
			for (var y = 0; y < Ctx.Settings.GrassMapResolution; y++)
			for (var x = 0; x < Ctx.Settings.GrassMapResolution; x++)
			{
				var i = y * Ctx.Settings.GrassMapResolution + x;
				uvGlobal.x = (float) x / Ctx.Settings.GrassMapResolution;
				uvGlobal.y = (float) y / Ctx.Settings.GrassMapResolution;
				uvGlobal += pixelCenter;
				
				var posY = Ctx.HeightInput.GetHeight(uvGlobal.x, uvGlobal.y);
				var up = Ctx.NormalInput.GetNormal(uvGlobal.x, uvGlobal.y);

				normalHeightData[i] = new Color(up.x, up.y, up.z, posY);
			}
			NormalHeightTexture.SetPixels(normalHeightData);
			NormalHeightTexture.Apply();
			
			//Create and fill GrassMapTexture
			GrassMapTexture = new Texture2D(Ctx.Settings.GrassMapResolution,
				Ctx.Settings.GrassMapResolution,
				TextureFormat.RGBA32, false, true)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};

			var grassMapData = new Color[Ctx.Settings.GrassMapResolution * Ctx.Settings.GrassMapResolution];
			for (var y = 0; y < Ctx.Settings.GrassMapResolution; y++)
			for (var x = 0; x < Ctx.Settings.GrassMapResolution; x++)
			{
				var i = y * Ctx.Settings.GrassMapResolution + x;
				var pos = new Vector3((float) x / Ctx.Settings.GrassMapResolution, 0, (float) y / Ctx.Settings.GrassMapResolution);
				pos.z = 1 - pos.z;
				pos.y = Ctx.HeightInput.GetHeight(pos.x, pos.z);
				grassMapData[i] = new Color(Ctx.GrassMapInput.GetGrassType(pos.x, pos.y, pos.z) / 255f,
					Ctx.GrassMapInput.GetDensity(pos.x, pos.y, pos.z), Ctx.GrassMapInput.GetHeightModifier(pos.x, pos.y, pos.z), 0);
			}

			GrassMapTexture.SetPixels(grassMapData);
			GrassMapTexture.Apply();

			Ctx.GrassGeometry.SetBuffer("UvBuffer", UvBuffer);
			Ctx.GrassGeometry.SetTexture("ParameterTexture", ParameterTexture);
			Ctx.GrassGeometry.SetTexture("GrassMapTexture", GrassMapTexture);
			Ctx.GrassGeometry.SetTexture("NormalHeightTexture", NormalHeightTexture);
			Ctx.GrassBillboardGeneration.SetBuffer("UvBuffer", UvBuffer);
			Ctx.GrassBillboardGeneration.SetTexture("ParameterTexture", ParameterTexture);
			Ctx.GrassBillboardGeneration.SetTexture("GrassMapTexture", GrassMapTexture);
			Ctx.GrassBillboardGeneration.SetTexture("NormalHeightTexture", NormalHeightTexture);
			Ctx.GrassBillboardCrossed.SetBuffer("UvBuffer", UvBuffer);
			Ctx.GrassBillboardCrossed.SetTexture("ParameterTexture", ParameterTexture);
			Ctx.GrassBillboardCrossed.SetTexture("GrassMapTexture", GrassMapTexture);
			Ctx.GrassBillboardCrossed.SetTexture("NormalHeightTexture", NormalHeightTexture);
			Ctx.GrassBillboardScreen.SetBuffer("UvBuffer", UvBuffer);
			Ctx.GrassBillboardScreen.SetTexture("ParameterTexture", ParameterTexture);
			Ctx.GrassBillboardScreen.SetTexture("GrassMapTexture", GrassMapTexture);
			Ctx.GrassBillboardScreen.SetTexture("NormalHeightTexture", NormalHeightTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "ParameterTexture", ParameterTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "GrassMapTexture", GrassMapTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "ParameterTexture", ParameterTexture);

			// !CAUTION! NormalHeightTexture WILL BE OVERWRITTEN BY BILLBOARD GENERATION
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "NormalHeightTexture", NormalHeightTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "NormalHeightTexture", NormalHeightTexture);
		}

		public UvData[] UvData { get; private set; }

		public void Destroy() { UvBuffer.Release(); }
	}
}