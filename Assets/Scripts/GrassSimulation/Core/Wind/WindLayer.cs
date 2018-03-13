using UnityEngine;

namespace GrassSimulation.Core.Wind {
	public abstract class WindLayer : MonoBehaviour
	{
		public GrassSimulationController GrassSim;
		protected SimulationContext Ctx;
		[HideInInspector]
		public bool IsActive;
		public abstract int WindType { get; }

		public abstract WindLayerData GetWindData();

		protected void Start()
		{
			Ctx = GrassSim.Context;
			IsActive = true;
			Ctx.WindManager.RegisterLayer(this);
		}

		protected void OnDisable()
		{
			IsActive = false;
			Ctx.WindManager.CleanLayers();
		}

		protected void OnDestroy()
		{
			IsActive = false;
			Ctx.WindManager.CleanLayers();
		}
	}
}