namespace GrassSimulation.Core
{
	public interface IInitializableWithCtx
	{
		bool Init(SimulationContext context);
	}
}