﻿using UnityEngine;

namespace GrassSimulation.Core.Lod
{
	public abstract class Patch : ContextRequirement
	{
		protected Patch(SimulationContext ctx) : base(ctx)
		{
		}

		public Bounds Bounds { get; protected set; }

		public abstract bool IsLeaf { get; }

		public virtual void DrawGizmo(int level = 0)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(Bounds.center, 0.5f);
			Gizmos.DrawWireCube(Bounds.center, Bounds.size);
		}

		public virtual void OnGUI() { }

		public abstract void Unload();
	}
}