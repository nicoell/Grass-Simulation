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

		public float UpdateInterval = 10.0f;
		private double _lastInterval;
		private int _frames = 0;
		private float _fps;

		// Use this for initialization
		private void OnEnable()
		{
			PrepareSimulation();
		}

		public void PrepareSimulation()
		{
			if (Context == null) Context = ScriptableObject.CreateInstance<SimulationContext>();
			Context.Init();
			_lastInterval = Time.realtimeSinceStartup;
			_frames = 0;
		}

		// Update is called once per frame
		private void Update()
		{
			++_frames;
			float timeNow = Time.realtimeSinceStartup;
			if (Context.IsReady)
			{
				if (timeNow > _lastInterval + UpdateInterval)
				{
					_fps = (float)(_frames / (timeNow - _lastInterval));
					_frames = 0;
					_lastInterval = timeNow;
					Debug.Log("FPS: " + _fps);
				}
				Context.CollisionTextureRenderer.UpdateDepthTexture();
				Context.WindManager.Update();
				//Context.ProceduralWind.Update();
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

		private void OnGUI()
		{
			if (Context.IsReady)  Context.OnGUI();
		}
	}
}