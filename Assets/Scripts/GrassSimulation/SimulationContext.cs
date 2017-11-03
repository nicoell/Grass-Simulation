using System;
using GrassSimulation.Grass;
using UnityEngine;

namespace GrassSimulation
{
	//[CreateAssetMenu(menuName = "Grass Simulation/Simulation Context", fileName = "NewSimulationContext", order = 1)]
	[Serializable]
	public class SimulationContext : IInitializable
	{
		public Camera Camera;
		public Texture2D Heightmap;

		[HideInInspector] public bool IsReady;

		public SimulationSettings Settings;
		public EditorSettings EditorSettings;
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

			if (Settings == null) Settings = new SimulationSettings();
			if (EditorSettings == null) EditorSettings = new EditorSettings();

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