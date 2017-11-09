using System;
using GrassSimulation.Grass;
using UnityEngine;
using Random = System.Random;

namespace GrassSimulation
{
	//[CreateAssetMenu(menuName = "Grass Simulation/Simulation Context", fileName = "NewSimulationContext", order = 1)]
	[Serializable]
	public class SimulationContext : IInitializable
	{
		public Camera Camera;
		public EditorSettings EditorSettings;
		public ComputeShader ForcesComputeShader;
		public int ForcesComputeShaderKernel;
		public Material GrassSimulationMaterial;
		public Shader GrassSimulationShader;
		public Texture2D Heightmap;

		[HideInInspector] public bool IsReady;
		[HideInInspector] public Random Random;

		public SimulationSettings Settings;
		public SharedGrassData SharedGrassData;
		public Terrain Terrain;
		public Transform Transform;
		public ComputeShader VisibilityComputeShader;
		public int VisibilityComputeShaderKernel;

		public bool Init()
		{
			if (!Camera || !Terrain || !Transform || !ForcesComputeShader || !VisibilityComputeShader)
			{
				IsReady = false;
				return false;
			}

			if (Settings == null) Settings = new SimulationSettings();
			if (EditorSettings == null) EditorSettings = new EditorSettings();

			//Build Heightmap Texture
			Heightmap = Utils.CreateHeightmapFromTerrain(Terrain);
			
			//Create a single random object
			Random = new Random(Settings.RandomSeed);
			
			//Find kernels for ComputeShaders
			ForcesComputeShaderKernel = ForcesComputeShader.FindKernel("CSMain");
			VisibilityComputeShaderKernel = VisibilityComputeShader.FindKernel("CSMain");


			//Create sharedGrassData
			SharedGrassData = new SharedGrassData(this);
			SharedGrassData.Init();

			//Everything is ready.
			IsReady = true;
			return true;
		}
	}
}