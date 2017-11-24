using System;
using GrassSimulation.Core;
using GrassSimulation.Core.Patches;
using UnityEngine;

namespace GrassSimulation.StandardContainers
{
	public class BoundingPatch : Patch
	{
		private readonly Patch[] _childPatches = new Patch[4];
		private int _index;

		public BoundingPatch(SimulationContext ctx) : base(ctx)
		{
		}

		public Patch[] ChildPatches
		{
			get { return _childPatches; }
		}

		public override bool IsLeaf
		{
			get { return false; }
		}


		public void AddChild(Patch child)
		{
			if (_index >= 4) throw new OverflowException("");
			ChildPatches[_index] = child;
			_index++;
			if (_index >= 4)
				CreateBounds();
		}

		private void CreateBounds()
		{
			var tempBounds = new Bounds();
			if (ChildPatches[0] != null) tempBounds = ChildPatches[0].Bounds;
			for (var i = 1; i < ChildPatches.Length; i++)
				if (ChildPatches[i] != null) tempBounds.Encapsulate(ChildPatches[i].Bounds);
			Bounds = tempBounds;
		}

		public override void DrawGizmo()
		{
			if (!Ctx.EditorSettings.EnableHierarchyGizmo) return;
			
			Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
			Gizmos.DrawWireSphere(Bounds.center, 0.5f);
			Gizmos.DrawWireCube(Bounds.center, Bounds.size);
			foreach (var child in ChildPatches)
				if (child != null && !child.IsLeaf) child.DrawGizmo();
		}
	}
}