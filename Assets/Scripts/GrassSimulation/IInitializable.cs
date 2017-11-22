namespace GrassSimulation
{
	public interface IInitializable
	{
		bool Init();
	}

	public interface IIntializableWithCtx
	{
		bool Init(SimulationContext context);
	}
}