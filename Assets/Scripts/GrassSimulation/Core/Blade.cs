using System;
using UnityEngine;

namespace GrassSimulation.Core
{
	[Serializable]
	public class Blade
	{
		public AnimationCurve LeftEdgeCurve = AnimationCurve.Linear(0, 1, 1, 0);
		public AnimationCurve RightEdgeCurve = AnimationCurve.Linear(0, 1, 1, 0);
		public AnimationCurve LeftEdgeRotation = AnimationCurve.Linear(0, 1, 1, 0);
		public AnimationCurve RightEdgeRotation = AnimationCurve.Linear(0, 1, 1, 0);
		public Gradient LeftColorGradient;
		public Gradient RightColorGradient;
	}
}