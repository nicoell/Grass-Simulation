using System;
using UnityEngine;

namespace GrassSim {
	public class HierarchicalPatch : APatch
	{
		private readonly APatch[] m_childPatches = new APatch[4];
		public APatch[] ChildPatches { get { return m_childPatches; } }
		private int m_index = 0;
		public override bool IsLeaf { get { return false; } }
		

		public void AddChild(APatch child)
		{
			if (m_index >= 4) throw new OverflowException("");
			ChildPatches[m_index] = child;
			m_index++;
			if (m_index >= 4)
			{
				CreateBounds();
			}
		}

		private void CreateBounds()
		{
			var tempBounds = new Bounds();
			if (ChildPatches[0] != null) tempBounds = ChildPatches[0].Bounds;
			for (int i = 1; i < ChildPatches.Length; i++)
			{
				if (ChildPatches[i] != null) tempBounds.Encapsulate(ChildPatches[i].Bounds);
			}
			Bounds = tempBounds;
		}

		public override void DrawGizmo()
		{
			Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
			Gizmos.DrawWireSphere(Bounds.center, 0.5f);
			Gizmos.DrawWireCube(Bounds.center, Bounds.size);
			foreach (var child in ChildPatches)
			{
				if (child != null && !child.IsLeaf) child.DrawGizmo();
			}
		}
	}
}