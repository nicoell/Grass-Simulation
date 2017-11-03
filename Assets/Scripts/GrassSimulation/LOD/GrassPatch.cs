using UnityEngine;
using Random = System.Random;

namespace GrassSimulation.LOD
{
	public class GrassPatch : Patch
	{
		private readonly Vector4 _patchTexCoord; //x: xStart, y: yStart, z: width, w:height
		private readonly int _startIndex;
		private Matrix4x4 _matrix;
		private readonly Random _random;

		public GrassPatch(SimulationContext context, Vector4 patchTexCoord, Bounds bounds) : base(context)
		{
			Bounds = bounds;
			_patchTexCoord = patchTexCoord;
			_startIndex =
				(int) UnityEngine.Random.Range(0,
					Context.Settings.GetAmountPrecomputedBlades() - Context.Settings.GetAmountBlades() - 1);
			_random = new Random(Context.Settings.RandomSeed);
			var translate = bounds.center - bounds.extents;
			translate.y = Context.Transform.position.y;
			_matrix = Matrix4x4.TRS(translate, Quaternion.identity,
				new Vector3(Context.Settings.PatchSize, Context.Terrain.terrainData.size.y, Context.Settings.PatchSize));
			GeneratePerBladeData();
		}

		public Vector4[] GrassDataA { get; private set; } //bladeUp.xyz, position.y
		public Vector4[] GrassDataB { get; private set; } //bladeV1.xyz, height
		public Vector4[] GrassDataC { get; private set; } //bladeV2.xyz, dirAlpha

		public override bool IsLeaf
		{
			get { return true; }
		}

		public void GeneratePerBladeData()
		{
			GrassDataA = new Vector4[Context.Settings.GetAmountBlades()];
			GrassDataB = new Vector4[Context.Settings.GetAmountBlades()];
			GrassDataC = new Vector4[Context.Settings.GetAmountBlades()];
			for (var i = 0; i < Context.Settings.GetAmountBlades(); i++)
			{
				//Fill GrassDataA
				var bladePosition =
					new Vector2(_patchTexCoord.x + _patchTexCoord.z * Context.SharedGrassData.GrassData[_startIndex + i].x,
						_patchTexCoord.y + _patchTexCoord.w * Context.SharedGrassData.GrassData[_startIndex + i].y);
				var posY = Context.Heightmap.GetPixel((int) (bladePosition.x * Context.Heightmap.width),
					(int) (bladePosition.y * Context.Heightmap.height)).r;
				var up = Context.Terrain.terrainData.GetInterpolatedNormal(bladePosition.x, bladePosition.y);
				GrassDataA[i].Set(up.x, up.y, up.z, posY);
				//Fill GrassDataB
				var height = (float) (Context.Settings.BladeMinHeight +
				                      _random.NextDouble() * (Context.Settings.BladeMaxHeight - Context.Settings.BladeMinHeight));
				GrassDataB[i].Set(up.x * height, up.y * height, up.z * height, height);
				//Fill GrassDataC
				var dirAlpha = (float) (_random.NextDouble() * Mathf.PI * 2f);
				GrassDataC[i].Set(up.x * height, up.y * height, up.z * height, dirAlpha);
			}
		}

		public override void DrawGizmo()
		{
			if (Context.EditorSettings.DrawGrassPatchGizmo)
			{
				Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
				Gizmos.DrawWireSphere(Bounds.center, 0.5f);
				Gizmos.DrawWireCube(Bounds.center, Bounds.size);
			}
			if (Context.EditorSettings.DrawGrassDataGizmo)
			{
				Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
				for (var i = 0; i < Context.Settings.GetAmountBlades(); i++)
				{
					var pos = new Vector3(Context.SharedGrassData.GrassData[_startIndex + i].x,
						GrassDataA[i].w, Context.SharedGrassData.GrassData[_startIndex + i].y);
					pos = _matrix.MultiplyPoint3x4(pos);
					Gizmos.DrawLine(pos, pos + new Vector3(GrassDataB[i].x, GrassDataB[i].y, GrassDataB[i].z));
				}
			}
		}
	}
}