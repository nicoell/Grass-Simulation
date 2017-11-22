using System;
using GrassSimulation.Grass;
using UnityEngine;
using Random = System.Random;

namespace GrassSimulation
{
	[Serializable]
	public class SimulationContext : IInitializable
	{
		public Camera Camera;
		public EditorSettings EditorSettings;
		public ComputeShader GrassSimulationComputeShader;
		public Material GrassGeometry;
		[HideInInspector] 
		public Material GrassBillboardCrossed;
		[HideInInspector] 
		public Material GrassBillboardScreen;
		public Texture2D Heightmap;
		[HideInInspector]
		public bool IsReady;
		[HideInInspector]
		public int KernelCulling;
		[HideInInspector] 
		public int KernelPhysics;
		[HideInInspector] 
		public int KernelSimulationSetup;
		[HideInInspector] 
		public Random Random;
		public SimulationSettings Settings;
		public SharedGrassData SharedGrassData;
		public Terrain Terrain;
		public Transform Transform;

		public bool Init()
		{
			if (!Camera || !GrassGeometry || !Terrain || !Transform || !GrassSimulationComputeShader)
			{
				Debug.LogWarning("GrassSimulation: Not all dependencies are set.");
				if (!Camera) Debug.Log("GrassSimulation: Camera not set.");
				if (!GrassGeometry) Debug.Log("GrassSimulation: Material not set.");
				if (!Terrain) Debug.Log("GrassSimulation: Terrain not set.");
				if (!Transform) Debug.Log("GrassSimulation: Transform not set.");
				if (!GrassSimulationComputeShader) Debug.Log("GrassSimulation: GrassSimulationComputeShader not set.");
				IsReady = false;
				return false;
			}

			if (Settings == null)
			{
				Settings = new SimulationSettings();
				//Settings.GrassBlade = Texture2D.whiteTexture;
			}
			if (EditorSettings == null) EditorSettings = new EditorSettings();

			//Build Heightmap Texture
			Heightmap = Utils.Terrain.CreateHeightmapFromTerrain(Terrain);

			//Create a single random object
			Random = new Random(Settings.RandomSeed);

			//Find kernels for ComputeShaders
			KernelPhysics = GrassSimulationComputeShader.FindKernel("PhysicsMain");
			KernelCulling = GrassSimulationComputeShader.FindKernel("CullingMain");
			KernelSimulationSetup =  GrassSimulationComputeShader.FindKernel("SimulationSetup"); 
			
			GrassBillboardCrossed = new Material(GrassGeometry);
			GrassBillboardScreen = new Material(GrassGeometry);
			
			//Create sharedGrassData
			SharedGrassData = new SharedGrassData(this);
			SharedGrassData.Init();

			GrassGeometry.EnableKeyword("GRASS_GEOMETRY");
			GrassGeometry.DisableKeyword("GRASS_BILLBOARD_CROSSED");
			GrassGeometry.DisableKeyword("GRASS_BILLBOARD_SCREEN");
			GrassGeometry.SetTexture("GrassBlade", Settings.GrassBlade);
			GrassGeometry.SetInt("vertexCount", (int) Settings.GetMinAmountBladesPerPatch());
			GrassGeometry.SetFloat("billboardSize", Settings.BillboardSize);
			GrassBillboardCrossed.DisableKeyword("GRASS_GEOMETRY");
			GrassBillboardCrossed.EnableKeyword("GRASS_BILLBOARD_CROSSED");
			GrassBillboardCrossed.DisableKeyword("GRASS_BILLBOARD_SCREEN");
			GrassBillboardCrossed.SetTexture("GrassBlade", Settings.GrassBlade);
			GrassBillboardCrossed.SetInt("vertexCount", (int) Settings.GetMinAmountBillboardsPerPatch());
			GrassBillboardCrossed.SetFloat("billboardSize", Settings.BillboardSize);
			GrassBillboardScreen.DisableKeyword("GRASS_GEOMETRY");
			GrassBillboardScreen.DisableKeyword("GRASS_BILLBOARD_CROSSED");
			GrassBillboardScreen.EnableKeyword("GRASS_BILLBOARD_SCREEN");
			GrassBillboardScreen.SetTexture("GrassBlade", Settings.GrassBlade);
			GrassBillboardScreen.SetInt("vertexCount", (int) Settings.GetMinAmountBillboardsPerPatch());
			GrassBillboardScreen.SetFloat("billboardSize", Settings.BillboardSize);
			
			/*GrassSimulationComputeShader.SetFloat("LodDistanceFullDetail", Settings.LodDistanceFullDetail);
			GrassSimulationComputeShader.SetFloat("LodDistanceBillboard", Settings.LodDistanceBillboard);
			GrassSimulationComputeShader.SetFloat("LodDistanceMax", Settings.LodDistanceMax);
			GrassSimulationComputeShader.SetFloat("LodDensityFullDetailDistance", Settings.LodDensityFullDetailDistance);
			GrassSimulationComputeShader.SetFloat("LodDensityBillboardDistance", Settings.LodDensityBillboardDistance);
			GrassSimulationComputeShader.SetFloat("LodDensityMaxDistance", Settings.LodDensityMaxDistance);*/
			
			Shader.SetGlobalFloat("LodInstancesGeometry", Settings.LodInstancesGeometry);
			Shader.SetGlobalFloat("LodInstancesBillboardCrossed", Settings.LodInstancesBillboardCrossed);
			Shader.SetGlobalFloat("LodInstancesBillboardScreen", Settings.LodInstancesBillboardScreen);
			Shader.SetGlobalFloat("LodDistanceGeometryStart", Settings.LodDistanceGeometryStart);
			Shader.SetGlobalFloat("LodDistanceGeometryPeak", Settings.LodDistanceGeometryPeak);
			Shader.SetGlobalFloat("LodDistanceGeometryEnd", Settings.LodDistanceGeometryEnd);
			Shader.SetGlobalFloat("LodDistanceBillboardCrossedStart", Settings.LodDistanceBillboardCrossedStart);
			Shader.SetGlobalFloat("LodDistanceBillboardCrossedPeak", Settings.LodDistanceBillboardCrossedPeak);
			Shader.SetGlobalFloat("LodDistanceBillboardCrossedEnd", Settings.LodDistanceBillboardCrossedEnd);
			Shader.SetGlobalFloat("LodDistanceBillboardScreenStart", Settings.LodDistanceBillboardScreenStart);
			Shader.SetGlobalFloat("LodDistanceBillboardScreenPeak", Settings.LodDistanceBillboardScreenPeak);
			Shader.SetGlobalFloat("LodDistanceBillboardScreenEnd", Settings.LodDistanceBillboardScreenEnd);

			//Everything is ready.
			IsReady = true;
			return true;
		}
	}
}