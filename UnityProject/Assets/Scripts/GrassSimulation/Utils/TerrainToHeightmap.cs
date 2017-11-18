using UnityEngine;

namespace GrassSimulation.Utils
{
	public static class Terrain
	{
		public static Texture2D CreateHeightmapFromTerrain(UnityEngine.Terrain terrain)
		{
			var terrainHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth,
				terrain.terrainData.heightmapHeight);
			var heightmapTexture = new Texture2D(terrain.terrainData.heightmapWidth,
				terrain.terrainData.heightmapHeight,
				TextureFormat.RGBA32, false);
			for (var y = 0; y < heightmapTexture.height; y++)
			for (var x = 0; x < heightmapTexture.width; x++)
			{
				var color = new Color(terrainHeights[y, x], terrainHeights[y, x], terrainHeights[y, x], 1.0f);
				heightmapTexture.SetPixel(x, y, color);
			}
			heightmapTexture.Apply();
			heightmapTexture.name = terrain.name + "_Heightmap";
			return heightmapTexture;
		}
	}

	public static class Common
	{
		public static float Smoothstep(float from, float to, float t)
		{
			t = Mathf.Clamp01((t - from) / (to - from));
			return t * t * (3 - 2*t);
		}

		public static float Smootherstep(float from, float to, float t)
		{
			t = Mathf.Clamp01((t - from) / (to - from));
			return t * t * t * (t * (t * 6 - 15) + 10);
		}
	}

	public static class Bounds
	{
		public class BoundsVertices
		{
			public Vector3[] BoundVertices;
			
			public BoundsVertices(UnityEngine.Bounds bounds)
			{
				BoundVertices = new Vector3[8];
				BoundVertices[0] = bounds.min;
				BoundVertices[1] = bounds.max;
				BoundVertices[2] = new Vector3(BoundVertices[0].x, BoundVertices[0].y, BoundVertices[1].z);
				BoundVertices[3] = new Vector3(BoundVertices[0].x, BoundVertices[1].y, BoundVertices[0].z);
				BoundVertices[4] = new Vector3(BoundVertices[1].x, BoundVertices[0].y, BoundVertices[0].z);
				BoundVertices[5] = new Vector3(BoundVertices[0].x, BoundVertices[1].y, BoundVertices[1].z);
				BoundVertices[6] = new Vector3(BoundVertices[1].x, BoundVertices[0].y, BoundVertices[1].z);
				BoundVertices[7] = new Vector3(BoundVertices[1].x, BoundVertices[1].y, BoundVertices[0].z);
			}

			public static bool IntersectsSphere(BoundsVertices vertices, Vector3 center, float radius)
			{
				foreach (var vertex in vertices.BoundVertices)
				{
					if (Vector3.Distance(vertex, center) <= radius) return true;
				}
				return false;
			}
		}
	}
}