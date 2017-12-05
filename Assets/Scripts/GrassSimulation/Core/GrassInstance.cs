using UnityEngine;

namespace GrassSimulation.Core
{
	public struct UvData
	{
		public Vector2 Position;
	}

	public class GrassInstance : ContextRequirement, IInitializable
	{
		public Texture2D GrassMapTexture;
		public Texture2D ParameterTexture;
		public ComputeBuffer UvBuffer;

		public GrassInstance(SimulationContext ctx) : base(ctx)
		{
		}

		public UvData[] UvData { get; private set; }

		public void Destroy()
		{
			UvBuffer.Release();
		}

		public bool Init()
		{
			//Create and fill UvData
			UvData = new UvData[Ctx.Settings.GetSharedBufferLength()];

			for (var i = 0; i < Ctx.Settings.GetSharedBufferLength(); i++)
				UvData[i].Position = Ctx.PositionInput.GetPosition(i);
			UvBuffer = new ComputeBuffer((int) Ctx.Settings.GetSharedBufferLength(), 2 * sizeof(float),
				ComputeBufferType.Default);
			UvBuffer.SetData(UvData);

			//Create and fill ParameterTexture
			ParameterTexture = new Texture2D(Ctx.Settings.GetSharedTextureWidthHeight(),
				Ctx.Settings.GetSharedTextureWidthHeight(),
				TextureFormat.RGBAFloat, false, true)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Repeat
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

			//Create and fill GrassMapTexture
			GrassMapTexture = new Texture2D(Ctx.Settings.GrassMapResolution,
				Ctx.Settings.GrassMapResolution,
				TextureFormat.RGBA32, false, true)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};

			var grassMapData = new Color[Ctx.Settings.GrassMapResolution * Ctx.Settings.GrassMapResolution];
			for (var i = 0; i < Ctx.Settings.GrassMapResolution * Ctx.Settings.GrassMapResolution; i++)
				grassMapData[i] = new Color(Ctx.GrassMapInput.GetGrassType(0, 0, 0) / 255f, 0, 0, 0);

			GrassMapTexture.SetPixels(grassMapData);
			GrassMapTexture.Apply();

			Ctx.GrassGeometry.SetBuffer("UvBuffer", UvBuffer);
			Ctx.GrassGeometry.SetTexture("ParameterTexture", ParameterTexture);
			Ctx.GrassGeometry.SetTexture("GrassMapTexture", GrassMapTexture);
			Ctx.GrassBillboardCrossed.SetBuffer("UvBuffer", UvBuffer);
			Ctx.GrassBillboardCrossed.SetTexture("ParameterTexture", ParameterTexture);
			Ctx.GrassBillboardCrossed.SetTexture("GrassMapTexture", GrassMapTexture);
			Ctx.GrassBillboardScreen.SetBuffer("UvBuffer", UvBuffer);
			Ctx.GrassBillboardScreen.SetTexture("ParameterTexture", ParameterTexture);
			Ctx.GrassBillboardScreen.SetTexture("GrassMapTexture", GrassMapTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "ParameterTexture", ParameterTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "ParameterTexture", ParameterTexture);

			return true;
		}
	}
}