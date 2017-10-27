using UnityEngine;

namespace GrassSim {
	public abstract class APatch
	{
		public Bounds Bounds { get; protected set; }
		public abstract bool IsLeaf { get; }
		
		public virtual void DrawGizmo()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(Bounds.center, 0.5f);
			Gizmos.DrawWireCube(Bounds.center, Bounds.size);
		}

		
	}
}