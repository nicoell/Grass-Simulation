using GrassSimulation.Core.Attribute;
using UnityEditor;
using UnityEngine;

namespace GrassSimulation.Core.Editor
{
	[CustomPropertyDrawer(typeof(EmbeddedScriptableObjectAttribute))]
	public class EmbeddedScriptableObjectAttributePropertyDrawer : PropertyDrawer
	{
		private EmbeddedScriptableObjectAttribute Target
		{
			get { return (EmbeddedScriptableObjectAttribute) attribute; }
		}

		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Target.ShowScriptableObject =
				EditorGUILayout.Foldout(Target.ShowScriptableObject, property.displayName, true, EditorStyles.foldout);
			if (Target.ShowScriptableObject && property.objectReferenceValue != null)
			{
				var so = new SerializedObject(property.objectReferenceValue);
				so.Update();
				var prop = so.GetIterator();
				prop.NextVisible(true);
				while (prop.NextVisible(false))
				{
					EditorGUI.indentLevel = prop.depth;
					EditorGUILayout.PropertyField(prop, true);
				}

				if (GUI.changed) so.ApplyModifiedProperties();
				
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}
	}
}