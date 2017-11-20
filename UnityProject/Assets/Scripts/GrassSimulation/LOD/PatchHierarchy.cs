using System.Collections.Generic;
using UnityEngine;

namespace GrassSimulation.LOD
{
	//TODO: Decouple from terrain
	public class PatchHierarchy : RequiredContext, IInitializable, IDestroyable, IDrawable
	{
		private GrassPatch[,] _grassPatches;
		private Plane[] _planes;
		private BoundingPatch _rootPatch;
		private List<GrassPatch> _visiblePatches;

		public PatchHierarchy(SimulationContext ctx) : base(ctx)
		{
		}

		public void Destroy()
		{
			foreach (var grassPatch in _grassPatches)
				grassPatch.Destroy();
		}

		public void Draw()
		{
			CullViewFrustum();
			UpdatePerFrameData();
			foreach (var visiblePatch in _visiblePatches)
				visiblePatch.Draw();
		}

		public void DebugDraw()
		{
			if (_visiblePatches.Count > 1)
			{
				GUI.DrawTexture(new Rect(512, 0, 256, 256), _visiblePatches[1]._normalHeightTexture);
			} 
		}

		public bool Init()
		{
			//TODO: A progressBar would be nice
			_planes = new Plane[6];
			_visiblePatches = new List<GrassPatch>();
			CreateGrassPatchGrid();
			CreatePatchHierarchy();

			return true;
		}

		private void CreateGrassPatchGrid()
		{
			//Transform terrain bounds center from local to world coordinates
			var localToWorldMatrix = Matrix4x4.TRS(Ctx.Transform.position, Quaternion.identity, Vector3.one);
			var terrainBoundsWorldCenter = localToWorldMatrix.MultiplyPoint3x4(Ctx.Terrain.terrainData.bounds.center);
			//Prepare some measurements
			var terrainSize = new Vector2(Ctx.Terrain.terrainData.size.x, Ctx.Terrain.terrainData.size.z);
			var terrainLevel = Ctx.Terrain.terrainData.size.y;
			var heightmapSize = new Vector2Int(Ctx.Terrain.terrainData.heightmapWidth,
				Ctx.Terrain.terrainData.heightmapHeight);
			//var heightmapToTerrainFactor = new Vector2(heightmapSize.x / terrainSize.x, heightmapSize.y / terrainSize.y);
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
					terrainBoundsWorldCenter.x - Ctx.Terrain.terrainData.bounds.extents.x,
					Ctx.Transform.position.y,
					terrainBoundsWorldCenter.z - Ctx.Terrain.terrainData.bounds.extents.z);
				var patchBoundsSize = new Vector3(Ctx.Settings.PatchSize, 0, Ctx.Settings.PatchSize);
				//We can already calculate x and z positions
				patchBoundsCenter.x += x * Ctx.Settings.PatchSize + Ctx.Settings.PatchSize / 2;
				patchBoundsCenter.z += y * Ctx.Settings.PatchSize + Ctx.Settings.PatchSize / 2;

				//Sample heightmapTexture to find min and maxheight values of current patch
				var minHeight = 1f;
				var maxHeight = 0f;
				for (var j = (int) (patchTexCoord.x * heightmapSize.x);
					j < (patchTexCoord.x + patchTexCoord.z) * heightmapSize.x;
					j++)
				for (var k = (int) (patchTexCoord.y * heightmapSize.y);
					k < (patchTexCoord.y + patchTexCoord.w) * heightmapSize.y;
					k++)
				{
					var height = Ctx.Heightmap.GetPixel(j, k);
					if (height.r < minHeight) minHeight = height.r;
					if (height.r > maxHeight) maxHeight = height.r;
				}
				//We can now calculate the center.y and height of BoundingBox
				patchBoundsCenter.y += (minHeight + (maxHeight - minHeight) / 2) * terrainLevel;
				patchBoundsSize.y = (maxHeight - minHeight) * terrainLevel;

				//TODO: Tessellated grass may exceed this bounds, need to add some tolerance

				//Create new patch and give it the data we just calculated
				_grassPatches[y, x] = new GrassPatch(Ctx, patchTexCoord,
					new Bounds(patchBoundsCenter, patchBoundsSize));
			}
		}

		private void CreatePatchHierarchy()
		{
			var patchHierarchy = Combine2X2Patches(_grassPatches);

			while (patchHierarchy.Length > 1)
				patchHierarchy = Combine2X2Patches(patchHierarchy);
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

		public void DrawGizmo()
		{
			//Draw Gizmos for Hierchical Patches
			_rootPatch.DrawGizmo();
			//Draw Gizmos for visible Leaf Patches
			foreach (var visiblePatch in _visiblePatches)
				visiblePatch.DrawGizmo();
		}

		private void CullViewFrustum()
		{
			_visiblePatches.Clear();
			GeometryUtility.CalculateFrustumPlanes(Ctx.Camera, _planes);

			TestViewFrustum(_rootPatch);
		}

		private void TestViewFrustum(Patch patch)
		{
			if (!GeometryUtility.TestPlanesAABB(_planes, patch.Bounds)) return;
			if (patch.IsLeaf)
			{
				_visiblePatches.Add(patch as GrassPatch);
			}
			else
			{
				var childPatches = ((BoundingPatch) patch).ChildPatches;
				if (childPatches == null) return;
				foreach (var childPatch in childPatches)
					if (childPatch != null) TestViewFrustum(childPatch);
			}
		}

		private void UpdatePerFrameData()
		{
			//TODO: Maybe outsource all the computeshader data settings to its own class
			Ctx.GrassSimulationComputeShader.SetBool("applyTransition", Ctx.Settings.EnableHeightTransition);
			Ctx.GrassMaterial.SetVector("camPos", Ctx.Camera.transform.position);
			Ctx.GrassSimulationComputeShader.SetFloat("deltaTime", Time.deltaTime);
			Ctx.GrassSimulationComputeShader.SetVector("gravityVec", Ctx.Settings.Gravity);
			Ctx.GrassSimulationComputeShader.SetMatrix("viewProjMatrix",
				Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
			Ctx.GrassSimulationComputeShader.SetFloats("camPos", Ctx.Camera.transform.position.x,
				Ctx.Camera.transform.position.y, Ctx.Camera.transform.position.z);
		}
	}
}