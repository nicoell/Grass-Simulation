using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace GrassSim.Grass
{
	public sealed class PrecomputedGrassData
	{
		private static readonly PrecomputedGrassData instance = new PrecomputedGrassData();
		private uint m_amountBlades;
		private float m_grassDensity;
		private uint m_patchSize;
		private Random m_random;
		private int m_seed;
		public Vector4[] GrassData { get; private set; } //pos.xy, width, bend
		private PrecomputedGrassData() { }
		public static PrecomputedGrassData Instance { get { return instance; } }

		public void Build(Settings settings)
		{
			if (m_patchSize == settings.patchSize && Mathf.Approximately(m_grassDensity, settings.grassDensity) &&
			    m_seed == settings.randomSeed) return;

			m_amountBlades = settings.GetAmountPrecomputedBlades();
			m_random = new Random(settings.randomSeed);
			m_seed = settings.randomSeed;

			GrassData = new Vector4[m_amountBlades];

			for (var i = 0; i < m_amountBlades; i++)
			{
				var randPos = new Vector2((float) m_random.NextDouble(), (float) m_random.NextDouble());
				randPos *= m_patchSize;
				float width = (float) (settings.bladeMinWidth + m_random.NextDouble() * (settings.bladeMaxWidth - settings.bladeMinWidth));
				float bend = (float) (settings.bladeMinBend + m_random.NextDouble() * (settings.bladeMaxBend - settings.bladeMinBend));
				
				GrassData[i].x = randPos.x;
				GrassData[i].y = randPos.y;
				GrassData[i].z = width;
				GrassData[i].w = bend;
			}

			Shader.SetGlobalVectorArray("_InstancedGrassData", GrassData);
		}
	}
}