using UnityEngine;

namespace GrassSimulation.Core
{
	public struct UvData
	{
		public Vector2 Position;
	}

	public class GrassInstance : ContextRequirement, IInitializable, IDestroyable
	{
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
				UvData[i].Position = new Vector2((float) Ctx.Random.NextDouble(), (float) Ctx.Random.NextDouble());
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

			Ctx.GrassGeometry.SetBuffer("UvBuffer", UvBuffer);
			Ctx.GrassGeometry.SetTexture("ParameterTexture", ParameterTexture);
			Ctx.GrassBillboardCrossed.SetBuffer("UvBuffer", UvBuffer);
			Ctx.GrassBillboardCrossed.SetTexture("ParameterTexture", ParameterTexture);
			Ctx.GrassBillboardScreen.SetBuffer("UvBuffer", UvBuffer);
			Ctx.GrassBillboardScreen.SetTexture("ParameterTexture", ParameterTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "ParameterTexture", ParameterTexture);
			//Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelCulling, "ParameterTexture", ParameterTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "ParameterTexture", ParameterTexture);

			return true;
		}
	}
}