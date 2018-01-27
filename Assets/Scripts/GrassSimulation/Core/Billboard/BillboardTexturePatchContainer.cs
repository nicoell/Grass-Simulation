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
		public Texture2DArray BillboardNormals;

		public override void Destroy() { _billboardTexturePatch.Destroy(); }

		public override Bounds GetBounds() { return _billboardTexturePatch.Bounds; }

		protected override void DrawImpl()
		{
			_billboardTexturePatch.RunSimulationComputeShader();

			SetupBounding();
			var mipMapCount = 1 + Mathf.FloorToInt(Mathf.Log(Mathf.Max(_billboardTexture.width, _billboardTexture.height), 2));
			for (var i = 0; i < Ctx.BladeContainer.GetTypeCount(); i++)
			{
				Ctx.GrassBillboardGeneration.SetInt("GrassType", i);
				if (Ctx.GrassBlossomBillboardGeneration) Ctx.GrassBlossomBillboardGeneration.SetInt("GrassType", i);
				//Draw texture
				Ctx.GrassBillboardGeneration.SetInt("RenderNormals", 0);
				_billboardTexturePatch.Draw();
				Ctx.BillboardTextureCamera.Render();
				//_billboardTexture.GenerateMips();
				GenerateMipMapsAlphaPreserving();
				//TODO: Custom mipmapping and antialiasing
				for (var m = 0; m < mipMapCount; m++) Graphics.CopyTexture(_billboardTexture, 0, m, BillboardTextures, i, m);
				
				Ctx.GrassBillboardGeneration.SetInt("RenderNormals", 1);
				//Draw normals
				_billboardTexturePatch.Draw();
				Ctx.BillboardTextureCamera.Render();
				_billboardTexture.GenerateMips();
				//TODO: Custom mipmapping and antialiasing
				for (var m = 0; m < mipMapCount; m++) Graphics.CopyTexture(_billboardTexture, 0, m, BillboardNormals, i, m);
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
			
			BillboardNormals = new Texture2DArray(
				(int) (Ctx.Settings.BillboardTextureResolution * Ctx.BillboardTextureCamera.aspect + 0.5f),
				Ctx.Settings.BillboardTextureResolution, Ctx.BladeContainer.GetTypeCount(),
				TextureFormat.RGBA32, true, true)
			{
				name = "BillboardNormals",
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
				autoGenerateMips = false,
				mipMapBias = -0.5f
			};
			_billboardTexture.Create();

			Ctx.BillboardTextureCamera.targetTexture = _billboardTexture;
		}

		private void GenerateMipMapsAlphaPreserving()
		{
			var tempTex2D =
				Utils.RenderTextureUtils.GetRenderTextureAsTexture2D(_billboardTexture, TextureFormat.ARGB32, true, true);
			
			var mipMapCount = 1 + Mathf.FloorToInt(Mathf.Log(Mathf.Max(_billboardTexture.width, _billboardTexture.height), 2));
			int miplevel = 1, mipWidth, mipHeight;
			
			do
			{
				mipWidth = Mathf.Max(1, _billboardTexture.width >> miplevel);
				mipHeight = Mathf.Max(1, _billboardTexture.height >> miplevel);

				var uvCenter = new Vector2(0.5f / mipWidth, 0.5f / mipHeight);
				var colors = new Color[mipHeight * mipWidth];
				for (var y = 0; y < mipHeight; y++)
				for (var x = 0; x < mipWidth; x++)
				{
					float r = 0, g = 0, b = 0, a = 0;
					
					var uv = new Vector2((float) x / mipWidth + uvCenter.x, (float) y / mipHeight + uvCenter.y);
					var color = tempTex2D.GetPixelBilinear(uv.x, uv.y);
					r = color.r;
					g = color.g;
					b = color.b;
					a = color.a >= Ctx.Settings.BillboardAlphaCutoff ? 1 : color.a;
							

					var index = mipWidth * y + x;
					colors[index] = new Color(r, g, b, a);
				}
				tempTex2D.SetPixels(colors, miplevel);

				miplevel++;
			} while (mipHeight != 1 || mipWidth != 1);
			
			tempTex2D.Apply(false);
			
			for (var m = 1; m < mipMapCount; m++) Graphics.CopyTexture(tempTex2D, 0, m, _billboardTexture, 0, m);
		}
		
		public override void SetupContainer() { _billboardTexturePatch = new BillboardTexturePatch(Ctx); }

		protected override void DrawGizmoImpl() { _billboardTexturePatch.DrawGizmo(); }

		public override void OnGUI() { }

		protected override void UpdatePerFrameData()
		{
			Ctx.GrassBillboardGeneration.SetVector("CamPos", Ctx.BillboardTextureCamera.transform.position);
			Ctx.GrassBillboardGeneration.SetMatrix("ViewProjMatrix", Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
			Ctx.GrassBillboardGeneration.SetFloat("MinGrassBladeWidth", Ctx.Settings.BladeMaxWidth);

			if (Ctx.GrassBlossomBillboardGeneration)
			{
				Ctx.GrassBlossomBillboardGeneration.SetVector("CamPos", Ctx.BillboardTextureCamera.transform.position);
				Ctx.GrassBlossomBillboardGeneration.SetMatrix("ViewProjMatrix",
					Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
				Ctx.GrassBlossomBillboardGeneration.SetFloat("MinGrassBladeWidth", Ctx.Settings.BladeMaxWidth);
			}
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