using GrassSimulation.Core;
using GrassSimulation.Core.Attribute;
using UnityEngine;

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
			if (Context.IsReady) Context.PatchContainer.Draw();
		}

		private void OnDrawGizmos()
		{
			if (Context.IsReady) Context.PatchContainer.DrawGizmo();
		}

		//TODO: Need to revisit the correct way to destroy/dispose/release ComputeBuffers so the warnings go away
		private void OnDisable()
		{
			if (Context.IsReady) Context.PatchContainer.Destroy();
		}
	}
}