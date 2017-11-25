namespace GrassSimulation.Core
{
	public interface IIntializableWithCtx
	{
		bool Init(SimulationContext context);
	}
}