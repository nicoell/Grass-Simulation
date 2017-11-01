using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;

namespace GrassSim
{
	[CreateAssetMenu(fileName = "Data", menuName = "GrassSimulation/Settings", order = 3)]
	public class Settings : ScriptableObject
	{
		public float grassDensity = 50f;
		public uint patchSize = 8;
		public int randomSeed = 42;
		public uint precomputedFactor = 64;
		public float bladeMinHeight = 2f;
		public float bladeMaxHeight = 10f;
		public float bladeMinWidth = 0.1f;
		public float bladeMaxWidth = 3f;
		public float bladeMinBend = 3f;
		public float bladeMaxBend = 10f;

		public uint GetAmountBlades() { return (uint) (patchSize * patchSize * grassDensity); }
		public uint GetAmountPrecomputedBlades() { return GetAmountBlades() * precomputedFactor; }
	}
}