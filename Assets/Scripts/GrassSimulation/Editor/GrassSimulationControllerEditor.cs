using System;
using GrassSimulation.Core.Inputs;
using GrassSimulation.StandardInputs;
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
			serializedObject.Update();
			var controller = (GrassSimulationController) target;
			
			if (GUILayout.Button("Prepare Simulation"))
				controller.PrepareSimulation();
			if (GUILayout.Button("Print Debug Info"))
				controller.Context.PrintDebugInfo();
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}