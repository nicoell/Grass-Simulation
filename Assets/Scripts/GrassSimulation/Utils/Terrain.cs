using UnityEngine;

namespace GrassSimulation.Utils {
	public static class Terrain
	{
		public static Texture2D CreateHeightmapFromTerrain(UnityEngine.Terrain terrain)
		{
			var terrainHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth,
				terrain.terrainData.heightmapHeight);
			var heightmapTexture = new Texture2D(terrain.terrainData.heightmapWidth,
				terrain.terrainData.heightmapHeight,
				TextureFormat.RGBA32, false)
			{
				
				filterMode = FilterMode.Trilinear
			};
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
}