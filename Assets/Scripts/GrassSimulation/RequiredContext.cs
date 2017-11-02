namespace GrassSimulation
{
	public abstract class RequiredContext
	{
		protected SimulationContext Context;

		public RequiredContext(SimulationContext context)
		{
			Context = context;
		}
	}
}