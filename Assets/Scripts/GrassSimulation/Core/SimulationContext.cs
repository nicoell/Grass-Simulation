using System;
using GrassSimulation.Core.Attribute;
using GrassSimulation.Core.Inputs;
using GrassSimulation.Core.Patches;
using TypeReferences;
using UnityEngine;
using Random = System.Random;

namespace GrassSimulation.Core
{
	[Serializable]
	public class SimulationContext : ScriptableObject, IInitializable
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
		
		[Header("Inputs")]
		[ClassExtends(typeof(DimensionsInput))]
		public ClassTypeReference.ClassTypeReference DimensionsInputType;
		[EmbeddedScriptableObject]
		public DimensionsInput DimensionsInput;
		[ClassExtends(typeof(HeightInput))]
		public ClassTypeReference.ClassTypeReference HeightInputType;
		[EmbeddedScriptableObject]
		public HeightInput HeightInput;
		[ClassExtends(typeof(NormalInput))]
		public ClassTypeReference.ClassTypeReference NormalInputType;
		[EmbeddedScriptableObject]
		public NormalInput NormalInput;
		
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
		public GrassInstance GrassInstance;
		//public Terrain Terrain;
		//Settings
		public EditorSettings EditorSettings;
		public SimulationSettings Settings;

		private void OnValidate()
		{
			if (HeightInputType.Type == null)
				HeightInput = null;
			else if (HeightInput == null || HeightInput.GetType() != HeightInputType.Type)
				HeightInput = Activator.CreateInstance(HeightInputType) as HeightInput;
			
			if (DimensionsInputType.Type == null)
				DimensionsInput = null;
			else if (DimensionsInput == null || DimensionsInput.GetType() != DimensionsInputType.Type)
				DimensionsInput = Activator.CreateInstance(DimensionsInputType) as DimensionsInput;
			
			if (NormalInputType.Type == null)
				NormalInput = null;
			else if (NormalInput == null || NormalInput.GetType() != NormalInputType.Type)
				NormalInput = Activator.CreateInstance(NormalInputType) as NormalInput;
		}

		public bool Init()
		{
			if (!Transform || !Camera || !GrassSimulationComputeShader || !GrassGeometry || !DimensionsInput || !HeightInput || !NormalInput)
			{
				Debug.LogWarning("GrassSimulation: Not all dependencies are set.");
				if (!Transform) Debug.Log("GrassSimulation: Transform not set.");
				if (!Camera) Debug.Log("GrassSimulation: Camera not set.");
				if (!GrassSimulationComputeShader) Debug.Log("GrassSimulation: GrassSimulationComputeShader not set.");
				if (!GrassGeometry) Debug.Log("GrassSimulation: Material not set.");
				if (!DimensionsInput) Debug.Log("GrassSimulation: DimensionsInput not set.");
				if (!HeightInput) Debug.Log("GrassSimulation: HeightInput not set.");
				if (!NormalInput) Debug.Log("GrassSimulation: NormalInput not set.");
				IsReady = false;
				return false;
			}
			
			//HeightInput = Activator.CreateInstance(HeightInputType) as IHeightInput;

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
			GrassInstance = new GrassInstance(this);
			GrassInstance.Init();

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
			if (DimensionsInput is IIntializableWithCtx) (DimensionsInput as IIntializableWithCtx).Init(this);
			else if (DimensionsInput is IInitializable) (DimensionsInput as IInitializable).Init();
			
			if (HeightInput is IIntializableWithCtx) (HeightInput as IIntializableWithCtx).Init(this);
			else if (HeightInput is IInitializable) (HeightInput as IInitializable).Init();
			
			if (NormalInput is IIntializableWithCtx) (NormalInput as IIntializableWithCtx).Init(this);
			else if (NormalInput is IInitializable) (NormalInput as IInitializable).Init();
			
			
			//Everything is ready.
			IsReady = true;
			return true;
		}
	}
}