using System;
using JetBrains.Annotations;
using UnityEngine;

namespace GrassSimulation.Core.GrassBlade
{
	[Serializable]
	public class Blade
	{
		[SerializeField]
		[Range(0, 1)]
		public float Probability = 0.5f;
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

		[SerializeField]
		public bool HasBlossom;
		[SerializeField]
		public Texture2D BlossomTexture;
		[SerializeField]
		public AnimationCurve BlossomBeta;
		[SerializeField]
		[Tooltip("Verschiebung weg von der Mitte")]
		public AnimationCurve BlossomGamma;
		[SerializeField]
		[Tooltip("Verschiebung entlang der Tangente")]
		public AnimationCurve BlossomDelta;
		[Range(0, 1)]
		public float BlossomDiffuseReflectance;

	}
}