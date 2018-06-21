using System.Collections.Generic;
using GrassSimulation.Core.Lod;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrassSimulation.StandardContainers
{
	public class UniformGridHierarchyPatchContainer : PatchContainer
	{
		private int _grassPatchCount;
		private GrassPatch[,] _grassPatches;
		private Plane[] _planes;
		private BoundingPatch _rootPatch;
		private List<GrassPatch> _visiblePatches;
		private RenderTexture _simulationTexture0;
		private RenderTexture _simulationTexture1;
	
		public override void Unload()
		{
			if (_grassPatches != null) foreach (var grassPatch in _grassPatches) grassPatch.Unload();
			if (_visiblePatches != null) _visiblePatches.Clear();
			_grassPatches = null;
			Object.DestroyImmediate(_simulationTexture0);
			Object.DestroyImmediate(_simulationTexture1);
		}

		public override Bounds GetBounds() { return _rootPatch.Bounds; }

		protected override void DrawImpl()
		{
			CullViewFrustum();
			foreach (var grassPatch in _grassPatches) grassPatch.DeltaTime += Time.deltaTime;
			foreach (var visiblePatch in _visiblePatches) visiblePatch.Draw();
		}

		public override void SetupContainer()
		{
			//TODO: A progressBar would be nice
			_planes = new Plane[6];
			_visiblePatches = new List<GrassPatch>();
			CreateGrassPatchGrid();
			CreatePatchHierarchy();
			
			CreateGpuTextureAndBuffer();
			SetupSimulation();

		}

		private ComputeBuffer _patchDataBuffer;
		private ComputeBuffer _argBuffer;
		private ComputeBuffer _appendBuffer;
		private uint[] _argsBuffer = {0, 0, 0, 0, 0};
		
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
			
			_patchDataBuffer = new ComputeBuffer(_grassPatchCount, PatchData.GetSize(), ComputeBufferType.Default);
			var patchDataArray = new PatchData[_grassPatchCount];
			for (int y = 0; y < _grassPatches.GetLength(0); y++)
			for (int x = 0; x < _grassPatches.GetLength(1); x++)
			{
				int i = y * _grassPatches.GetLength(1) + x;
				patchDataArray[i] = _grassPatches[x,y].PatchData;	//TODO: possible error source
			}
			_patchDataBuffer.SetData(patchDataArray);
			
			_appendBuffer = new ComputeBuffer(_grassPatchCount, sizeof(int), ComputeBufferType.Append);
			_appendBuffer.SetCounterValue(0);
			
			_argBuffer = new ComputeBuffer(1, _argsBuffer.Length * sizeof(int), ComputeBufferType.IndirectArguments);
			
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture0", _simulationTexture0);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture1", _simulationTexture1);
			
		}

		private void SetupSimulation()
		{
			Ctx.GrassSimulationComputeShader.SetInt("StartIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetX", _parameterOffsetX);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetY", _parameterOffsetY);
			Ctx.GrassSimulationComputeShader.SetVector("PatchTexCoord", _patchTexCoord);
			Ctx.GrassSimulationComputeShader.SetMatrix("PatchModelMatrix", _patchModelMatrix);

			//Set buffers for SimulationSetup Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture0",
				_simulationTexture0);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture1",
				_simulationTexture1);
			//Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "NormalHeightTexture", _normalHeightTexture);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelSimulationSetup, out threadGroupX,
				out threadGroupY, out threadGroupZ);
			_threadGroupX = (int) (Ctx.Settings.GrassDataResolution / threadGroupX);
			_threadGroupY = (int) (Ctx.Settings.GrassDataResolution / threadGroupY);
			_threadGroupZ = 1;

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
					new Bounds(patchBoundsCenter, patchBoundsSize), y * patchQuantity.x + x);
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

		public override void OnGUI() {  }

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
				foreach (var childPatch in childPatches) if (childPatch != null) TestViewFrustum(childPatch);
			}
		}

		public override void GetDebugInfo(ref int visiblePatchCount, ref int simulatedGrassCount, ref int geometryGrassCount, ref int crossedBillboardGrassCount, ref int screenBillboardGrassCount, ref int geometryPatchCount, ref int crossedBillboardPatchCount, ref int screenBillboardPatchCount)		{
			foreach (var visiblePatch in _visiblePatches)
			{
				visiblePatchCount++;
				simulatedGrassCount += Ctx.Settings.GrassDataResolution * Ctx.Settings.GrassDataResolution;
				geometryGrassCount += (int) visiblePatch._argsGeometry[1] * (int) Ctx.Settings.GetMinAmountBladesPerPatch();
				if (visiblePatch._argsGeometry[1] > 0)
				{
					geometryPatchCount += 1;
					geometryGrassCount -= (int)((int) Ctx.Settings.GetMinAmountBladesPerPatch() * 0.5f);
				}

				crossedBillboardGrassCount += (int) visiblePatch._argsBillboardCrossed[1] * (int) Ctx.Settings.GetMinAmountBillboardsPerPatch();
				if (visiblePatch._argsBillboardCrossed[1] > 0)
				{
					crossedBillboardPatchCount += 1;
					crossedBillboardGrassCount -= (int)((int) Ctx.Settings.GetMinAmountBillboardsPerPatch() * 0.5f);
				}

				screenBillboardGrassCount += (int) visiblePatch._argsBillboardScreen[1] * (int) Ctx.Settings.GetMinAmountBillboardsPerPatch();
				if (visiblePatch._argsBillboardScreen[1] > 0)
				{
					screenBillboardPatchCount += 1;
					screenBillboardGrassCount -= (int)((int) Ctx.Settings.GetMinAmountBillboardsPerPatch() * 0.5f);
				}
			}
			
			
		}
	}
}