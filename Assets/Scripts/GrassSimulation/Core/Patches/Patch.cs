using UnityEngine;

namespace GrassSimulation.Core.Patches
{
	public abstract class Patch : ContextRequirement
	{
		protected Patch(SimulationContext ctx) : base(ctx)
		{
		}

		private Bounds _bounds;
		public Bounds Bounds { get { return _bounds; } protected set { _bounds = value; } }
		public abstract bool IsLeaf { get; }

		public virtual void DrawGizmo()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(Bounds.center, 0.5f);
			Gizmos.DrawWireCube(Bounds.center, Bounds.size);
		}
	}
}