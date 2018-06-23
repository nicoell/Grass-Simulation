using System.Collections.Generic;
using GrassSimulation.Core.Lod;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrassSimulation.StandardContainers
{
	public struct IdDeltaTime
	{
		public uint Id;
		public float DeltaTime;
	}

	public class UniformGridHierarchyPatchContainer : PatchContainer
	{
		private GrassDrawGroup[] _billboardCrossedDrawGroups;
		private GrassDrawGroup[] _billboardScreenDrawGroups;

		private GrassDrawGroup[] _geometryDrawGroups;
		private int _grassPatchCount;
		private GrassPatch[,] _grassPatches;
		private ComputeBuffer _instanceToPatchIdDeltaTimeBuffer;
		private IdDeltaTime[] _instanceToPatchIdDeltaTimeData;

		private ComputeBuffer _patchDataBuffer;
		private Plane[] _planes;
		private BoundingPatch _rootPatch;
		private RenderTexture _simulationTexture0;
		private RenderTexture _simulationTexture1;

		private int _threadGroupX, _threadGroupY, _threadGroupZ;
		private List<GrassPatch> _visiblePatches;
		public Mesh BillboardCrossedDummyMesh;
		public Mesh BillboardScreenDummyMesh;
		public Mesh GeometryDummyMesh;

		public override void Unload()
		{
			if (_grassPatches != null)
				foreach (var grassPatch in _grassPatches)
					grassPatch.Unload();
			if (_visiblePatches != null) _visiblePatches.Clear();
			_grassPatches = null;
			DestroyImmediate(_simulationTexture0);
			DestroyImmediate(_simulationTexture1);
			if (_geometryDrawGroups != null)
				foreach (var geometryDrawGroup in _geometryDrawGroups)
					geometryDrawGroup.Unload();
			if (_billboardCrossedDrawGroups != null)
				foreach (var billboardCrossedDrawGroup in _billboardCrossedDrawGroups)
					billboardCrossedDrawGroup.Unload();
			if (_billboardScreenDrawGroups != null)
				foreach (var billboardScreenDrawGroup in _billboardScreenDrawGroups)
					billboardScreenDrawGroup.Unload();
			if (_patchDataBuffer != null) _patchDataBuffer.Release();
			if (_instanceToPatchIdDeltaTimeBuffer != null) _instanceToPatchIdDeltaTimeBuffer.Release();
		}

		public override Bounds GetBounds() { return _rootPatch.Bounds; }

		protected override void DrawImpl()
		{
			CullViewFrustum();
			foreach (var grassPatch in _grassPatches) grassPatch.DeltaTime += Time.deltaTime;
			foreach (var visiblePatch in _visiblePatches)
			{
				//Compute instanceCounts to determine LOD and register patch in appropriate drawgroup
				uint geometryInstanceCount, billboardCrossedInstanceCount, billboardScreenInstanceCount;
				visiblePatch.ComputeLod(out geometryInstanceCount, out billboardCrossedInstanceCount,
					out billboardScreenInstanceCount);

				if (geometryInstanceCount > 0)
					_geometryDrawGroups[geometryInstanceCount - 1].RegisterPatch(visiblePatch);
				if (billboardCrossedInstanceCount > 0)
					_billboardCrossedDrawGroups[billboardCrossedInstanceCount - 1].RegisterPatch(visiblePatch);
				if (billboardScreenInstanceCount > 0)
					_billboardScreenDrawGroups[billboardScreenInstanceCount - 1].RegisterPatch(visiblePatch);
			}

			//Update instanceToGrassPatchId Buffer
			
			//_instanceToPatchIdDeltaTimeData = new IdDeltaTime[/*_visiblePatches.Count*/_grassPatchCount];
			/*
			for (var i = 0; i < _instanceToPatchIdDeltaTimeData.Length; i++)
				_instanceToPatchIdDeltaTimeData[i] = new IdDeltaTime
				{
					Id = _visiblePatches[i].TextureIndex,
					DeltaTime = _visiblePatches[i].DeltaTime
					
				};
			*/
			int i = 0;
			foreach (var grassPatch in _visiblePatches)
			{
				_instanceToPatchIdDeltaTimeData[i].Id = grassPatch.TextureIndex;
				_instanceToPatchIdDeltaTimeData[i].DeltaTime = grassPatch.DeltaTime;
				i++;
			}
			//_instanceToPatchIdDeltaTimeBuffer.SetCounterValue((uint) _grassPatchCount/*_visiblePatches.Count*/);
			_instanceToPatchIdDeltaTimeBuffer.SetData(_instanceToPatchIdDeltaTimeData);
			/*Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysics, "InstanceToPatchIdDeltaTimeBuffer",
				_instanceToPatchIdDeltaTimeBuffer); //TODO: temporary test*/

			RunSimulation();

			//Reset visible patches delta Time
			foreach (var grassPatch in _visiblePatches) grassPatch.DeltaTime = 0;

			//Draw different lod representations in grouped draw call
			foreach (var geometryDrawGroup in _geometryDrawGroups) geometryDrawGroup.Draw();
			foreach (var billboardCrossedDrawGroup in _billboardCrossedDrawGroups) billboardCrossedDrawGroup.Draw();
			foreach (var billboardScreenDrawGroup in _billboardScreenDrawGroups) billboardScreenDrawGroup.Draw();
		}

		private void RunSimulation()
		{
			//uint threadGroupX, threadGroupY, threadGroupZ;
			//Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelPhysics, out threadGroupX, out threadGroupY, out threadGroupZ);

			_threadGroupZ = _visiblePatches.Count;

			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelPhysics, _threadGroupX, _threadGroupY, _threadGroupZ);
		}

		public override void SetupContainer()
		{
			//TODO: A progressBar would be nice
			_planes = new Plane[6];
			_visiblePatches = new List<GrassPatch>();
			CreateGrassPatchGrid();
			CreatePatchHierarchy();

			GeometryDummyMesh = CreateDummyMesh(Ctx.Settings.GetMinAmountBladesPerPatch());
			BillboardCrossedDummyMesh = CreateDummyMesh(Ctx.Settings.GetMinAmountBillboardsPerPatch() * 3);
			BillboardScreenDummyMesh = CreateDummyMesh(Ctx.Settings.GetMinAmountBillboardsPerPatch());

			CreateGpuTextureAndBuffer();
			CreateDrawGroups();
			SetupSimulation();
		}

		private static Mesh CreateDummyMesh(uint meshSize)
		{
			var dummyVertices = new Vector3[meshSize];
			var indices = new int[meshSize];

			for (var i = 0; i < meshSize; i++)
			{
				dummyVertices[i] = Vector3.zero;
				indices[i] = i;
			}

			var dummyMesh = new Mesh {vertices = dummyVertices};
			dummyMesh.SetIndices(indices, MeshTopology.Points, 0);
			dummyMesh.RecalculateBounds();
			return dummyMesh;
		}

		private void CreateDrawGroups()
		{
			_geometryDrawGroups = new GrassDrawGroup[Ctx.Settings.LodInstancesGeometry];
			_billboardCrossedDrawGroups = new GrassDrawGroup[Ctx.Settings.LodInstancesBillboardCrossed];
			_billboardScreenDrawGroups = new GrassDrawGroup[Ctx.Settings.LodInstancesBillboardScreen];
			for (uint i = 0; i < Ctx.Settings.LodInstancesGeometry; i++)
				_geometryDrawGroups[i] =
					new GrassDrawGroup(Ctx, Ctx.GrassGeometry, GeometryDummyMesh, _grassPatchCount, i + 1, true);

			for (uint i = 0; i < Ctx.Settings.LodInstancesBillboardCrossed; i++)
				_billboardCrossedDrawGroups[i] =
					new GrassDrawGroup(Ctx, Ctx.GrassBillboardCrossed, BillboardCrossedDummyMesh, _grassPatchCount,
						i + 1);

			for (uint i = 0; i < Ctx.Settings.LodInstancesBillboardScreen; i++)
				_billboardScreenDrawGroups[i] =
					new GrassDrawGroup(Ctx, Ctx.GrassBillboardScreen, BillboardScreenDummyMesh, _grassPatchCount,
						i + 1);
		}

		private void CreateGpuTextureAndBuffer()
		{
			_simulationTexture0 = new RenderTexture(Ctx.Settings.GetPerPatchTextureWidthHeight(),
				Ctx.Settings.GetPerPatchTextureWidthHeight(), 0,
				RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
			{
				filterMode = FilterMode.Bilinear,
				autoGenerateMips = false,
				useMipMap = false,
				enableRandomWrite = true,
				wrapMode = TextureWrapMode.Clamp,
				dimension = TextureDimension.Tex2DArray,
				volumeDepth = _grassPatchCount
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
				wrapMode = TextureWrapMode.Clamp,
				dimension = TextureDimension.Tex2DArray,
				volumeDepth = _grassPatchCount
			};
			_simulationTexture1.Create();

			_instanceToPatchIdDeltaTimeBuffer =
				new ComputeBuffer(_grassPatchCount, sizeof(uint) + sizeof(float), ComputeBufferType.Append);
			_instanceToPatchIdDeltaTimeBuffer.SetCounterValue((uint) _grassPatchCount);

			_instanceToPatchIdDeltaTimeData = new IdDeltaTime[_grassPatchCount];
			for (uint i = 0; i < _instanceToPatchIdDeltaTimeData.Length; i++)
				_instanceToPatchIdDeltaTimeData[i] = new IdDeltaTime {Id = i, DeltaTime = 0.1f};

			_instanceToPatchIdDeltaTimeBuffer.SetData(_instanceToPatchIdDeltaTimeData);

			/*
			 * TODO:
			 * Bind texture to shader glabally.
			 * Assing each grassapatch an index
			 * Create DrawGroups for each possible instanceIdCount of geometry, billboardcrossed and billboardscreen grass.
			 * Compute Lods of grasspatches and assign patch to matching drawgroup
			 * Run computeShader
			 * Update DrawGroups InstanceToPatchID Buffer and perform draw calls.
			 */

			//Create constant patchData Buffer
			_patchDataBuffer = new ComputeBuffer(_grassPatchCount, PatchData.GetSize(), ComputeBufferType.Default);
			var patchDataArray = new PatchData[_grassPatchCount];
			foreach (var grassPatch in _grassPatches) patchDataArray[grassPatch.TextureIndex] = grassPatch.PatchData;
			_patchDataBuffer.SetData(patchDataArray);

			Ctx.GrassGeometry.SetBuffer("PatchConstantBuffer", _patchDataBuffer);
			Ctx.GrassGeometry.SetTexture("SimulationTexture0", _simulationTexture0);
			Ctx.GrassGeometry.SetTexture("SimulationTexture1", _simulationTexture1);
			Shader.SetGlobalBuffer("PatchConstantBuffer", _patchDataBuffer);
			Shader.SetGlobalTexture("SimulationTexture0", _simulationTexture0);
			Shader.SetGlobalTexture("SimulationTexture1", _simulationTexture1);

			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture0", _simulationTexture0);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture1", _simulationTexture1);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysics, "PatchConstantBuffer", _patchDataBuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysics, "InstanceToPatchIdDeltaTimeBuffer",
				_instanceToPatchIdDeltaTimeBuffer);

			Ctx.GrassSimulationComputeShader.SetFloat("GrassDataResolution", Ctx.Settings.GrassDataResolution);
		}

		private void SetupSimulation()
		{
			//Set buffers for SimulationSetup Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture0",
				_simulationTexture0);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture1",
				_simulationTexture1);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelSimulationSetup, "PatchConstantBuffer",
				_patchDataBuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelSimulationSetup, "InstanceToPatchIdDeltaTimeBuffer",
				_instanceToPatchIdDeltaTimeBuffer);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelSimulationSetup, out threadGroupX,
				out threadGroupY, out threadGroupZ);
			_threadGroupX = (int) (Ctx.Settings.GrassDataResolution / threadGroupX);
			_threadGroupY = (int) (Ctx.Settings.GrassDataResolution / threadGroupY);
			_threadGroupZ = _grassPatchCount;

			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelSimulationSetup, _threadGroupX, _threadGroupY,
				_threadGroupZ);
		}

		private void CreateGrassPatchGrid()
		{
			//Transform terrain bounds center from local to world coordinates
			var localToWorldMatrix = Matrix4x4.TRS(Ctx.Transform.position, Quaternion.identity, Vector3.one);
			var terrainBoundsWorldCenter = localToWorldMatrix.MultiplyPoint3x4(Ctx.DimensionsInput.GetBounds().center);
			//Prepare some measurements
			var terrainSize = new Vector2(Ctx.DimensionsInput.GetWidth(), Ctx.DimensionsInput.GetDepth());
			var heightSamplingRate = Ctx.HeightInput.GetSamplingRate();
			var patchQuantity = new Vector2Int((int) (terrainSize.x / Ctx.Settings.PatchSize),
				(int) (terrainSize.y / Ctx.Settings.PatchSize));
			_grassPatches = new GrassPatch[patchQuantity.y, patchQuantity.x];
			_grassPatchCount = 0;

			//Initiate all Leaf Patches by creating their BoundingBox and textureCoordinates for heightmap Access
			for (var y = 0; y < patchQuantity.y; y++)
			for (var x = 0; x < patchQuantity.x; x++)
			{
				//Create patch texture coordinates in range 0..1, where x: xStart, y: yStart, z: width, w:height
				var patchTexCoord = new Vector4((float) x / patchQuantity.x, (float) y / patchQuantity.y,
					1f / patchQuantity.x,
					1f / patchQuantity.y);

				//Calculate bounding box center and size in world coordinates
				var patchBoundsCenter = new Vector3(
					terrainBoundsWorldCenter.x - Ctx.DimensionsInput.GetBounds().extents.x,
					Ctx.Transform.position.y,
					terrainBoundsWorldCenter.z - Ctx.DimensionsInput.GetBounds().extents.z);
				var patchBoundsSize = new Vector3(Ctx.Settings.PatchSize, 0, Ctx.Settings.PatchSize);
				//We can already calculate x and z positions
				patchBoundsCenter.x += x * Ctx.Settings.PatchSize + Ctx.Settings.PatchSize / 2;
				patchBoundsCenter.z += y * Ctx.Settings.PatchSize + Ctx.Settings.PatchSize / 2;

				var minHeight = float.PositiveInfinity;
				var maxHeight = float.NegativeInfinity;
				for (var j = patchTexCoord.x; j < patchTexCoord.x + patchTexCoord.z; j += heightSamplingRate.x)
				for (var k = patchTexCoord.y; k < patchTexCoord.y + patchTexCoord.w; k += heightSamplingRate.y)
				{
					var height = Ctx.HeightInput.GetHeight(j, k);
					if (height < minHeight) minHeight = height;
					if (height > maxHeight) maxHeight = height;
				}

				//We can now calculate the center.y and height of BoundingBox
				patchBoundsCenter.y += minHeight + (maxHeight - minHeight) / 2;
				patchBoundsSize.y = maxHeight - minHeight;

				//Create new patch and give it the data we just calculated
				_grassPatches[y, x] = new GrassPatch(Ctx, patchTexCoord,
					new Bounds(patchBoundsCenter, patchBoundsSize), (uint) _grassPatchCount);
				_grassPatchCount++;
			}
		}

		private void CreatePatchHierarchy()
		{
			var patchHierarchy = Combine2X2Patches(_grassPatches);

			while (patchHierarchy.Length > 1) patchHierarchy = Combine2X2Patches(patchHierarchy);
			_rootPatch = patchHierarchy[0, 0];
		}

		private BoundingPatch[,] Combine2X2Patches(Patch[,] patchesInput)
		{
			var newRows = (patchesInput.GetLength(0) + 1) / 2;
			var newCols = (patchesInput.GetLength(1) + 1) / 2;
			var patchesOutput = new BoundingPatch[newRows, newCols];

			for (var y = 0; y < newRows; y++)
			for (var x = 0; x < newCols; x++)
			{
				var hierarchicalPatch = new BoundingPatch(Ctx);
				for (var k = 0; k <= 1; k++)
				for (var j = 0; j <= 1; j++)
				{
					var row = 2 * y + k;
					var col = 2 * x + j;

					if (row < patchesInput.GetLength(0) && col < patchesInput.GetLength(1))
						hierarchicalPatch.AddChild(patchesInput[row, col]);
					else hierarchicalPatch.AddChild(null);
				}

				patchesOutput[y, x] = hierarchicalPatch;
			}

			return patchesOutput;
		}

		protected override void DrawGizmoImpl()
		{
			//Draw Gizmos for Hierchical Patches
			_rootPatch.DrawGizmo(0);
			//Draw Gizmos for visible Leaf Patches
			foreach (var visiblePatch in _visiblePatches) visiblePatch.DrawGizmo();
		}

		public override void OnGUI() { }

		private void CullViewFrustum()
		{
			_visiblePatches.Clear();
			GeometryUtility.CalculateFrustumPlanes(Ctx.Camera, _planes);

			TestViewFrustum(_rootPatch);
		}

		private void TestViewFrustum(Patch patch)
		{
			//TODO: Test performance of this. If a bound is completely in frustum one could add all childs to visiblePatch List instead of testing them all 
			if (!GeometryUtility.TestPlanesAABB(_planes, patch.Bounds)) return;
			if (patch.IsLeaf)
			{
				_visiblePatches.Add(patch as GrassPatch);
			} else
			{
				var childPatches = ((BoundingPatch) patch).ChildPatches;
				if (childPatches == null) return;
				foreach (var childPatch in childPatches)
					if (childPatch != null)
						TestViewFrustum(childPatch);
			}
		}

		public override void GetDebugInfo(ref int visiblePatchCount, ref int simulatedGrassCount,
			ref int geometryGrassCount, ref int crossedBillboardGrassCount, ref int screenBillboardGrassCount,
			ref int geometryPatchCount, ref int crossedBillboardPatchCount, ref int screenBillboardPatchCount)
		{
			foreach (var visiblePatch in _visiblePatches)
			{
				visiblePatchCount++;
				simulatedGrassCount += Ctx.Settings.GrassDataResolution * Ctx.Settings.GrassDataResolution;
				geometryGrassCount +=
					(int) visiblePatch.GeometryInstanceCount * (int) Ctx.Settings.GetMinAmountBladesPerPatch();
				if (visiblePatch.GeometryInstanceCount > 0)
				{
					geometryPatchCount += 1;
					geometryGrassCount -= (int) ((int) Ctx.Settings.GetMinAmountBladesPerPatch() * 0.5f);
				}

				crossedBillboardGrassCount += (int) visiblePatch.BillboardCrossedInstanceCount *
				                              (int) Ctx.Settings.GetMinAmountBillboardsPerPatch();
				if (visiblePatch.BillboardCrossedInstanceCount > 0)
				{
					crossedBillboardPatchCount += 1;
					crossedBillboardGrassCount -= (int) ((int) Ctx.Settings.GetMinAmountBillboardsPerPatch() * 0.5f);
				}

				screenBillboardGrassCount += (int) visiblePatch.BillboardScreenInstanceCount *
				                             (int) Ctx.Settings.GetMinAmountBillboardsPerPatch();
				if (visiblePatch.BillboardScreenInstanceCount > 0)
				{
					screenBillboardPatchCount += 1;
					screenBillboardGrassCount -= (int) ((int) Ctx.Settings.GetMinAmountBillboardsPerPatch() * 0.5f);
				}
			}
		}
	}
}