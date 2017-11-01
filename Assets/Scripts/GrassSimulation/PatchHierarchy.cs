using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrassSim {
	public class PatchHierarchy
	{
		private readonly Settings m_settings;
		private readonly TerrainData m_terrainData;
		private readonly Transform m_transform;
		private Texture2D m_heightmap;
		private Patch[,] m_leafPatches;
		private BoundingPatch m_rootPatch;
		private List<Patch> m_visiblePatches;

		public PatchHierarchy(Settings settings, TerrainData terrainData, Transform transform)
		{
			if (terrainData == null) throw new ArgumentNullException("terrainData");
			if (transform == null) throw new ArgumentNullException("transform");

			m_settings = settings;
			m_terrainData = terrainData;
			m_transform = transform;
			m_visiblePatches = new List<Patch>();
			CreateHeightmapFromTerrainData();
			CreatePatchLeaves();
			CreatePatchHierarchy();
		}

		private void CreateHeightmapFromTerrainData()
		{
			var terrainHeights = m_terrainData.GetHeights(0, 0, m_terrainData.heightmapWidth,
				m_terrainData.heightmapHeight);
			m_heightmap = new Texture2D(m_terrainData.heightmapWidth, m_terrainData.heightmapHeight,
				TextureFormat.RGBA32, false);
			for (var y = 0; y < m_heightmap.height; y++)
			for (var x = 0; x < m_heightmap.width; x++)
			{
				var color = new Color(terrainHeights[y, x], terrainHeights[y, x], terrainHeights[y, x], 1.0f);
				m_heightmap.SetPixel(x, y, color);
			}
			m_heightmap.Apply();
			//var pngBytes = heightmapTexture.EncodeToPNG();
			//File.WriteAllBytes(Application.dataPath + "/Textures/heightmap_terraindata.png", pngBytes);
		}

		private void CreatePatchLeaves()
		{
			//Transform terrain bounds center from local to world coordinates
			var localToWorldMatrix = Matrix4x4.TRS(m_transform.position, Quaternion.identity, Vector3.one);
			var terrainBoundsWorldCenter = localToWorldMatrix.MultiplyPoint3x4(m_terrainData.bounds.center);
			//Prepare some measurements
			var terrainSize = new Vector2(m_terrainData.size.x, m_terrainData.size.z);
			var terrainLevel = m_terrainData.size.y;
			var heightmapSize = new Vector2Int(m_terrainData.heightmapWidth, m_terrainData.heightmapHeight);
			//var heightmapToTerrainFactor = new Vector2(heightmapSize.x / terrainSize.x, heightmapSize.y / terrainSize.y);
			var patchQuantity = new Vector2Int((int) (terrainSize.x / m_settings.patchSize), (int) (terrainSize.y / m_settings.patchSize));
			m_leafPatches = new Patch[patchQuantity.y, patchQuantity.x];

			//Initiate all Leaf Patches by creating their BoundingBox and textureCoordinates for heightmap Access
			for (var y = 0; y < patchQuantity.y; y++)
			for (var x = 0; x < patchQuantity.x; x++)
			{
				//Create patch texture coordinates in range 0..1, where x: xStart, y: yStart, z: width, w:height
				var patchTexCoord = new Vector4((float) x / patchQuantity.x, (float) y / patchQuantity.y, 1f / patchQuantity.x,
					1f / patchQuantity.y);

				//Calculate bounding box center and size in world coordinates
				var patchBoundsCenter = new Vector3(
					terrainBoundsWorldCenter.x - m_terrainData.bounds.extents.x,
					m_transform.position.y,
					terrainBoundsWorldCenter.z - m_terrainData.bounds.extents.z);
				var patchBoundsSize = new Vector3(m_settings.patchSize, 0, m_settings.patchSize);
				//We can already calculate x and y positions
				patchBoundsCenter.x += x * m_settings.patchSize + m_settings.patchSize/2;
				patchBoundsCenter.z += y * m_settings.patchSize + m_settings.patchSize/2;

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
					var height = m_heightmap.GetPixel(j, k);
					if (height.r < minHeight) minHeight = height.r;
					if (height.r > maxHeight) maxHeight = height.r;
				}
				//We can now calculate the center.y and height of BoundingBox
				patchBoundsCenter.y += (minHeight + (maxHeight - minHeight) / 2) * terrainLevel;
				patchBoundsSize.y = (maxHeight - minHeight) * terrainLevel;

				//Create new patch and give it the data we just calculated
				m_leafPatches[y, x] = new Patch(patchTexCoord, new Bounds(patchBoundsCenter, patchBoundsSize));
			}
		}

		private void CreatePatchHierarchy()
		{
			var patchHierarchy = Combine2X2Patches(m_leafPatches);
			
			while (patchHierarchy.Length > 1)
			{
				patchHierarchy = Combine2X2Patches(patchHierarchy);
			}
			m_rootPatch = patchHierarchy[0,0];
		}

		private BoundingPatch[,] Combine2X2Patches(APatch[,] patchesInput)
		{
			var newRows = (patchesInput.GetLength(0) + 1) / 2;
			var newCols = (patchesInput.GetLength(1) + 1) / 2;
			var patchesOutput = new BoundingPatch[newRows, newCols];

			for (var y = 0; y < newRows; y++)
			for (var x = 0; x < newCols; x++)
			{
				var hierarchicalPatch = new BoundingPatch();
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
			m_rootPatch.DrawGizmo();
			//Draw Gizmos for visible Leaf Patches
			foreach (var visiblePatch in m_visiblePatches)
			{
				visiblePatch.DrawGizmo();
			}
		}

		public void CullViewFrustum(Camera camera)
		{
			m_visiblePatches.Clear();
			var vfPlanes = GeometryUtility.CalculateFrustumPlanes(camera);

			TestViewFrustum(vfPlanes, m_rootPatch);
		}

		private void TestViewFrustum(Plane[] vfPlanes, APatch patch)
		{
			if (!GeometryUtility.TestPlanesAABB(vfPlanes, patch.Bounds)) return;
			if (patch.IsLeaf)
			{
				m_visiblePatches.Add(patch as Patch);
			} else
			{
				foreach (var childPatch in (patch as BoundingPatch).ChildPatches)
				{
					if (childPatch != null) TestViewFrustum(vfPlanes, childPatch);
				}
				
			}
		}
	}
}