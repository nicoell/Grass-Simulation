using GrassSimulation.Core;
using GrassSimulation.Core.Utils;
using UnityEngine;

/* TODO:
 Dynamic Tessellation Level
Wind
Collision
Blade Texture
Blade Width Correction Minimal-width shape

Statistics
Blades drawn
*/

namespace GrassSimulation
{
	[ExecuteInEditMode]
	public class GrassSimulationController : MonoBehaviour
	{
		[EmbeddedScriptableObject(true)]
		public SimulationContext Context;

		// Use this for initialization
		private void OnEnable()
		{
			PrepareSimulation();
		}

		public void PrepareSimulation()
		{
			if (Context == null) Context = ScriptableObject.CreateInstance<SimulationContext>();
			Context.Init();
		}

		// Update is called once per frame
		private void Update()
		{
			if (Context.IsReady)
			{
				Context.CollisionTextureRenderer.UpdateDepthTexture();
				Context.ProceduralWind.Update();
				//Context.WindFieldRenderer.Update();
				Context.PatchContainer.Draw();
				//Context.BillboardTexturePatchContainer.Draw();
			}
		}

		private void OnDrawGizmos()
		{
			if (Context.IsReady)
			{
				Context.PatchContainer.DrawGizmo();
				Context.BillboardTexturePatchContainer.DrawGizmo();
			}
		}

		//TODO: Need to revisit the correct way to destroy/dispose/release ComputeBuffers so the warnings go away
		private void OnDisable()
		{
			if (Context.IsReady) Context.PatchContainer.Destroy();
		}

		private void OnGUI() {if (Context.IsReady)  Context.OnGUI(); }
	}
}