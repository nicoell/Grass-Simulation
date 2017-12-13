using UnityEngine;
using UnityEngine.Rendering;

namespace GrassSimulation.Core.Patches
{
	public sealed class BillboardTexturePatchContainer : PatchContainer
	{
		private BillboardTexturePatch _billboardTexturePatch;
		public RenderTexture BillboardTexture;
		public Texture2DArray BillboardTextures;

		public override void Destroy()
		{
			_billboardTexturePatch.Destroy();
		}

		public override Bounds GetBounds()
		{
			return _billboardTexturePatch.Bounds;
		}

		protected override void DrawImpl()
		{
			/*Ctx.BillboardTextureCamera.enabled = true;
			CommandBuffer cb = new CommandBuffer();
			cb.SetRenderTarget(_texture, _depth, 0, CubemapFace.Unknown, i); //i as depthSlice: element index in array
			cb.ClearRenderTarget(true, true, c[i], depth[i]);
 
			_cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, cb);
			_cam.cullingMask = 0;    //not actually drawing anything. just use the CommandBuffer to draw.
			_cam.targetTexture = ms_DummyTexture;
			_cam.Render();
			_cam.RemoveAllCommandBuffers();*/
			
			/*var dummyTexture = new RenderTexture(Ctx.Settings.BillboardTextureResolution,
				Ctx.Settings.BillboardTextureResolution, 0, RenderTextureFormat.ARGB32);
			Ctx.BillboardTextureCamera.targetTexture = dummyTexture;*/
			for (var i = 0; i < Ctx.BladeContainer.GetTypeCount(); i++)
			{
				//Graphics.SetRenderTarget(BillboardTexture, 0, CubemapFace.Unknown, i);
				Ctx.GrassBillboardGeneration.SetFloat("GrassType", i);
				_billboardTexturePatch.Draw();
				
				Ctx.BillboardTextureCamera.Render();
				Graphics.CopyTexture(BillboardTexture, 0, 0, BillboardTextures, i, 0);
			}
			
		}

		public override void SetupContainer()
		{
			_billboardTexturePatch = new BillboardTexturePatch(Ctx);

			BillboardTextures = new Texture2DArray(Ctx.Settings.BillboardTextureResolution,
				Ctx.Settings.BillboardTextureResolution, Ctx.BladeContainer.GetTypeCount(),
				TextureFormat.RGBA32, true, true)
			{
				name = "BillboardTextures",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
				anisoLevel = 16
			};

			BillboardTexture = new RenderTexture(Ctx.Settings.BillboardTextureResolution,
				Ctx.Settings.BillboardTextureResolution, 0,
				RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
			{
				filterMode = FilterMode.Trilinear,
				dimension = TextureDimension.Tex2DArray,
				wrapMode = TextureWrapMode.Clamp,
				antiAliasing = 8
			};
			/*BillboardTexture = new RenderTexture(Ctx.Settings.BillboardTextureResolution,
				Ctx.Settings.BillboardTextureResolution, 0,
				RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
			{
				depth = 0,
				filterMode = FilterMode.Trilinear,
				dimension = TextureDimension.Tex2DArray,
				volumeDepth = Ctx.BladeContainer.GetTypeCount(),
				enableRandomWrite = true,
				wrapMode = TextureWrapMode.Clamp
			};*/
			BillboardTexture.Create();

			Ctx.BillboardTextureCamera.aspect = 1;
			Ctx.BillboardTextureCamera.orthographic = true;
			Ctx.BillboardTextureCamera.orthographicSize = 0.5f + (0.1f * Ctx.Settings.BladeMaxHeight) / 2f;
			Ctx.BillboardTextureCamera.nearClipPlane = 0;
			Ctx.BillboardTextureCamera.farClipPlane = 1 + 2 * Ctx.Settings.BladeMaxHeight;
			Ctx.BillboardTextureCamera.useOcclusionCulling = false;
			Ctx.BillboardTextureCamera.targetTexture = BillboardTexture;
			Ctx.BillboardTextureCamera.forceIntoRenderTexture = true;
			Ctx.BillboardTextureCamera.enabled = false;

			var position = _billboardTexturePatch.Bounds.center - new Vector3(0, 0, _billboardTexturePatch.Bounds.extents.z);
			var rotation = Quaternion.LookRotation(Ctx.Transform.forward, Ctx.Transform.up);
			Ctx.BillboardTextureCamera.transform.SetPositionAndRotation(position, rotation);
		}

		protected override void DrawGizmoImpl()
		{
			_billboardTexturePatch.DrawGizmo();
		}

		public override void OnGUI()
		{
		}

		protected override void UpdatePerFrameData()
		{
			Ctx.GrassSimulationComputeShader.SetBool("ApplyTransition", Ctx.Settings.EnableHeightTransition);
			Ctx.GrassBillboardGeneration.SetVector("CamPos", Ctx.BillboardTextureCamera.transform.position);
			Ctx.GrassSimulationComputeShader.SetFloat("DeltaTime", 1);
			Ctx.GrassSimulationComputeShader.SetVector("GravityVec", Ctx.Settings.Gravity);
			Ctx.GrassSimulationComputeShader.SetMatrix("ViewProjMatrix",
				Ctx.BillboardTextureCamera.projectionMatrix * Ctx.BillboardTextureCamera.worldToCameraMatrix);
			Ctx.GrassSimulationComputeShader.SetFloats("CamPos", Ctx.BillboardTextureCamera.transform.position.x,
				Ctx.BillboardTextureCamera.transform.position.y, Ctx.BillboardTextureCamera.transform.position.z);
		}
	}
}