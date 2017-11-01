using GrassSim.Grass;
using UnityEngine;

namespace GrassSim
{
	public class Patch : APatch
	{
		private readonly Settings m_settings;
		private readonly TerrainData m_terrainData;
		private Vector4 m_patchTexCoord;
		private int m_startIndex;
		
		public Vector4[] GrassDataA { get; private set; } //bladeUp.xyz, position.y
		public Vector4[] GrassDataB { get; private set; } //bladeV1.xyz, height
		public Vector4[] GrassDataC { get; private set; } //bladeV2.xyz, dirAlpha

		public Patch(Settings settings, TerrainData terrainData, Vector4 patchTexCoord, Bounds bounds)
		{
			Bounds = bounds;
			m_settings = settings;
			m_terrainData = terrainData;
			m_patchTexCoord = patchTexCoord;
			m_startIndex = (int) Random.Range(0, settings.GetAmountPrecomputedBlades() - settings.GetAmountBlades() - 1);
		}

		public void GeneratePerBladeData()
		{
			for (int i = m_startIndex; i < m_settings.GetAmountBlades(); i++)
			{
				//TODO: Continue here.
			}
		}
		
		public override bool IsLeaf { get { return true; } }

		public override void DrawGizmo()
		{
			Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
			Gizmos.DrawWireSphere(Bounds.center, 0.5f);
			Gizmos.DrawWireCube(Bounds.center, Bounds.size);
		}
	}
}