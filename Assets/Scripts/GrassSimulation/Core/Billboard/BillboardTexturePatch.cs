#region

using GrassSimulation.Core.Lod;
using GrassSimulation.Core.Utils;
using UnityEngine;
using UnityEngine.Rendering;

#endregion

namespace GrassSimulation.Core.Billboard
{
	public class BillboardTexturePatch : Patch
	{
		private readonly uint[] _argsGeometry = {0, 0, 0, 0, 0};
		private readonly ComputeBuffer _argsGeometryBuffer;
		private readonly MaterialPropertyBlock _materialPropertyBlock;
		private readonly float _parameterOffsetX;
		private readonly float _parameterOffsetY;
		private readonly Matrix4x4 _patchModelMatrix;
		private readonly Vector4 _patchTexCoord; //x: xStart, y: yStart, z: width, w:height
		private readonly int _startIndex;
		private Texture2D _boundsTexture0;
		private Texture2D _boundsTexture1;
		private Mesh _dummyMesh;
		private Texture2D _normalHeightTexture;

		private RenderTexture _simulationTexture0;
		private RenderTexture _simulationTexture1;

		public BillboardTexturePatch(SimulationContext ctx) : base(ctx)
		{
			_patchTexCoord = new Vector4(0, 0, 1, 1);
			Bounds = new Bounds(Vector3.zero, Vector3.one);
			_startIndex = Ctx.Random.Next(0,
				(int) (Ctx.GetBufferLength() - Ctx.Settings.BillboardGrassCount));
			_materialPropertyBlock = new MaterialPropertyBlock();
			_parameterOffsetX = (float) Ctx.Random.NextDouble();
			_parameterOffsetY = (float) Ctx.Random.NextDouble();
			//TODO: make customizable
			_patchModelMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
				new Vector3(Ctx.Settings.BillboardGrassSpacingFactor, 1f, 5f));

			_argsGeometryBuffer =
				new ComputeBuffer(1, _argsGeometry.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsGeometry[0] = Ctx.Settings.BillboardGrassCount; //Vertex Count
			_argsGeometry[1] = 1;
			_argsGeometryBuffer.SetData(_argsGeometry);

			CreateGrassDataTexture();
			CreateDummyMesh();
			SetupMaterialPropertyBlock();
		}

		public override bool IsLeaf { get { return true; } }

		public override void Unload()
		{
			_argsGeometryBuffer.Release();
			Object.DestroyImmediate(_boundsTexture0);
			Object.DestroyImmediate(_boundsTexture1);
			Object.DestroyImmediate(_normalHeightTexture);
			Object.DestroyImmediate(_simulationTexture0);
			Object.DestroyImmediate(_simulationTexture1);
			Object.DestroyImmediate(_dummyMesh);
		}

