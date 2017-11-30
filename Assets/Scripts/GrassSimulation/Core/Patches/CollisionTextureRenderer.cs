using UnityEngine;

namespace GrassSimulation.Core.Patches
{
	public class CollisionTextureRenderer : ContextRequirement
	{
		private Bounds _bounds;
		public RenderTexture CollisionDepthTexture;

		public CollisionTextureRenderer(SimulationContext ctx, Bounds bounds) : base(ctx)
		{
			_bounds = bounds;
			//Create CollisionDepthTexture
			//Init Collision Camera
			CollisionDepthTexture = new RenderTexture(Ctx.Settings.CollisionDepthResolution,
				Ctx.Settings.CollisionDepthResolution, 0,
				RenderTextureFormat.Depth, RenderTextureReadWrite.Linear)
			{
				wrapMode = TextureWrapMode.Clamp,
				depth = 32
			};
			CollisionDepthTexture.Create();

			Ctx.CollisionCamera.aspect = Ctx.DimensionsInput.GetWidth() / Ctx.DimensionsInput.GetDepth();//Settings.PatchSize / Settings.PatchSize;
			Ctx.CollisionCamera.orthographic = true;
			Ctx.CollisionCamera.orthographicSize = Mathf.Max(_bounds.extents.x, _bounds.extents.z);
			Ctx.CollisionCamera.nearClipPlane = 0;
			Ctx.CollisionCamera.farClipPlane = _bounds.size.y;
			Ctx.CollisionCamera.useOcclusionCulling = false;
			Ctx.CollisionCamera.depthTextureMode = DepthTextureMode.Depth;
			Ctx.CollisionCamera.SetReplacementShader(Ctx.CollisionDepthShader, "RenderType");
			Ctx.CollisionCamera.targetTexture = CollisionDepthTexture;
			
			_bounds = bounds;
			var position = _bounds.center - new Vector3(0, _bounds.extents.y, 0);
			var rotation = Quaternion.LookRotation(Ctx.Transform.up, Ctx.Transform.forward);
			Ctx.CollisionCamera.transform.SetPositionAndRotation(position, rotation);
			
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "CollisionDepthTexture", CollisionDepthTexture);
			Ctx.GrassSimulationComputeShader.SetFloat("CollisionVolumeHeight", _bounds.size.y);
		}

		public void UpdateDepthTexture()
		{
			Ctx.CollisionCamera.Render();
		}

		public void OnGUI()
		{
			GUI.DrawTexture(new Rect(0, 0, 256, 256), CollisionDepthTexture);
		}
	}
}