namespace GrassSimulation
{
	public abstract class RequiredContext
	{
		protected SimulationContext Context;

		protected RequiredContext(SimulationContext context)
		{
			Context = context;
		}
	}
}