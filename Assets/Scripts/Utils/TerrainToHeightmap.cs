using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Terrain))]
public class TerrainToHeightmap : MonoBehaviour
{
	public void Convert()
	{
		var terrain = GetComponent<Terrain>();

		var terrainHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth,
			terrain.terrainData.heightmapHeight);
		var heightmapTexture = new Texture2D(terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight,
			TextureFormat.RGBA32, false);
		for (var y = 0; y < heightmapTexture.height; y++)
		for (var x = 0; x < heightmapTexture.width; x++)
		{
			var color = new Color(terrainHeights[y, x], terrainHeights[y, x], terrainHeights[y, x], 1.0f);
			heightmapTexture.SetPixel(x, y, color);
		}
		heightmapTexture.Apply();
		var pngBytes = heightmapTexture.EncodeToPNG();
		File.WriteAllBytes(Application.dataPath + "/Textures/heightmap_" + terrain.name + ".png", pngBytes);
	}
}