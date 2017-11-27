using UnityEditor;
using UnityEngine;

namespace GrassSimulation.Core.Editor
{
	[CustomEditor(typeof(BladeContainer))]
	public class BladeContainerEditor : UnityEditor.Editor
	{
		private BladeContainer BladeContainer
		{
			get { return (BladeContainer) target; }
		}
		//private PreviewRenderUtility _previewRenderUtility;

		private void OnValidate()
		{
			/*if (_previewRenderUtility == null)
			{
				_previewRenderUtility = new PreviewRenderUtility();
			}*/
			if (BladeContainer.Blades == null) BladeContainer.Blades = new Blade[1];
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			base.OnPreviewGUI(r, background);
		}

		public override bool HasPreviewGUI()
		{
			return base.HasPreviewGUI();
		}
	}
	
}