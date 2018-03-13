using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GrassSimulation.Core.Lod
{
	/**
	 * Info on GrassBlades:
	 * 	X,Z Coordinates ]0.0, 1.0[
	 * 	 - relative to the patch
	 * 	 - Applying _patchModelMatrix:
	 * 	 	 - translates to lower corner of bounding box
	 * 	 	 - scales to PatchSize
	 * 
	 * Y Coordinate ]0.0, 1.0[
	 * 	 - is the sampled height of the terrains heightmap
	 * 	 - Applying _patchModelMatrix:
	 * 		 - translates to Transform.position.y
	 * 		 - scales to TerrainSize
	 */
	public class GrassPatch : Patch
	{
		public uint[] _argsBillboardCrossed = {0, 0, 0, 0, 0};
		private readonly ComputeBuffer _argsBillboardCrossedBuffer;
		public uint[] _argsBillboardScreen = {0, 0, 0, 0, 0};
		private readonly ComputeBuffer _argsBillboardScreenBuffer;
		public uint[] _argsGeometry = {0, 0, 0, 0, 0};
		private readonly ComputeBuffer _argsGeometryBuffer;

		private readonly MaterialPropertyBlock _materialPropertyBlock;

		private readonly float _parameterOffsetX;
		private readonly float _parameterOffsetY;
		private readonly Vector4 _patchTexCoord; //x: xStart, y: yStart, z: width, w:height
		private readonly int _startIndex;
		private bool _applyTransition;
		private Mesh _dummyMesh;
		private Mesh _dummyMeshBillboardCrossed;
		private Mesh _dummyMeshBillboardScreen;
		private Bounds _inputBounds;
		private int _threadGroupX, _threadGroupY, _threadGroupZ;

		/*
		 * _patchModelMatrix Notes:
		 * 		Translation
		 * 			X: bounds.center.x - bounds.extents.x
		 * 			Y: Context.Transform.position.y
		 * 			Z: bounds.center.z - bounds.extents.z
		 * 		Rotation
		 * 			None as Unity Terrain doesn't take rotation into account either
		 * 		Scale
		 * 			X: PatchSize
		 * 			Y: TerrainHeight
		 * 			Z: PatchSize
		 */
		private Matrix4x4 _patchModelMatrix;

		private RenderTexture _simulationTexture0;
		private RenderTexture _simulationTexture1;
		public float DeltaTime;

		public GrassPatch(SimulationContext ctx, Vector4 patchTexCoord, Bounds bounds) : base(ctx)
		{
			_inputBounds = bounds;
			//Extend BoundingBox by blades maxheight to avoid false culling
			var boundsCorrected = bounds;
			//TODO: Unify bound correction
			boundsCorrected.size += new Vector3(Ctx.Settings.BladeMaxHeight * 2, Ctx.Settings.BladeMaxHeight * 2,
				Ctx.Settings.BladeMaxHeight * 2);
			Bounds = boundsCorrected;

			//_boundsVertices = new Bounds.BoundsVertices(bounds);
			_patchTexCoord = patchTexCoord;
			var maxElementsUsed = Mathf.Max(Ctx.Settings.GetMaxAmountBladesPerPatch(),
				Ctx.Settings.GetMaxAmountBillboardsPerPatch() * Ctx.PositionInput.GetRepetitionCount());
			_startIndex = Ctx.Random.Next(0,
				(int) (Ctx.GetBufferLength() - maxElementsUsed));
			//_startIndex = 0;
			_materialPropertyBlock = new MaterialPropertyBlock();
			_parameterOffsetX = (float) Ctx.Random.NextDouble();
			_parameterOffsetY = (float) Ctx.Random.NextDouble();

			_patchModelMatrix = Matrix4x4.TRS(
				new Vector3(bounds.center.x - bounds.extents.x, Ctx.Transform.position.y, bounds.center.z - bounds.extents.z),
				Quaternion.identity,
				new Vector3(Ctx.Settings.PatchSize, 1, Ctx.Settings.PatchSize));
			//new Vector3(Ctx.Settings.PatchSize, Ctx.DimensionsInput.GetHeight(), Ctx.Settings.PatchSize));

			// Create the IndirectArguments Buffer
			_argsGeometryBuffer =
				new ComputeBuffer(1, _argsGeometry.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsGeometry[0] = Ctx.Settings.GetMinAmountBladesPerPatch(); //Vertex Count
			_argsGeometry[1] = Ctx.Settings.LodInstancesGeometry; //Instance Count
			_argsGeometryBuffer.SetData(_argsGeometry);

			_argsBillboardCrossedBuffer =
				new ComputeBuffer(1, _argsBillboardCrossed.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsBillboardCrossed[0] = Ctx.Settings.GetMinAmountBillboardsPerPatch() * 3; //Vertex Count
			_argsBillboardCrossed[1] = 1; //Instance Count
			_argsBillboardCrossedBuffer.SetData(_argsBillboardCrossed);

			_argsBillboardScreenBuffer =
				new ComputeBuffer(1, _argsBillboardScreen.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsBillboardScreen[0] = Ctx.Settings.GetMinAmountBillboardsPerPatch(); //Vertex Count
			_argsBillboardScreen[1] = 1; //Instance Count
			_argsBillboardScreenBuffer.SetData(_argsBillboardScreen);

			CreateGrassDataTexture();
			CreateDummyMesh();
			//CreateBlossomBuffer();
			CreateDummyMeshBillboardCrossed();
			CreateDummyMeshBillboardScreen();
			SetupMaterialPropertyBlock();
		}

		public override bool IsLeaf { get { return true; } }

		public override void Unload()
		{
			_argsBillboardCrossedBuffer.Release();
			_argsBillboardScreenBuffer.Release();
			_argsGeometryBuffer.Release();
			Object.DestroyImmediate(_dummyMesh);
			Object.DestroyImmediate(_dummyMeshBillboardCrossed);
			Object.DestroyImmediate(_dummyMeshBillboardScreen);
			Object.DestroyImmediate(_simulationTexture0);
			Object.DestroyImmediate(_simulationTexture1);
		}

		public void Draw()
		{
			//TODO: Actually use _argsGeometryBuffer in computeShader or if CPU only, don't use Indirect Draw Methd
			//TODO: Add settings for options in computeShader
			ComputeLod();
			RunSimulationComputeShader();

			if (_argsGeometry[1] > 0)
			{
				Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassGeometry, Bounds, _argsGeometryBuffer, 0,
					_materialPropertyBlock);
				if (Ctx.BlossomCount > 0)
					Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassBlossom, Bounds, _argsGeometryBuffer, 0,
						_materialPropertyBlock);
			}

			if (_argsBillboardCrossed[1] > 0)
				Graphics.DrawMeshInstancedIndirect(_dummyMeshBillboardCrossed, 0, Ctx.GrassBillboardCrossed, Bounds,
					_argsBillboardCrossedBuffer, 0,
					_materialPropertyBlock);

			if (_argsBillboardScreen[1] > 0)
				Graphics.DrawMeshInstancedIndirect(_dummyMeshBillboardScreen, 0, Ctx.GrassBillboardScreen, Bounds,
					_argsBillboardScreenBuffer, 0,
					_materialPropertyBlock);
		}

		private void ComputeLod()
		{
			//Distance between Camera and closest Point on BoundingBox from Camera
			var nearestDistance =
				Vector3.Distance(Ctx.Camera.transform.position, Bounds.ClosestPoint(Ctx.Camera.transform.position));
			var farthestDistance = Vector3.Distance(Ctx.Camera.transform.position,
				Bounds.ClosestPoint(Bounds.center + (Bounds.center - Ctx.Camera.transform.position)));

			//Calculate InstanceCounts of different LODs (Geometry, BillboardsCrossed, BillboardsScreen)
			var geometryInstanceCount = (uint) Mathf.Ceil(SingleLerp(
				                            Ctx.Settings.LodInstancesGeometry,
				                            nearestDistance,
				                            Ctx.Settings.LodDistanceGeometryStart, Ctx.Settings.LodDistanceGeometryEnd));
			var billboardCrossedInstanceCount = (uint) Mathf.Ceil(DoubleLerp(
				                                    Ctx.Settings.LodInstancesBillboardCrossed,
				                                    nearestDistance,
				                                    Ctx.Settings.LodDistanceBillboardCrossedStart,
				                                    Ctx.Settings.LodDistanceBillboardCrossedPeak,
				                                    Ctx.Settings.LodDistanceBillboardCrossedEnd));
			var billboardCrossedInstanceCount2 = (uint) Mathf.Ceil(DoubleLerp(
				                                     Ctx.Settings.LodInstancesBillboardCrossed,
				                                     farthestDistance,
				                                     Ctx.Settings.LodDistanceBillboardCrossedStart,
				                                     Ctx.Settings.LodDistanceBillboardCrossedPeak,
				                                     Ctx.Settings.LodDistanceBillboardCrossedEnd));
			billboardCrossedInstanceCount = (uint) Mathf.Max(billboardCrossedInstanceCount, billboardCrossedInstanceCount2);
			var billboardScreenInstanceCount = (uint) Mathf.Ceil(DoubleLerp(
				                                   Ctx.Settings.LodInstancesBillboardScreen,
				                                   nearestDistance,
				                                   Ctx.Settings.LodDistanceBillboardScreenStart,
				                                   Ctx.Settings.LodDistanceBillboardScreenPeak,
				                                   Ctx.Settings.LodDistanceBillboardScreenEnd));
			var billboardScreenInstanceCount2 = (uint) Mathf.Ceil(DoubleLerp(
				Ctx.Settings.LodInstancesBillboardScreen,
				farthestDistance,
				Ctx.Settings.LodDistanceBillboardScreenStart,
				Ctx.Settings.LodDistanceBillboardScreenPeak,
				Ctx.Settings.LodDistanceBillboardScreenEnd));
			billboardScreenInstanceCount = (uint) Mathf.Max(billboardScreenInstanceCount, billboardScreenInstanceCount2);

			_argsGeometry[1] = geometryInstanceCount;
			_argsBillboardCrossed[1] = billboardCrossedInstanceCount;
			_argsBillboardScreen[1] = billboardScreenInstanceCount;

			_argsGeometryBuffer.SetData(_argsGeometry);
			_argsBillboardCrossedBuffer.SetData(_argsBillboardCrossed);
			_argsBillboardScreenBuffer.SetData(_argsBillboardScreen);

		}

		private static float SingleLerp(float value, float cur, float peak, float end)
		{
			if (peak >= end) end = peak + 1;
			var t1 = Mathf.Clamp01((cur - peak) / (end - peak));
			return Mathf.LerpUnclamped(value, 0, t1);
		}

		private static float DoubleLerp(float value, float cur, float start, float peak, float end)
		{
			if (start >= peak) peak = start + 1;
			if (peak >= end) end = peak + 1;
			var t0 = Mathf.Clamp01((cur - start) / (peak - start));
			var t1 = Mathf.Clamp01((cur - peak) / (end - peak));
			return value - (Mathf.LerpUnclamped(value, 0, t0) + Mathf.LerpUnclamped(0, value, t1));
		}

		private void CreateGrassDataTexture()
		{
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

		private void CreateDummyMesh()
		{
			var dummyMeshSize = Ctx.Settings.GetMinAmountBladesPerPatch();
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

		private void CreateDummyMeshBillboardCrossed()
		{
			var dummyMeshSize = Ctx.Settings.GetMinAmountBillboardsPerPatch() * 3;
			var dummyVertices = new Vector3[dummyMeshSize];
			var indices = new int[dummyMeshSize];

			for (var i = 0; i < dummyMeshSize; i++)
			{
				dummyVertices[i] = Vector3.zero;
				indices[i] = i;
			}

			_dummyMeshBillboardCrossed = new Mesh {vertices = dummyVertices};
			_dummyMeshBillboardCrossed.SetIndices(indices, MeshTopology.Points, 0);
			_dummyMeshBillboardCrossed.RecalculateBounds();
		}

		private void CreateDummyMeshBillboardScreen()
		{
			var dummyMeshSize = Ctx.Settings.GetMinAmountBillboardsPerPatch();
			var dummyVertices = new Vector3[dummyMeshSize];
			var indices = new int[dummyMeshSize];

			for (var i = 0; i < dummyMeshSize; i++)
			{
				dummyVertices[i] = Vector3.zero;
				indices[i] = i;
			}

			_dummyMeshBillboardScreen = new Mesh {vertices = dummyVertices};
			_dummyMeshBillboardScreen.SetIndices(indices, MeshTopology.Points, 0);
			_dummyMeshBillboardScreen.RecalculateBounds();
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
			//_materialPropertyBlock.SetTexture("NormalHeightTexture", _normalHeightTexture);
			_materialPropertyBlock.SetMatrix("PatchModelMatrix", _patchModelMatrix);
		}

		private void SetupSimulation()
		{
			Ctx.GrassSimulationComputeShader.SetInt("StartIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetX", _parameterOffsetX);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetY", _parameterOffsetY);
			Ctx.GrassSimulationComputeShader.SetVector("PatchTexCoord", _patchTexCoord);
			Ctx.GrassSimulationComputeShader.SetMatrix("PatchModelMatrix", _patchModelMatrix);

			//Set buffers for SimulationSetup Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture0", _simulationTexture0);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture1", _simulationTexture1);
			//Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "NormalHeightTexture", _normalHeightTexture);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelSimulationSetup, out threadGroupX,
				out threadGroupY, out threadGroupZ);
			_threadGroupX = (int) (Ctx.Settings.GrassDataResolution / threadGroupX);
			_threadGroupY = (int) (Ctx.Settings.GrassDataResolution / threadGroupY);
			_threadGroupZ = 1;

			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelSimulationSetup, _threadGroupX, _threadGroupY, _threadGroupZ);
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
			//Set DeltaTime and reset to 0
			Ctx.GrassSimulationComputeShader.SetFloat("DeltaTime", DeltaTime);
			DeltaTime = 0;

			//Set buffers for Physics Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture0", _simulationTexture0);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture1", _simulationTexture1);
			//Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "NormalHeightTexture", _normalHeightTexture);

			//uint threadGroupX, threadGroupY, threadGroupZ;
			//Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelPhysics, out threadGroupX, out threadGroupY, out threadGroupZ);

			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelPhysics, _threadGroupX, _threadGroupY, _threadGroupZ);
		}

		public override void OnGUI() { }

#if UNITY_EDITOR
		public override void DrawGizmo(int level = 0)
		{
			if (Ctx.EditorSettings.EnablePatchGizmo)
			{
				Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
				//Gizmos.DrawWireSphere(Bounds.center, 0.5f);
				Gizmos.DrawCube(Bounds.center, Bounds.size);
				Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
				Gizmos.DrawWireCube(_inputBounds.center, _inputBounds.size);
			}
		}
#endif
	}
}