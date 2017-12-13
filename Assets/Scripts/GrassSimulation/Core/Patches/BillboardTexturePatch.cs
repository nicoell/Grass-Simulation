using UnityEngine;
using UnityEngine.Rendering;

namespace GrassSimulation.Core.Patches
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
		private Mesh _dummyMesh;
		private Texture2D _normalHeightTexture;
		private RenderTexture _simulationTexture;

		public BillboardTexturePatch(SimulationContext ctx) : base(ctx)
		{
			_patchTexCoord = new Vector4(0, 0, 1, 1);
			Bounds = new Bounds(Vector3.zero,
				new Vector3(1 + 2 * Ctx.Settings.BladeMaxHeight, Ctx.Settings.BladeMaxHeight, 1 + 2 * Ctx.Settings.BladeMaxHeight));
			_startIndex = Ctx.Random.Next(0,
				(int) (Ctx.Settings.GetSharedBufferLength() - Ctx.Settings.GetMaxAmountBladesPerPatch()));
			_materialPropertyBlock = new MaterialPropertyBlock();
			_parameterOffsetX = (float) Ctx.Random.NextDouble();
			_parameterOffsetY = (float) Ctx.Random.NextDouble();
			_patchModelMatrix = Matrix4x4.TRS(new Vector3(-0.5f, -0.5f - (0.1f * Ctx.Settings.BladeMaxHeight) / 2f, -0.5f), Quaternion.identity,
				Vector3.one);

			_argsGeometryBuffer =
				new ComputeBuffer(1, _argsGeometry.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsGeometry[0] = Ctx.Settings.BillboardTextureGrassCount; //Vertex Count
			_argsGeometry[1] = 1;
			_argsGeometryBuffer.SetData(_argsGeometry);

			CreateGrassDataTexture();
			CreateDummyMesh();
			SetupMaterialPropertyBlock();
		}

		public override bool IsLeaf
		{
			get { return true; }
		}

		public void Destroy()
		{
			//TODO: Clean up buffers and textures
			_argsGeometryBuffer.Release();
		}

		public void Draw()
		{
			RunSimulationComputeShader();
			if (_argsGeometry[1] > 0)
				Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassBillboardGeneration, Bounds, _argsGeometryBuffer, 0,
					_materialPropertyBlock, ShadowCastingMode.Off, false, 0,
					Ctx.BillboardTextureCamera);
		}

		private void SetupMaterialPropertyBlock()
		{
			//TODO: Add option to update things like matrix not only on startup but also on update
			_materialPropertyBlock.SetFloat("StartIndex", _startIndex);
			_materialPropertyBlock.SetFloat("ParameterOffsetX", _parameterOffsetX);
			_materialPropertyBlock.SetFloat("ParameterOffsetY", _parameterOffsetY);
			_materialPropertyBlock.SetVector("PatchTexCoord", _patchTexCoord);
			_materialPropertyBlock.SetTexture("SimulationTexture", _simulationTexture);
			_materialPropertyBlock.SetTexture("NormalHeightTexture", _normalHeightTexture);
			_materialPropertyBlock.SetMatrix("PatchModelMatrix", _patchModelMatrix);
		}

		private void RunSimulationComputeShader()
		{
			//Set per patch data for whole compute shader
			Ctx.GrassSimulationComputeShader.SetInt("StartIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetX", _parameterOffsetX);
			Ctx.GrassSimulationComputeShader.SetVector("PatchTexCoord", _patchTexCoord);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetY", _parameterOffsetY);
			Ctx.GrassSimulationComputeShader.SetFloat("GrassDataResolution", Ctx.Settings.GrassDataResolution);
			Ctx.GrassSimulationComputeShader.SetMatrix("PatchModelMatrix", _patchModelMatrix);

			//Set buffers for Physics Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture", _simulationTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "NormalHeightTexture", _normalHeightTexture);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelPhysics, out threadGroupX, out threadGroupY,
				out threadGroupZ);

			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelPhysics, (int) (Ctx.Settings.GrassDataResolution / threadGroupX),
				(int) (Ctx.Settings.GrassDataResolution / threadGroupY), 1);
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

			_simulationTexture = new RenderTexture(Ctx.Settings.GetPerPatchTextureWidthHeight(),
				Ctx.Settings.GetPerPatchTextureWidthHeight(), 0,
				RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
			{
				//TODO: Remove if mipmaps not used or use mipmaps
				filterMode = Ctx.Settings.GrassDataTrilinearFiltering ? FilterMode.Trilinear : FilterMode.Bilinear,
				autoGenerateMips = Ctx.Settings.GrassDataTrilinearFiltering,
				useMipMap = Ctx.Settings.GrassDataTrilinearFiltering,
				dimension = TextureDimension.Tex2DArray,
				volumeDepth = 2,
				enableRandomWrite = true,
				wrapMode = TextureWrapMode.Clamp
			};
			_simulationTexture.Create();

			SetupSimulation();
		}

		private void SetupSimulation()
		{
			Ctx.GrassSimulationComputeShader.SetInt("StartIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetX", _parameterOffsetX);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetY", _parameterOffsetY);
			Ctx.GrassSimulationComputeShader.SetMatrix("PatchModelMatrix", _patchModelMatrix);

			//Set buffers for SimulationSetup Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture", _simulationTexture);
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
			var dummyMeshSize = Ctx.Settings.BillboardTextureGrassCount;
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