		public void Draw()
		{
			//RunSimulationComputeShader();
			if (_argsGeometry[1] > 0)
			{
				Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassBillboardGeneration, Bounds, _argsGeometryBuffer, 0,
					_materialPropertyBlock, ShadowCastingMode.Off, false, 0, Ctx.BillboardTextureCamera);
				if (Ctx.GrassBlossomBillboardGeneration)
					Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassBlossomBillboardGeneration, Bounds, _argsGeometryBuffer,
						0, _materialPropertyBlock, ShadowCastingMode.Off, false, 0, Ctx.BillboardTextureCamera);
			}
		}

		private void SetupMaterialPropertyBlock()
		{
			//TODO: Add option to update things like matrix not only on startup but also on update
			_materialPropertyBlock.SetFloat("StartIndex", _startIndex);
			_materialPropertyBlock.SetFloat("ParameterOffsetX", _parameterOffsetX);
			_materialPropertyBlock.SetFloat("ParameterOffsetY", _parameterOffsetY);
			_materialPropertyBlock.SetVector("PatchTexCoord", _patchTexCoord);
			_materialPropertyBlock.SetTexture("SimulationTexture0", _simulationTexture0);
			_materialPropertyBlock.SetTexture("SimulationTexture1", _simulationTexture1);
			_materialPropertyBlock.SetTexture("NormalHeightTexture", _normalHeightTexture);
			_materialPropertyBlock.SetMatrix("PatchModelMatrix", _patchModelMatrix);
		}

		public Bounds GetBillboardBounding()
		{
			var min = Vector3.positiveInfinity;
			var max = Vector3.negativeInfinity;

			//var debug = "_startIndex = "+_startIndex;
			//debug += "\n\tmin = "+min;
			//debug += "\n\tmax = "+max;

			for (var i = _startIndex; i < _startIndex + Ctx.Settings.BillboardGrassCount; i++)
			{
				var localV0 = new Vector3(Ctx.GrassInstance.UvData[i].Position.x, 0, Ctx.GrassInstance.UvData[i].Position.y);
				var v0 = _patchModelMatrix.MultiplyPoint(localV0);
				var v1Sample = _boundsTexture0.GetPixelBilinear(localV0.x, localV0.z);
				var v2Sample = _boundsTexture1.GetPixelBilinear(localV0.x, localV0.z);
				var v1 = new Vector3(v1Sample.r, v1Sample.g, v1Sample.b);
				var v2 = new Vector3(v2Sample.r, v2Sample.g, v2Sample.b);
				
				//Sample Curve
				for (float t = 0; t <= 1; t+=0.01f)
				{
					var h1 = v0 + t * v1;
					var h2 = v0 + v1 + t * (v2 - v1);
					var c = h1 + t * (h2 - h1);
					min = Vector3.Min(min, c);
					max = Vector3.Max(max, c);
				}
				
				min = Vector3.Min(min, v0);
				max = Vector3.Max(max, v0);
				
				/*
				debug += "\n#### Loop#" + (i - _startIndex);
				debug += "\n\tlocalV0 = " + localV0;
				debug += "\n\tv0 = " + v0;
				debug += "\n\tv1Sample = " + v1Sample;
				debug += "\n\tv2Sample = " + v2Sample;
				debug += "\n\tv1 = " + v1;
				debug += "\n\tv2 = " + v2;
				debug += "\n\tmin = "+min;
				debug += "\n\tmax = "+max;
				*/
			}

			var retBounds = new Bounds();
			var bladeWidthCorrection = new Vector3(Ctx.Settings.BladeMaxWidth * Ctx.Settings.BillboardGrassWidthCorrectionFactor,
				0, Ctx.Settings.BladeMaxWidth * Ctx.Settings.BillboardGrassWidthCorrectionFactor);
			var bladeHeightCorrection = new Vector3(0, max.y*0.05f, 0);
			retBounds.SetMinMax(min - bladeWidthCorrection, max + bladeWidthCorrection + bladeHeightCorrection);

			Bounds = retBounds;
			return retBounds;
		}

		public override void DrawGizmo(int level = 0)
		{
			//base.DrawGizmo();

			if (!_boundsTexture0 || !_boundsTexture1) return;

			var min = Vector3.positiveInfinity;
			var max = Vector3.negativeInfinity;

			for (var i = _startIndex; i < _startIndex + Ctx.Settings.BillboardGrassCount; i++)
			{
				var localV0 = new Vector3(Ctx.GrassInstance.UvData[i].Position.x, 0, Ctx.GrassInstance.UvData[i].Position.y);
				var v0 = _patchModelMatrix.MultiplyPoint(localV0);
				var v1Sample = _boundsTexture0.GetPixelBilinear(localV0.x, localV0.z);
				var v2Sample = _boundsTexture1.GetPixelBilinear(localV0.x, localV0.z);
				var v1 = new Vector3(v1Sample.r, v1Sample.g, v1Sample.b);
				var v2 = new Vector3(v2Sample.r, v2Sample.g, v2Sample.b);

				var cOld = v0;
				var colorA = new Color(0.77f, 0.64f, 0.05f);
				var colorB = new Color(0.78f, 0.43f, 0.66f);
				
				//Sample Curve
				for (float t = 0; t <= 1; t+=0.01f)
				{
					var h1 = v0 + t * v1;
					var h2 = v0 + v1 + t * (v2 - v1);
					var c = h1 + t * (h2 - h1);
					
					min = Vector3.Min(min, c);
					max = Vector3.Max(max, c);
					
					Gizmos.color = Color.Lerp(colorA, colorB, t);
					Gizmos.DrawLine(cOld, c);

					cOld = c;
				}
				
				min = Vector3.Min(min, v0);
				max = Vector3.Max(max, v0);

				//Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
				//Gizmos.DrawLine(v0, v1);
				//Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
				//Gizmos.DrawLine(v1, v2);
			}

			var retBounds = new Bounds();
			retBounds.SetMinMax(min, max);
			retBounds = GetBillboardBounding();

			Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
			Gizmos.DrawWireCube(retBounds.center, retBounds.size);
		}

		public void RunSimulationComputeShader()
		{
			//Set per patch data for whole compute shader
			Ctx.GrassSimulationComputeShader.SetInt("StartIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetX", _parameterOffsetX);
			Ctx.GrassSimulationComputeShader.SetVector("PatchTexCoord", _patchTexCoord);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetY", _parameterOffsetY);
			Ctx.GrassSimulationComputeShader.SetFloat("GrassDataResolution", Ctx.Settings.GrassDataResolution);
			Ctx.GrassSimulationComputeShader.SetMatrix("PatchModelMatrix", _patchModelMatrix);

			//Set buffers for Physics Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture0", _simulationTexture0);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture1", _simulationTexture1);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "NormalHeightTexture", _normalHeightTexture);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelPhysics, out threadGroupX, out threadGroupY,
				out threadGroupZ);

			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelPhysics, (int) (Ctx.Settings.GrassDataResolution / threadGroupX),
				(int) (Ctx.Settings.GrassDataResolution / threadGroupY), 1);

			_boundsTexture0 = RenderTextureUtils.GetRenderTextureAsTexture2D(
				_simulationTexture0,
				TextureFormat.RGBAFloat, false, true);
			_boundsTexture1 = RenderTextureUtils.GetRenderTextureAsTexture2D(
				_simulationTexture1,
				TextureFormat.RGBAFloat, false, true);
		}

		private void CreateGrassDataTexture()
		{
			_normalHeightTexture = new Texture2D(Ctx.Settings.GetPerPatchTextureWidthHeight(),
				Ctx.Settings.GetPerPatchTextureWidthHeight(),
				TextureFormat.RGBAFloat, false, true)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			var textureData = new Color[Ctx.Settings.GetPerPatchTextureLength()];
			var i = 0;
			for (var y = 0; y < Ctx.Settings.GetPerPatchTextureWidthHeight(); y++)
			for (var x = 0; x < Ctx.Settings.GetPerPatchTextureWidthHeight(); x++)
			{
				var posY = 0f;
				var up = Vector3.up;

				textureData[i] = new Color(up.x, up.y, up.z, posY);
				i++;
			}

			_normalHeightTexture.SetPixels(textureData);
			_normalHeightTexture.Apply();

			_simulationTexture0 = new RenderTexture(Ctx.Settings.GetPerPatchTextureWidthHeight(),
				Ctx.Settings.GetPerPatchTextureWidthHeight(), 0,
				RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
			{
				filterMode = FilterMode.Bilinear,
				autoGenerateMips = false,
				useMipMap = false,
				enableRandomWrite = true,
				wrapMode = TextureWrapMode.Clamp
			};
			_simulationTexture0.Create();

			_simulationTexture1 = new RenderTexture(Ctx.Settings.GetPerPatchTextureWidthHeight(),
				Ctx.Settings.GetPerPatchTextureWidthHeight(), 0,
				RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
			{
				filterMode = FilterMode.Bilinear,
				autoGenerateMips = false,
				useMipMap = false,
				enableRandomWrite = true,
				wrapMode = TextureWrapMode.Clamp
			};
			_simulationTexture1.Create();

			SetupSimulation();
		}

		private void SetupSimulation()
		{
			Ctx.GrassSimulationComputeShader.SetInt("StartIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetX", _parameterOffsetX);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetY", _parameterOffsetY);
			Ctx.GrassSimulationComputeShader.SetMatrix("PatchModelMatrix", _patchModelMatrix);

			//Set buffers for SimulationSetup Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture0", _simulationTexture0);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture1", _simulationTexture1);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "NormalHeightTexture", _normalHeightTexture);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelSimulationSetup, out threadGroupX,
				out threadGroupY, out threadGroupZ);

			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelSimulationSetup,
				(int) (Ctx.Settings.GrassDataResolution / threadGroupX), (int) (Ctx.Settings.GrassDataResolution / threadGroupY),
				1);
		}

		private void CreateDummyMesh()
		{
			var dummyMeshSize = Ctx.Settings.BillboardGrassCount;
			var dummyVertices = new Vector3[dummyMeshSize];
			var indices = new int[dummyMeshSize];

			for (var i = 0; i < dummyMeshSize; i++)
			{
				dummyVertices[i] = Vector3.zero;
				indices[i] = i;
			}

			_dummyMesh = new Mesh {vertices = dummyVertices};
			_dummyMesh.SetIndices(indices, MeshTopology.Points, 0);
			_dummyMesh.RecalculateBounds();
		}
	}
}