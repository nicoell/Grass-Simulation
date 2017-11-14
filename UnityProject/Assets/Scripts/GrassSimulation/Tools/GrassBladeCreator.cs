using System.IO;
using UnityEngine;

namespace GrassSimulation.Tools
{
	public class GrassBladeCreator : MonoBehaviour
	{
		[Tooltip("Rotation around the blades center axis")]
		public AnimationCurve BladeCurveRot;

		[Tooltip("Curvature for the blades sides")]
		public AnimationCurve BladeCurveU;

		[Tooltip("Curvature along the blades length")]
		public AnimationCurve BladeCurveV;

		public string BladeName = "Default";

		public void GenerateBlade()
		{
			var bladeTexture = new Texture2D(2, 64, TextureFormat.RGBA32, true, true)
			{
				name = BladeName,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
				anisoLevel = 0
			};
			var colors = new Color[bladeTexture.height * 2];
			for (var y = 0; y < bladeTexture.height; y++)
			//for (var x = 0; x < bladeTexture.width; x++)
			{
				var r = Mathf.Clamp(BladeCurveU.Evaluate((y + float.Epsilon) / bladeTexture.height), 0f, 1f);
				var g = Mathf.Clamp(BladeCurveV.Evaluate((y + float.Epsilon) / bladeTexture.height), 0f, 1f);
				var b = Mathf.Clamp(BladeCurveRot.Evaluate((y + float.Epsilon) / bladeTexture.height), 0f, 1f);
				colors[2*y + 1] = colors[2*y] = new Color(r, g, b, 1f);
			}
			bladeTexture.SetPixels(colors);
			bladeTexture.Apply();

			Debug.Log("Generated " + BladeName + " with " + bladeTexture.mipmapCount + "mipmap levels.");


			var bytes = bladeTexture.EncodeToPNG();
			File.WriteAllBytes(Application.dataPath + "/../Assets/Textures/" + BladeName + ".png", bytes);
		}
	}
}