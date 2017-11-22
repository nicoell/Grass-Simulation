using System;
using GrassSimulation.DataProvider;
using GrassSimulation.Grass;
using UnityEngine;
using Random = System.Random;

namespace GrassSimulation
{
	[Serializable]
	public class SimulationContext : IInitializable
	{
		[Header("Requirements")]
		public Transform Transform;
		public Camera Camera;
		public ComputeShader GrassSimulationComputeShader;
		public Material GrassGeometry;
		[NonSerialized]
		public Material GrassBillboardCrossed;
		[NonSerialized]
		public Material GrassBillboardScreen;
		
		[Header("Data Provider")]
		public DimensionsProvider DimensionsProvider;
		public HeightProvider HeightProvider;
		public NormalProvider NormalProvider;
		
		[HideInInspector]
		public bool IsReady;
		//[HideInInspector]
		//public int KernelCulling;
		[HideInInspector] 
		public int KernelPhysics;
		[HideInInspector] 
		public int KernelSimulationSetup;
		[NonSerialized] 
		public Random Random;
		public SharedGrassData SharedGrassData;
		//public Terrain Terrain;
		//Settings
		public EditorSettings EditorSettings;
		public SimulationSettings Settings;

		public bool Init()
		{
			if (!Transform || !Camera || !GrassSimulationComputeShader || !GrassGeometry || !HeightProvider || !NormalProvider)
			{
				Debug.LogWarning("GrassSimulation: Not all dependencies are set.");
				if (!Transform) Debug.Log("GrassSimulation: Transform not set.");
				if (!Camera) Debug.Log("GrassSimulation: Camera not set.");
				if (!GrassSimulationComputeShader) Debug.Log("GrassSimulation: GrassSimulationComputeShader not set.");
				if (!GrassGeometry) Debug.Log("GrassSimulation: Material not set.");
				if (!HeightProvider) Debug.Log("GrassSimulation: HeightProvider not set.");
				if (!NormalProvider) Debug.Log("GrassSimulation: NormalProvider not set.");
				IsReady = false;
				return false;
			}

			if (Settings == null)
			{
				Settings = new SimulationSettings();
				//Settings.GrassBlade = Texture2D.whiteTexture;
			}
			if (EditorSettings == null) EditorSettings = new EditorSettings();
			
			//Create a single random object
			Random = new Random(Settings.RandomSeed);

			//Find kernels for ComputeShaders
			KernelPhysics = GrassSimulationComputeShader.FindKernel("PhysicsMain");
			//KernelCulling = GrassSimulationComputeShader.FindKernel("CullingMain");
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
			
			GrassSimulationComputeShader.SetFloat("LodTessellationMin", Settings.LodTessellationMin);
			GrassSimulationComputeShader.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
			GrassSimulationComputeShader.SetFloat("LodDistanceTessellationMin", Settings.LodDistanceTessellationMin);
			GrassSimulationComputeShader.SetFloat("LodDistanceTessellationMax", Settings.LodDistanceTessellationMax);
			
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
			
			//If possible initialize the Data Providers
			if (DimensionsProvider is IIntializableWithCtx) (DimensionsProvider as IIntializableWithCtx).Init(this);
			else if (DimensionsProvider is IInitializable) (DimensionsProvider as IInitializable).Init();
			
			if (HeightProvider is IIntializableWithCtx) (HeightProvider as IIntializableWithCtx).Init(this);
			else if (HeightProvider is IInitializable) (HeightProvider as IInitializable).Init();
			
			if (NormalProvider is IIntializableWithCtx) (NormalProvider as IIntializableWithCtx).Init(this);
			else if (NormalProvider is IInitializable) (NormalProvider as IInitializable).Init();
			
			
			//Everything is ready.
			IsReady = true;
			return true;
		}
	}
}