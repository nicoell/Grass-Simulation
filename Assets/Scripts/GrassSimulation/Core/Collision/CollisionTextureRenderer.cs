using UnityEngine;

namespace GrassSimulation.Core.Collision
{
	public class CollisionTextureRenderer : ContextRequirement
	{
		public readonly RenderTexture CollisionDepthTexture;

		public CollisionTextureRenderer(SimulationContext ctx, Bounds bounds) : base(ctx)
		{

			//Create CollisionDepthTexture
			//Init Collision Camera
			CollisionDepthTexture = new RenderTexture(Ctx.Settings.CollisionDepthResolution,
				Ctx.Settings.CollisionDepthResolution, 0,
				RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
			{
				wrapMode = TextureWrapMode.Clamp,
				depth = 0,
				filterMode = FilterMode.Bilinear
			};
			CollisionDepthTexture.Create();

			Ctx.CollisionCamera.aspect = Ctx.DimensionsInput.GetWidth() / Ctx.DimensionsInput.GetDepth();//Settings.PatchSize / Settings.PatchSize;
			Ctx.CollisionCamera.orthographic = true;
			//TODO: Unify bound correction
			Ctx.CollisionCamera.orthographicSize = Mathf.Max(bounds.extents.x - Ctx.Settings.BladeMaxHeight, bounds.extents.z - Ctx.Settings.BladeMaxHeight);
			Ctx.CollisionCamera.nearClipPlane = 0;
			Ctx.CollisionCamera.farClipPlane = bounds.size.y;
			Ctx.CollisionCamera.useOcclusionCulling = false;
			Ctx.CollisionCamera.depthTextureMode = DepthTextureMode.Depth;
			Ctx.CollisionCamera.SetReplacementShader(Ctx.CollisionDepthShader, "RenderType");
			Ctx.CollisionCamera.targetTexture = CollisionDepthTexture;
			
			var position = bounds.center - new Vector3(0, bounds.extents.y, 0);
			var rotation = Quaternion.LookRotation(Ctx.Transform.up, Ctx.Transform.forward);
			Ctx.CollisionCamera.transform.SetPositionAndRotation(position, rotation);
			
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "CollisionDepthTexture", CollisionDepthTexture);
			Ctx.WindFluidSimulation.SetTexture(Ctx.KernelUpdateField, "CollisionDepthTexture", CollisionDepthTexture);
			Ctx.GrassSimulationComputeShader.SetFloats("CollisionVolumeSize", bounds.size.x, bounds.size.y, bounds.size.z);
			Ctx.GrassSimulationComputeShader.SetFloats("CollisionVolumeMin", bounds.min.x, bounds.min.y, bounds.min.z);
			Ctx.GrassSimulationComputeShader.SetFloats("CollisionVolumeMax", bounds.max.x, bounds.max.y, bounds.max.z);
			Ctx.GrassSimulationComputeShader.SetMatrix("CollisionViewProj", Ctx.CollisionCamera.projectionMatrix * Ctx.CollisionCamera.worldToCameraMatrix);
		}

		public void UpdateDepthTexture()
		{
			Ctx.CollisionCamera.Render();
		}

		public void OnGUI()
		{
			
		}
	}
}