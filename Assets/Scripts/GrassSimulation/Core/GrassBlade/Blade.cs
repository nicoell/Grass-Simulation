using System;
using JetBrains.Annotations;
using UnityEngine;

namespace GrassSimulation.Core.GrassBlade
{
	[Serializable]
	public class Blade
	{
		[SerializeField]
		public AnimationCurve EdgeCurve;
		[SerializeField]
		public AnimationCurve MidTranslation;
		[SerializeField]
		public Texture2D GrassTexture;
		[Range(0, 1)]
		public float WidthModifier = 1;
		[Range(0, 1)]
		public float DiffuseReflectance;
		[Range(0, 1)]
		public float Translucency;

	}
}