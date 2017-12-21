using UnityEngine;

namespace GrassSimulation.Core.Wind
{
	public class WindFieldRenderer : ContextRequirement
	{
		//public readonly RenderTexture[] WindDensityTexture;
		public readonly RenderTexture[] WindFieldTexture;
		private int _textureIndex;
		private Matrix4x4 _colorMatrix;

		public WindFieldRenderer(SimulationContext ctx, Bounds bounds) : base(ctx)
		{
			//WindDensityTexture = new RenderTexture[2];
			WindFieldTexture = new RenderTexture[2];
			_textureIndex = 0;
			
			for (var i = 0; i < 2; i++)
			{
				/*WindDensityTexture[i] = new RenderTexture(Ctx.Settings.WindDensityResolution,
					Ctx.Settings.WindDensityResolution, 0,
					RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
				{
					wrapMode = TextureWrapMode.Clamp,
					enableRandomWrite = true,
					depth = 0,
					filterMode = FilterMode.Bilinear
				};
				WindDensityTexture[i].Create();*/

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
			
			Ctx.WindFluidSimulation.SetFloat("Viscosity", Ctx.Settings.FluidViscosity);
			Ctx.WindFluidSimulation.SetFloat("PressureScale", Ctx.Settings.FluidPressureScale);
			
			Ctx.WindFluidSimulation.SetFloat("WindFieldResolution", Ctx.Settings.WindFieldResolution);
			Ctx.WindFluidSimulation.SetFloat("FieldStep", 1f / Ctx.Settings.WindFieldResolution);

			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "WindFieldTexture", WindFieldTexture[1]);
			
			SetupWindField();
		}

		public void Update()
		{
			if (Input.GetMouseButton(0))
			{
				Ctx.WindFluidSimulation.SetFloat("input", 1);
			} else
			{
				Ctx.WindFluidSimulation.SetFloat("input", 0);
			}
			Ctx.WindFluidSimulation.SetFloat("DeltaTime", Time.deltaTime * Ctx.Settings.FluidTimeFactor);
			for (var i = 0; i < Ctx.Settings.FluidIterationSteps; i++)
			{
				UpdateWindField(_textureIndex, (_textureIndex + 1) % 2);
				//UpdateWindField(0, 1);
				_textureIndex = (_textureIndex + 1) % 2;
			}
			if (Ctx.DisplayRenderTexture)
			{
				var sharedMat = Ctx.DisplayRenderTexture.GetComponent<MeshRenderer>().sharedMaterial;
				sharedMat.SetTexture("MainTex", WindFieldTexture[0]);
				sharedMat.SetMatrix("ColorMatrix", GenerateMatrix());
			}
		}
		
		
		private Matrix4x4 GenerateMatrix() {
			var m = Matrix4x4.identity;

			var alpha = new Vector4 (0f, 0f, 0f, 1f);
			for (var i = 0; i < 4; i++)
				m.SetRow (i, alpha);

			return m;
		}
		
		private void UpdateWindField(int first, int second)
		{
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateField, "WindFieldRenderTexture", WindFieldTexture[second]);
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateField, "WindFieldTexture", WindFieldTexture[first]);
			//Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateField, "WindDensityRenderTexture", WindDensityTexture[write]);
			//Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateField, "WindDensityTexture", WindDensityTexture[first]);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.WindFluidSimulation.GetKernelThreadGroupSizes(Ctx.KernelUpdateField, out threadGroupX, out threadGroupY,
				out threadGroupZ);

			Ctx.WindFluidSimulation.Dispatch(Ctx.KernelUpdateField, (int) (Ctx.Settings.WindFieldResolution / threadGroupX),
				(int) (Ctx.Settings.WindFieldResolution / threadGroupY), 1);
			
			/*//Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateDensity, "WindFieldRenderTexture", WindFieldTexture[write]);
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateDensity, "WindFieldTexture", WindFieldTexture[second]);
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateDensity, "WindDensityRenderTexture", WindDensityTexture[second]);
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateDensity, "WindDensityTexture", WindDensityTexture[first]);

			Ctx.WindFluidSimulation.GetKernelThreadGroupSizes(Ctx.KernelUpdateDensity, out threadGroupX, out threadGroupY,
				out threadGroupZ);

			Ctx.WindFluidSimulation.Dispatch(Ctx.KernelUpdateDensity, (int) (Ctx.Settings.WindDensityResolution / threadGroupX),
				(int) (Ctx.Settings.WindDensityResolution / threadGroupY), 1);*/
		}

		private void SetupWindField()
		{
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelSetupField, "WindFieldRenderTexture", WindFieldTexture[0]);
			
			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.WindFluidSimulation.GetKernelThreadGroupSizes(Ctx.KernelSetupField, out threadGroupX, out threadGroupY,
				out threadGroupZ);
			Ctx.WindFluidSimulation.Dispatch(Ctx.KernelSetupField, (int) (Ctx.Settings.WindFieldResolution / threadGroupX),
				(int) (Ctx.Settings.WindFieldResolution / threadGroupY), 1);
			
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelSetupField, "WindFieldRenderTexture", WindFieldTexture[1]);
			
			Ctx.WindFluidSimulation.GetKernelThreadGroupSizes(Ctx.KernelSetupField, out threadGroupX, out threadGroupY,
				out threadGroupZ);
			Ctx.WindFluidSimulation.Dispatch(Ctx.KernelSetupField, (int) (Ctx.Settings.WindFieldResolution / threadGroupX),
				(int) (Ctx.Settings.WindFieldResolution / threadGroupY), 1);
			
			/*Ctx.WindFluidSimulation.SetTexture(Ctx.KernelSetupDensity, "WindDensityRenderTexture", WindDensityTexture[1]);
			
			Ctx.WindFluidSimulation.GetKernelThreadGroupSizes(Ctx.KernelSetupDensity, out threadGroupX, out threadGroupY,
				out threadGroupZ);

			Ctx.WindFluidSimulation.Dispatch(Ctx.KernelSetupDensity, (int) (Ctx.Settings.WindDensityResolution / threadGroupX),
				(int) (Ctx.Settings.WindDensityResolution / threadGroupY), 1);*/
		}

		public void OnGUI()
		{
			
			
		}
	}
}