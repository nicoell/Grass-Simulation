namespace GrassSimulation
{
	public abstract class RequiredContext
	{
		protected SimulationContext Ctx;

		protected RequiredContext(SimulationContext ctx)
		{
			Ctx = ctx;
		}
	}
}