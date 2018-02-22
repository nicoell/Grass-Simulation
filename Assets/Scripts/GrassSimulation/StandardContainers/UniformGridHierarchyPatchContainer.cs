using System.Collections.Generic;
using GrassSimulation.Core.Lod;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

namespace GrassSimulation.StandardContainers
{
	public class UniformGridHierarchyPatchContainer : PatchContainer
	{
		private GrassPatch[,] _grassPatches;
		private Plane[] _planes;
		private BoundingPatch _rootPatch;
		private List<GrassPatch> _visiblePatches;

		public override void Destroy()
		{
			foreach (var grassPatch in _grassPatches) grassPatch.Destroy();
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
					new Bounds(patchBoundsCenter, patchBoundsSize));
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
			_rootPatch.DrawGizmo();
			//Draw Gizmos for visible Leaf Patches
			foreach (var visiblePatch in _visiblePatches) visiblePatch.DrawGizmo();
		}

		public override void OnGUI()
		{
			if (_visiblePatches.Count > 0) _visiblePatches[0].OnGUI();
		}

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

		public override void GetDebugInfo(ref int visiblePatchCount, ref int simulatedGrassCount, ref int geometryGrassCount, ref int crossedBillboardGrassCount, ref int screenBillboardGrassCount)
		{
			foreach (var visiblePatch in _visiblePatches)
			{
				visiblePatchCount++;
				simulatedGrassCount += Ctx.Settings.GrassDataResolution * Ctx.Settings.GrassDataResolution;
				geometryGrassCount += (int) visiblePatch._argsGeometry[1] * (int) Ctx.Settings.GetMinAmountBladesPerPatch();
				if (visiblePatch._argsGeometry[1] > 0)
				{
					geometryGrassCount -= (int)((int) Ctx.Settings.GetMinAmountBladesPerPatch() *
					                      (int) Ctx.Settings.LodGeometryTransitionSegments * 0.5f);
				}
				crossedBillboardGrassCount += (int) visiblePatch._argsBillboardCrossed[1] * (int) Ctx.Settings.GetMinAmountBillboardsPerPatch();
				if (visiblePatch._argsBillboardCrossed[1] > 0)
				{
					crossedBillboardGrassCount -= (int)((int) Ctx.Settings.GetMinAmountBillboardsPerPatch() *
					                            (int) Ctx.Settings.LodBillboardCrossedTransitionSegments * 0.5f);
				}
				screenBillboardGrassCount += (int) visiblePatch._argsBillboardScreen[1] * (int) Ctx.Settings.GetMinAmountBillboardsPerPatch();
				if (visiblePatch._argsBillboardScreen[1] > 0)
				{
					screenBillboardGrassCount -= (int)((int) Ctx.Settings.GetMinAmountBillboardsPerPatch() *
					                            (int) Ctx.Settings.LodBillboardScreenTransitionSegments * 0.5f);
				}
			}
			
			
		}
	}
}