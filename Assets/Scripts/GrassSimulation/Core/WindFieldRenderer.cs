using UnityEngine;

namespace GrassSimulation.Core
{
	public class WindFieldRenderer : ContextRequirement
	{
		public readonly RenderTexture[] WindDensityTexture;
		public readonly RenderTexture[] WindFieldTexture;
		private int _textureIndex = 1;

		public WindFieldRenderer(SimulationContext ctx, Bounds bounds) : base(ctx)
		{
			WindDensityTexture = new RenderTexture[2];
			WindFieldTexture = new RenderTexture[2];

			for (var i = 0; i < 2; i++)
			{
				WindDensityTexture[i] = new RenderTexture(Ctx.Settings.WindDensityResolution,
					Ctx.Settings.WindDensityResolution, 0,
					RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
				{
					wrapMode = TextureWrapMode.Clamp,
					enableRandomWrite = true,
					depth = 0,
					filterMode = FilterMode.Bilinear
				};
				WindDensityTexture[i].Create();

				WindFieldTexture[i] = new RenderTexture(Ctx.Settings.WindFieldResolution,
					Ctx.Settings.WindFieldResolution, 0,
					RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
				{
					wrapMode = TextureWrapMode.Clamp,
					enableRandomWrite = true,
					depth = 0,
					filterMode = FilterMode.Bilinear
				};
				WindFieldTexture[i].Create();
			}
			
			Ctx.WindFluidSimulation.SetFloat("WindDensityResolution", Ctx.Settings.WindDensityResolution);
			Ctx.WindFluidSimulation.SetFloat("WindFieldResolution", Ctx.Settings.WindFieldResolution);
			Ctx.WindFluidSimulation.SetFloat("DensityStep", 1f / Ctx.Settings.WindDensityResolution);
			Ctx.WindFluidSimulation.SetFloat("FieldStep", 1f / Ctx.Settings.WindFieldResolution);
		}

		public void Update()
		{
			for (var i = 0; i < Ctx.Settings.WindFluidIterationSteps; i++)
			{
				UpdateWindField(_textureIndex, (_textureIndex + 1) % 2);
				//UpdateWindField(0, 1);
				_textureIndex = (_textureIndex + 1) % 2;
			}
		}

		private void UpdateWindField(int read, int write)
		{
			//Set buffers for Physics Kernel
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateField, "WindFieldRenderTexture", WindFieldTexture[write]);
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateField, "WindFieldTexture", WindFieldTexture[read]);
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateField, "WindDensityRenderTexture", WindDensityTexture[write]);
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateField, "WindDensityTexture", WindDensityTexture[read]);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.WindFluidSimulation.GetKernelThreadGroupSizes(Ctx.KernelUpdateField, out threadGroupX, out threadGroupY,
				out threadGroupZ);

			//Run Physics Simulation
			Ctx.WindFluidSimulation.Dispatch(Ctx.KernelUpdateField, (int) (Ctx.Settings.WindFieldResolution / threadGroupX),
				(int) (Ctx.Settings.WindFieldResolution / threadGroupY), 1);
		}

		public void OnGUI()
		{
			GUI.DrawTexture(new Rect(0, 0, 256, 256),
				Utils.RenderTexture.GetRenderTextureAsTexture2D(WindFieldTexture[0], TextureFormat.RGBAFloat, false, true));
			GUI.DrawTexture(new Rect(0, 257, 256, 256),
				Utils.RenderTexture.GetRenderTextureAsTexture2D(WindFieldTexture[1], TextureFormat.RGBAFloat, false, true));
		}
	}
}