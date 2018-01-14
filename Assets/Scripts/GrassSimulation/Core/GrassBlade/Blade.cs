using System;
using UnityEngine;

namespace GrassSimulation.Core.GrassBlade
{
	[Serializable]
	public class Blade
	{
		public AnimationCurve EdgeCurve = AnimationCurve.Linear(0, 1, 1, 0);
		public AnimationCurve MidTranslation = AnimationCurve.Linear(0, 1, 1, 0);
		public Gradient ColorGradient;
		[Range(0, 1)]
		public float WidthModifier = 1;
	}
}