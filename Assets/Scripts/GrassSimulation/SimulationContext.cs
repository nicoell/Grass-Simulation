using GrassSimulation.Grass;
using UnityEngine;

namespace GrassSimulation
{
	[CreateAssetMenu(menuName = "Grass Simulation/Simulation Context", fileName = "NewSimulationContext", order = 1)]
	public class SimulationContext : ScriptableObject, IInitializable
	{
		public Camera Camera;
		public Texture2D Heightmap;

		[HideInInspector] public bool IsReady;

		public SimulationSettings Settings;
		public SharedGrassData SharedGrassData;
		public Terrain Terrain;
		public Transform Transform;

		public bool Init()
		{
			if (!Camera || !Terrain || !Transform)
			{
				IsReady = false;
				return false;
			}

			//Build Heightmap Texture
			Heightmap = Utils.CreateHeightmapFromTerrain(Terrain);

			//Create sharedGrassData
			SharedGrassData = new SharedGrassData(this);
			SharedGrassData.Init();

			//Everything is ready.
			IsReady = true;
			return true;
		}
	}
}