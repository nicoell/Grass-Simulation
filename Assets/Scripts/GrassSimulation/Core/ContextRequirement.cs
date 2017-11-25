using UnityEngine;

namespace GrassSimulation.Core
{
	public abstract class ContextRequirement
	{
		protected SimulationContext Ctx;

		protected ContextRequirement(SimulationContext ctx)
		{
			Ctx = ctx;
		}
	}

	public abstract class ScriptableObjectContextRequirement : ScriptableObject
	{
		protected SimulationContext Ctx;

		protected ScriptableObjectContextRequirement(SimulationContext ctx)
		{
			Ctx = ctx;
		}
	}
}