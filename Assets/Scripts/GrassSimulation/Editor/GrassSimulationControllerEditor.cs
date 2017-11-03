using UnityEditor;
using UnityEngine;

namespace GrassSimulation.Editor
{
	[CustomEditor(typeof(GrassSimulationController))]
	public class GrassSimulationControllerEditor : UnityEditor.Editor
	{
		private UnityEditor.Editor _editor;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			var controller = (GrassSimulationController) target;
			
			/*serializedObject.Update();
			CreateCachedEditor(controller.Context, null, ref _editor);
			EditorGUI.indentLevel++;
			_editor.OnInspectorGUI();
			EditorGUI.indentLevel--;
			serializedObject.ApplyModifiedProperties();*/

			if (GUILayout.Button("Prepare Simulation"))
				controller.PrepareSimulation();
		}
	}
}