using GrassSimulation.Utils;
using UnityEditor;
using UnityEngine;

namespace GrassSimulation.Editor
{
	[CustomEditor(typeof(GrassBladeCreator))]
	public class GrassBladeCreatorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			var controller = (GrassBladeCreator) target;
			
			if (GUILayout.Button("Generate Blade Texture"))
				controller.GenerateBlade();
		}
	}
}