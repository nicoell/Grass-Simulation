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
}