using System;
using UnityEditor;
using UnityEngine;

namespace GrassSimulation.Core.Attribute
{
	[AttributeUsage(AttributeTargets.Field)]
	public class EmbeddedScriptableObjectAttribute : PropertyAttribute
	{
		public bool ShowScriptableObject;
		public GUIStyle Style;
		public EmbeddedScriptableObjectAttribute()
		{
			Style = EditorStyles.foldout;
			Style.fontStyle = FontStyle.Bold;
		}
	}
}