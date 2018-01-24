using GrassSimulation.Core.Lod;
using UnityEngine;

namespace GrassSimulation.Core.Billboard
{
	public sealed class BillboardTexturePatchContainer : PatchContainer
	{
		private RenderTexture _billboardTexture;
		private BillboardTexturePatch _billboardTexturePatch;
		public float BillboardAspect;
		public Texture2DArray BillboardTextures;

		public override void Destroy() { _billboardTexturePatch.Destroy(); }

		public override Bounds GetBounds() { return _billboardTexturePatch.Bounds; }

		protected override void DrawImpl()
		{
			_billboardTexturePatch.RunSimulationComputeShader();

			SetupBounding();
			var mipMapCount = 1 + Mathf.FloorToInt(Mathf.Log(Mathf.Max(_billboardTexture.width, _billboardTexture.height), 2));
			for (var i = 0; i < Ctx.BladeContainer.GetTypeCount(); i++)
			{
				Ctx.GrassBillboardGeneration.SetFloat("GrassType", i);
				_billboardTexturePatch.Draw();

				Ctx.BillboardTextureCamera.Render();
				_billboardTexture.GenerateMips();
				//TODO: Custom mipmapping and antialiasing
				for (var m = 0; m < mipMapCount; m++) Graphics.CopyTexture(_billboardTexture, 0, m, BillboardTextures, i, m);
			}
		}

		private void SetupBounding()
		{
			var bounds = _billboardTexturePatch.GetBillboardBounding();

			Ctx.BillboardTextureCamera.orthographic = true;
			Ctx.BillboardTextureCamera.nearClipPlane = 0;
			Ctx.BillboardTextureCamera.farClipPlane = bounds.size.z;
			Ctx.BillboardTextureCamera.useOcclusionCulling = false;
			Ctx.BillboardTextureCamera.forceIntoRenderTexture = true;
			Ctx.BillboardTextureCamera.enabled = false;
			Ctx.BillboardTextureCamera.aspect = bounds.extents.x / bounds.extents.y;
			Ctx.BillboardTextureCamera.orthographicSize = bounds.extents.y;

			var position = bounds.center - new Vector3(0, 0, bounds.extents.z);
			var rotation = Quaternion.LookRotation(Ctx.Transform.forward, Ctx.Transform.up);
			Ctx.BillboardTextureCamera.transform.SetPositionAndRotation(position, rotation);

			BillboardAspect = Ctx.BillboardTextureCamera.aspect;

			BillboardTextures = new Texture2DArray(
				(int) (Ctx.Settings.BillboardTextureResolution * Ctx.BillboardTextureCamera.aspect + 0.5f),
				Ctx.Settings.BillboardTextureResolution, Ctx.BladeContainer.GetTypeCount(),
				TextureFormat.RGBA32, true, true)
			{
				name = "BillboardTextures",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
				anisoLevel = 16
			};

			_billboardTexture = new RenderTexture(
				(int) (Ctx.Settings.BillboardTextureResolution * Ctx.BillboardTextureCamera.aspect + 0.5f),
				Ctx.Settings.BillboardTextureResolution, 0,
				RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
			{
				filterMode = FilterMode.Trilinear,
				wrapMode = TextureWrapMode.Clamp,
				//antiAliasing = 8,
				useMipMap = true,
				autoGenerateMips = false
			};
			_billboardTexture.Create();

			Ctx.BillboardTextureCamera.targetTexture = _billboardTexture;
		}

		public override void SetupContainer() { _billboardTexturePatch = new BillboardTexturePatch(Ctx); }

		protected override void DrawGizmoImpl() { _billboardTexturePatch.DrawGizmo(); }

		public override void OnGUI() { }

		protected override void UpdatePerFrameData()
		{
			Ctx.GrassBillboardGeneration.SetVector("CamPos", Ctx.BillboardTextureCamera.transform.position);
			Ctx.GrassBillboardGeneration.SetMatrix("ViewProjMatrix", Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
			Ctx.GrassBlossomBillboardGeneration.SetVector("CamPos", Ctx.BillboardTextureCamera.transform.position);
			Ctx.GrassBlossomBillboardGeneration.SetMatrix("ViewProjMatrix", Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
			
			Ctx.GrassSimulationComputeShader.SetBool("BillboardGeneration", true);
			Ctx.GrassSimulationComputeShader.SetFloat("DeltaTime", 0.5f);
			Ctx.GrassSimulationComputeShader.SetFloat("Time", Time.time);
			Ctx.GrassSimulationComputeShader.SetMatrix("ViewProjMatrix",
				Ctx.BillboardTextureCamera.projectionMatrix * Ctx.BillboardTextureCamera.worldToCameraMatrix);
			Ctx.GrassSimulationComputeShader.SetFloats("CamPos", Ctx.BillboardTextureCamera.transform.position.x,
				Ctx.BillboardTextureCamera.transform.position.y, Ctx.BillboardTextureCamera.transform.position.z);

			Ctx.GrassSimulationComputeShader.SetVector("GravityVec", Ctx.Settings.Gravity);
		}
	}
}