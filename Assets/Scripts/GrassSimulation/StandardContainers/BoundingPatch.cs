using System;
using GrassSimulation.Core;
using GrassSimulation.Core.Lod;
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

		public override void DrawGizmo(int level = 0)
		{
			if (!Ctx.EditorSettings.EnableHierarchyGizmo) return;
			float fac = 0f, fac2 = 0f, fac3 = 0f;
			switch (level)
			{
					case 0: fac = 1.0f; fac2 = 0.0f; fac3 = 0.0f; break;
					case 1: fac = 0.0f; fac2 = 1.0f; fac3 = 0.0f; break;
					case 2: fac = 1.0f; fac2 = 1.0f; fac3 = 0.0f;  break;
					default: fac = 0.6f; break;
			}
			Debug.Log(level);
			Gizmos.color = new Color(fac, fac2, fac3, 0.04f);
			//Gizmos.DrawWireSphere(Bounds.center, 0.5f);
			Gizmos.DrawCube(Bounds.center, Bounds.size);
			Gizmos.color = new Color(fac, fac2, fac3, 0.5f);
			Gizmos.DrawWireCube(Bounds.center, Bounds.size);
			level++;
			foreach (var child in ChildPatches)
				if (child != null && !child.IsLeaf) child.DrawGizmo(level);
		}

		public override void Unload() { return; }
	}
}