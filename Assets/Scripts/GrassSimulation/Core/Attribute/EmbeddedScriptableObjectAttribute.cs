using System;
using UnityEngine;

namespace GrassSimulation.Core.Attribute
{
	[Serializable]
	[AttributeUsage(AttributeTargets.Field)]
	public class EmbeddedScriptableObjectAttribute : PropertyAttribute
	{
		public bool ShowScriptableObject;
		public bool OverlapTitle;
		public GUIStyle Style;
		
		public EmbeddedScriptableObjectAttribute()
		{
		}

		public EmbeddedScriptableObjectAttribute(bool foldout) : this()
		{
			ShowScriptableObject = foldout;
		}
		
		public EmbeddedScriptableObjectAttribute(bool foldout, bool overlap) : this(foldout)
		{
			OverlapTitle = overlap;
		}
	}
}