using UnityEngine;

namespace GrassSimulation.Core.Patches
{
	public sealed class BillboardTexturePatchContainer : PatchContainer
	{
		private BillboardTexturePatch _billboardTexturePatch;

		public override void Destroy()
		{
			_billboardTexturePatch.Destroy();
		}

		public override Bounds GetBounds()
		{
			return _billboardTexturePatch.Bounds;
		}

		protected override void DrawImpl()
		{
			_billboardTexturePatch.Draw();
		}

		public override void SetupContainer()
		{
			_billboardTexturePatch = new BillboardTexturePatch(Ctx);
		}

		protected override void DrawGizmoImpl()
		{
			_billboardTexturePatch.DrawGizmo();	
		}

		public override void OnGUI()
		{
			
		}
	}
}