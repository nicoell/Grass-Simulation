using GrassSimulation.Core.Lod;
using UnityEngine;

namespace GrassSimulation.Core.Billboard
{
	public sealed class BillboardTexturePatchContainer : PatchContainer
	{
		private BillboardTexturePatch _billboardTexturePatch;
		private RenderTexture _billboardTexture;
		public Texture2DArray BillboardTextures;
		public float BillboardAspect;

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
			//TODO: Calculate boundingbox/orthographic frustum in computeshader, save it to a buffer and read it on the cpu, set the camera accordingly.
			//Evoila, we have a perfect fitting texture.
			_billboardTexturePatch.RunSimulationComputeShader();

			SetupBounding();
			var mipMapCount = 1 + Mathf.FloorToInt(Mathf.Log(Mathf.Max(_billboardTexture.width, _billboardTexture.height), 2));
			for (var i = 0; i < Ctx.BladeContainer.GetTypeCount(); i++)
			{
				Ctx.GrassBillboardGeneration.SetFloat("GrassType", i);
				_billboardTexturePatch.Draw();
				
				Ctx.BillboardTextureCamera.Render();
				_billboardTexture.GenerateMips();
				//TODO Custom mipmapping and antialiasing
				for (int m = 0; m < mipMapCount; m++)
					Graphics.CopyTexture(_billboardTexture, 0, m, BillboardTextures, i, m);
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
			
			BillboardTextures = new Texture2DArray(Ctx.Settings.BillboardTextureResolution,
				(int) (Ctx.Settings.BillboardTextureResolution * Ctx.BillboardTextureCamera.aspect + 0.5f), Ctx.BladeContainer.GetTypeCount(),
				TextureFormat.RGBA32, true, true)
			{
				name = "BillboardTextures",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
				anisoLevel = 16
			};

			_billboardTexture = new RenderTexture(Ctx.Settings.BillboardTextureResolution,
				(int) (Ctx.Settings.BillboardTextureResolution * Ctx.BillboardTextureCamera.aspect + 0.5f), 0,
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

		public override void SetupContainer()
		{
			_billboardTexturePatch = new BillboardTexturePatch(Ctx);
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
			
			Ctx.GrassBillboardGeneration.SetFloat("specular", Ctx.Settings.Specular);
			Ctx.GrassBillboardGeneration.SetFloat("gloss", Ctx.Settings.Gloss);
			Ctx.GrassBillboardGeneration.SetVector("viewDir", Ctx.Camera.transform.forward);
			Ctx.GrassBillboardGeneration.SetVector("lightDir", Ctx.Light.transform.forward);
			Ctx.GrassBillboardGeneration.SetVector("lightColor", Ctx.Light.color);
			Ctx.GrassBillboardGeneration.SetMatrix("ViewProjMatrix",
				Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
			
			Ctx.GrassSimulationComputeShader.SetFloat("DeltaTime", 1f);
			Ctx.GrassSimulationComputeShader.SetFloat("Time", Time.time);
			Ctx.GrassSimulationComputeShader.SetMatrix("ViewProjMatrix",
				Ctx.BillboardTextureCamera.projectionMatrix * Ctx.BillboardTextureCamera.worldToCameraMatrix);
			Ctx.GrassSimulationComputeShader.SetFloats("CamPos", Ctx.BillboardTextureCamera.transform.position.x,
				Ctx.BillboardTextureCamera.transform.position.y, Ctx.BillboardTextureCamera.transform.position.z);

			Ctx.GrassSimulationComputeShader.SetFloat("WindAmplitude", Ctx.Settings.WindAmplitude);
			Ctx.GrassSimulationComputeShader.SetVector("GravityVec", Ctx.Settings.Gravity);
		}
	}
}