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
			if (property.objectReferenceValue == null) return;
			if (Target.OverlapTitle)
			{
				position.y -= 16;
				position.height = 16;
				Target.ShowScriptableObject =
					EditorGUI.Foldout(position, Target.ShowScriptableObject, label, true, Target.Style);
			}
			else
			{
				Target.ShowScriptableObject =
					EditorGUILayout.Foldout(Target.ShowScriptableObject, label, true, Target.Style);
			}
			if (Target.ShowScriptableObject)
			{
				var so = new SerializedObject(property.objectReferenceValue);
				so.Update();
				var prop = so.GetIterator();
				prop.NextVisible(true);
				while (prop.NextVisible(false))
				{
					EditorGUI.indentLevel = prop.depth + (Target.OverlapTitle ? 1 : 0);
					EditorGUILayout.PropertyField(prop, true);
				}

				if (GUI.changed) so.ApplyModifiedProperties();
				
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return Target.OverlapTitle ? 0 : 0;
		}
	}
}