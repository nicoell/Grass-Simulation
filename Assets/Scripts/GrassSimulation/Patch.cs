using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace GrassSim
{
	public class Patch : APatch
	{
		private int m_randomNumber;
		private Vector4 m_texCoordHeightmap;
		public override bool IsLeaf { get { return true; } }

		public Patch(Vector4 texCoordHeightmap, Bounds bounds)
		{
			this.Bounds = bounds;
			m_texCoordHeightmap = texCoordHeightmap;
			m_randomNumber = 42; //TODO: Use random number
		}

		

		public override void DrawGizmo()
		{
			Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
			Gizmos.DrawWireSphere(Bounds.center, 0.5f);
			Gizmos.DrawWireCube(Bounds.center, Bounds.size);
		}
	}
}