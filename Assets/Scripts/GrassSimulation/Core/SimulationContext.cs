using System;
using GrassSimulation.Core.Billboard;
using GrassSimulation.Core.Collision;
using GrassSimulation.Core.GrassBlade;
using GrassSimulation.Core.Inputs;
using GrassSimulation.Core.Lod;
using GrassSimulation.Core.Utils;
using GrassSimulation.Core.Wind;
using UnityEngine;
using Random = System.Random;

namespace GrassSimulation.Core
{
	[Serializable]
	public class SimulationContext : ScriptableObject
	{
		[Header("Requirements")]
		public Transform Transform;
		public Camera Camera;
		public Light SunLight;
		public ComputeShader GrassSimulationComputeShader;
		public ComputeShader RenderTextureVolumeToSlice;
		public Shader CollisionDepthShader;
		[HideInInspector]
		public CollisionTextureRenderer CollisionTextureRenderer;
		[HideInInspector]
		public ProceduralWind ProceduralWind;

		public Shader GrassSimulationShader;
		[NonSerialized]
		public Material GrassGeometry;
		[NonSerialized]
		public Material GrassBlossom;
		[NonSerialized]
		public Material GrassBlossomBillboardGeneration;
		[NonSerialized]
		public Material GrassBillboardGeneration;
		[NonSerialized]
		public Material GrassBillboardCrossed;
		[NonSerialized]
		public Material GrassBillboardScreen;
		[NonSerialized]
		public Camera CollisionCamera;
		public Camera BillboardTextureCamera;
		[NonSerialized]
		public BillboardTexturePatchContainer BillboardTexturePatchContainer;

		[EmbeddedScriptableObject]
		public BladeContainer BladeContainer;
		[HideInInspector]
		public Texture2DArray BladeTexture2DArray0;
		[HideInInspector]
		public Texture2DArray BladeTexture2DArray1;
		[HideInInspector]
		public Texture2DArray BlossomTexture2DArray0;
		[HideInInspector]
		public Texture2DArray BlossomTexture2DArray1;
		
		[Header("PatchContainer")]
		
		[ClassExtends(typeof(PatchContainer), " ")]
		public ClassTypeReference PatchContainerType;
		[EmbeddedScriptableObject(false, true)]
		public PatchContainer PatchContainer;
		
		[Header("Inputs")]
		
		[ClassExtends(typeof(DimensionsInput), " ")]
		public ClassTypeReference DimensionsInputType;
		[EmbeddedScriptableObject(false, true)]
		public DimensionsInput DimensionsInput;

		[ClassExtends(typeof(GrassMapInput), " ")]
		public ClassTypeReference GrassMapInputType;
		[EmbeddedScriptableObject(false, true)]
		public GrassMapInput GrassMapInput;
		
		[ClassExtends(typeof(HeightInput), " ")]
		public ClassTypeReference HeightInputType;
		[EmbeddedScriptableObject(false, true)]
		public HeightInput HeightInput;
		
		[ClassExtends(typeof(NormalInput), " ")]
		public ClassTypeReference NormalInputType;
		[EmbeddedScriptableObject(false, true)]
		public NormalInput NormalInput;
		
		[ClassExtends(typeof(PositionInput), " ")]
		public ClassTypeReference PositionInputType;
		[EmbeddedScriptableObject(false, true)]
		public PositionInput PositionInput;
		
		[HideInInspector]
		public bool IsReady;
		[HideInInspector] 
		public int KernelPhysics;
		[HideInInspector] 
		public int KernelSimulationSetup;
		[NonSerialized] 
		public Random Random;
		public GrassInstance GrassInstance;

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
			
			if (GrassMapInputType.Type == null)
				GrassMapInput = null;
			else if (GrassMapInput == null || GrassMapInput.GetType() != GrassMapInputType.Type)
				GrassMapInput = Activator.CreateInstance(GrassMapInputType) as GrassMapInput;
			
			if (NormalInputType.Type == null)
				NormalInput = null;
			else if (NormalInput == null || NormalInput.GetType() != NormalInputType.Type)
				NormalInput = Activator.CreateInstance(NormalInputType) as NormalInput;
			
			if (PositionInputType.Type == null)
				PositionInput = null;
			else if (PositionInput == null || PositionInput.GetType() != PositionInputType.Type)
				PositionInput = Activator.CreateInstance(PositionInputType) as PositionInput;
			
			if (PatchContainerType.Type == null)
				PatchContainer = null;
			else if (PatchContainer == null || PatchContainer.GetType() != PatchContainerType.Type)
				PatchContainer = Activator.CreateInstance(PatchContainerType) as PatchContainer;
		}

		public void Init()
		{
			if (Settings == null) Settings = new SimulationSettings();
			if (EditorSettings == null) EditorSettings = new EditorSettings();
			if (CollisionCamera == null) CollisionCamera = GameObject.FindWithTag("GrassSimulationCollisionCamera").GetComponent<Camera>();
			if (BillboardTextureCamera == null) BillboardTextureCamera = GameObject.FindWithTag("BillboardTextureCamera").GetComponent<Camera>();
			BillboardTexturePatchContainer = null;
			BillboardTexturePatchContainer = CreateInstance<BillboardTexturePatchContainer>();
			
			if (BladeContainer == null) BladeContainer = CreateInstance<BladeContainer>();
			BladeContainer.Init(this);
			BladeTexture2DArray0 = BladeContainer.GetGeoemetryTexture2DArray(0);
			BladeTexture2DArray1 = BladeContainer.GetGeoemetryTexture2DArray(2);
			BlossomTexture2DArray0 = BladeContainer.GetGeoemetryTexture2DArray(1);
			BlossomTexture2DArray1 = BladeContainer.GetGeoemetryTexture2DArray(3);
			var blossomCount = BladeContainer.GetBlossomCount();
			Debug.Log("blossomCount " + blossomCount);
			if (!Transform || !Camera || !CollisionCamera || !BillboardTextureCamera || !GrassSimulationComputeShader || !CollisionDepthShader || !GrassSimulationShader || !DimensionsInput || !GrassMapInput || !HeightInput || !NormalInput || !PositionInput || !PatchContainer || BladeTexture2DArray0 == null || BladeTexture2DArray1 == null)
			{
				Debug.LogWarning("GrassSimulation: Not all dependencies are set.");
				if (!Transform) Debug.Log("GrassSimulation: Transform not set.");
				if (!Camera) Debug.Log("GrassSimulation: Camera not set.");
				if (!CollisionCamera) Debug.Log("GrassSimulation: Could not find Camera on GameObject with Tag GrassSimulationCollisionCamera");
				if (!BillboardTextureCamera) Debug.Log("GrassSimulation: Could not find Camera on GameObject with Tag BillboardTextureCamera");
				if (!GrassSimulationComputeShader) Debug.Log("GrassSimulation: GrassSimulationComputeShader not set.");
				if (!CollisionDepthShader) Debug.Log("GrassSimulation: CollisionDepthShader not set.");
				if (!GrassSimulationShader) Debug.Log("GrassSimulation: GrassSimulationShader not set.");
				if (!DimensionsInput) Debug.Log("GrassSimulation: DimensionsInput not set.");
				if (!GrassMapInput) Debug.Log("GrassSimulation: GrassMapInput not set.");
				if (!HeightInput) Debug.Log("GrassSimulation: HeightInput not set.");
				if (!NormalInput) Debug.Log("GrassSimulation: NormalInput not set.");
				if (!PositionInput) Debug.Log("GrassSimulation: PositionInput not set.");
				if (!PatchContainer) Debug.Log("GrassSimulation: PatchContainer not set.");
				if (BladeTexture2DArray0 == null || BladeTexture2DArray1 == null) Debug.Log("GrassSimulation: No Grass Blades set. Cannot create Textures.");
				IsReady = false;
				return;
			}
			
			//Create a single random object
			Random = new Random(Settings.RandomSeed);

			//Find kernels for ComputeShaders
			KernelPhysics = GrassSimulationComputeShader.FindKernel("PhysicsMain");
			KernelSimulationSetup = GrassSimulationComputeShader.FindKernel("SimulationSetup"); 
			
			//Create Material Variants
			
			GrassGeometry = new Material(GrassSimulationShader);
			GrassBillboardGeneration = new Material(GrassGeometry);
			GrassBillboardCrossed = new Material(GrassGeometry);
			GrassBillboardScreen = new Material(GrassGeometry);


			if (blossomCount > 0)
			{
				GrassBlossom = new Material(GrassGeometry);
				GrassBlossomBillboardGeneration = new Material(GrassGeometry);
				GrassBlossom.EnableKeyword("GRASS_BLOSSOM");
				GrassBlossom.DisableKeyword("GRASS_GEOMETRY");
				GrassBlossom.DisableKeyword("GRASS_BILLBOARD_CROSSED");
				GrassBlossom.DisableKeyword("GRASS_BILLBOARD_SCREEN");
				GrassBlossom.SetTexture("GrassBlossom0", BlossomTexture2DArray0);
				GrassBlossom.SetTexture("GrassBlossom1", BlossomTexture2DArray1);
				GrassBlossom.SetInt("EnableHeightTransition", Settings.EnableHeightTransition ? 1 : 0);
				GrassBlossom.SetInt("VertexCount", (int) Settings.GetMinAmountBladesPerPatch());
				GrassBlossom.SetInt("BlossomCount", blossomCount);
				GrassBlossom.SetFloat("BladeTextureMaxMipmapLevel", Settings.BladeTextureMaxMipmapLevel);
				GrassBlossom.SetFloat("BladeHeightCullingThreshold", Settings.BladeHeightCullingThreshold);
				GrassBlossom.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
				GrassBlossom.SetFloat("LodInstancesGeometry",
					Settings.LodInstancesGeometry / (float) Settings.LodGeometryTransitionSegments);
				GrassBlossom.SetFloat("LodGeometryTransitionSegments", Settings.LodGeometryTransitionSegments);
				GrassBlossom.SetFloat("LodDistanceGeometryStart", Settings.LodDistanceGeometryStart);
				GrassBlossom.SetFloat("LodDistanceGeometryEnd", Settings.LodDistanceGeometryEnd);
				GrassBlossom.SetFloat("LodTessellationMin", Settings.LodTessellationMin);
				GrassBlossom.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
				GrassBlossom.SetFloat("LodDistanceTessellationMin", Settings.LodDistanceTessellationMin);
				GrassBlossom.SetFloat("LodDistanceTessellationMax", Settings.LodDistanceTessellationMax);

				GrassBlossomBillboardGeneration.EnableKeyword("BILLBOARD_GENERATION");
				GrassBlossomBillboardGeneration.EnableKeyword("GRASS_BLOSSOM");
				GrassBlossomBillboardGeneration.DisableKeyword("GRASS_GEOMETRY");
				GrassBlossomBillboardGeneration.DisableKeyword("GRASS_BILLBOARD_CROSSED");
				GrassBlossomBillboardGeneration.DisableKeyword("GRASS_BILLBOARD_SCREEN");
				GrassBlossomBillboardGeneration.SetTexture("GrassBlossom0", BlossomTexture2DArray0);
				GrassBlossomBillboardGeneration.SetTexture("GrassBlossom1", BlossomTexture2DArray1);
				GrassBlossomBillboardGeneration.SetInt("EnableHeightTransition", Settings.EnableHeightTransition ? 1 : 0);
				GrassBlossomBillboardGeneration.SetInt("VertexCount", (int) Settings.BillboardGrassCount);
				GrassBlossomBillboardGeneration.SetInt("BlossomCount", blossomCount);
				GrassBlossomBillboardGeneration.SetFloat("BladeTextureMaxMipmapLevel", Settings.BladeTextureMaxMipmapLevel);
				GrassBlossomBillboardGeneration.SetFloat("BladeHeightCullingThreshold", Settings.BladeHeightCullingThreshold);
				GrassBlossomBillboardGeneration.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
				GrassBlossomBillboardGeneration.SetFloat("LodInstancesGeometry",
					Settings.LodInstancesGeometry / (float) Settings.LodGeometryTransitionSegments);
				GrassBlossomBillboardGeneration.SetFloat("LodGeometryTransitionSegments", Settings.LodGeometryTransitionSegments);
				//GrassBlossomBillboardGeneration.SetFloat("LodDistanceGeometryStart", 0);
				GrassBlossomBillboardGeneration.SetFloat("LodDistanceGeometryStart", 1);
				GrassBlossomBillboardGeneration.SetFloat("LodDistanceGeometryEnd", 100);
				GrassBlossomBillboardGeneration.SetFloat("LodTessellationMin", Settings.LodTessellationMin);
				GrassBlossomBillboardGeneration.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
				GrassBlossomBillboardGeneration.SetFloat("LodDistanceTessellationMin", Settings.LodDistanceTessellationMin);
				GrassBlossomBillboardGeneration.SetFloat("LodDistanceTessellationMax", Settings.LodDistanceTessellationMax);
			} else
			{
				GrassBlossom = null;
				GrassBlossomBillboardGeneration = null;
			}

			GrassGeometry.EnableKeyword("GRASS_GEOMETRY");
			GrassGeometry.DisableKeyword("GRASS_BLOSSOM");
			GrassGeometry.DisableKeyword("GRASS_BILLBOARD_CROSSED");
			GrassGeometry.DisableKeyword("GRASS_BILLBOARD_SCREEN");
			GrassGeometry.SetTexture("GrassBlades0", BladeTexture2DArray0);
			GrassGeometry.SetTexture("GrassBlades1", BladeTexture2DArray1);
			GrassGeometry.SetInt("EnableHeightTransition", Settings.EnableHeightTransition ? 1 : 0);
			GrassGeometry.SetInt("VertexCount", (int) Settings.GetMinAmountBladesPerPatch());
			GrassGeometry.SetFloat("BladeTextureMaxMipmapLevel", Settings.BladeTextureMaxMipmapLevel);
			GrassGeometry.SetFloat("BladeHeightCullingThreshold", Settings.BladeHeightCullingThreshold);
			GrassGeometry.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
			GrassGeometry.SetFloat("LodInstancesGeometry", Settings.LodInstancesGeometry / (float) Settings.LodGeometryTransitionSegments);
			GrassGeometry.SetFloat("LodGeometryTransitionSegments", Settings.LodGeometryTransitionSegments);
			GrassGeometry.SetFloat("LodDistanceGeometryStart", Settings.LodDistanceGeometryStart);
			GrassGeometry.SetFloat("LodDistanceGeometryEnd", Settings.LodDistanceGeometryEnd);
			GrassGeometry.SetFloat("LodTessellationMin", Settings.LodTessellationMin);
			GrassGeometry.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
			GrassGeometry.SetFloat("LodDistanceTessellationMin", Settings.LodDistanceTessellationMin);
			GrassGeometry.SetFloat("LodDistanceTessellationMax", Settings.LodDistanceTessellationMax);
			
			GrassBillboardGeneration.EnableKeyword("BILLBOARD_GENERATION");
			GrassBillboardGeneration.EnableKeyword("GRASS_GEOMETRY");
			GrassBillboardGeneration.DisableKeyword("GRASS_BLOSSOM");
			GrassBillboardGeneration.DisableKeyword("GRASS_BILLBOARD_CROSSED");
			GrassBillboardGeneration.DisableKeyword("GRASS_BILLBOARD_SCREEN");
			GrassBillboardGeneration.SetTexture("GrassBlades0", BladeTexture2DArray0);
			GrassBillboardGeneration.SetTexture("GrassBlades1", BladeTexture2DArray1);
			GrassBillboardGeneration.SetInt("EnableHeightTransition", Settings.EnableHeightTransition ? 1 : 0);
			GrassBillboardGeneration.SetInt("VertexCount", (int) Settings.BillboardGrassCount);
			GrassBillboardGeneration.SetFloat("BladeTextureMaxMipmapLevel", Settings.BladeTextureMaxMipmapLevel);
			GrassBillboardGeneration.SetFloat("BladeHeightCullingThreshold", Settings.BladeHeightCullingThreshold);
			GrassBillboardGeneration.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
			GrassBillboardGeneration.SetFloat("LodInstancesGeometry", Settings.LodInstancesGeometry / (float) Settings.LodGeometryTransitionSegments);
			GrassBillboardGeneration.SetFloat("LodGeometryTransitionSegments", Settings.LodGeometryTransitionSegments);
			//GrassBillboardGeneration.SetFloat("LodDistanceGeometryStart", 0);
			GrassBillboardGeneration.SetFloat("LodDistanceGeometryStart", 1);
			GrassBillboardGeneration.SetFloat("LodDistanceGeometryEnd", 100);
			GrassBillboardGeneration.SetFloat("LodTessellationMin", Settings.LodTessellationMin);
			GrassBillboardGeneration.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
			GrassBillboardGeneration.SetFloat("LodDistanceTessellationMin", Settings.LodDistanceTessellationMin);
			GrassBillboardGeneration.SetFloat("LodDistanceTessellationMax", Settings.LodDistanceTessellationMax);
			
			GrassBillboardCrossed.SetOverrideTag("Queue", "AlphaTest");
			GrassBillboardCrossed.SetOverrideTag("RenderType", "TransparentCutout");
			GrassBillboardCrossed.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			GrassBillboardCrossed.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			GrassBillboardCrossed.SetInt("_ZWrite", 0);
			GrassBillboardCrossed.SetInt("_AlphaToMask", 1);
			GrassBillboardCrossed.renderQueue = 3000;
			GrassBillboardCrossed.DisableKeyword("GRASS_GEOMETRY");
			GrassBillboardCrossed.DisableKeyword("GRASS_BLOSSOM");
			GrassBillboardCrossed.EnableKeyword("GRASS_BILLBOARD_CROSSED");
			GrassBillboardCrossed.DisableKeyword("GRASS_BILLBOARD_SCREEN");
			GrassBillboardCrossed.SetTexture("GrassBlades0", BladeTexture2DArray0);
			GrassBillboardCrossed.SetInt("EnableHeightTransition", Settings.EnableHeightTransition ? 1 : 0);
			GrassBillboardCrossed.SetInt("VertexCount", (int) Settings.GetMinAmountBillboardsPerPatch());
			GrassBillboardCrossed.SetFloat("BillboardAlphaCutoff", Settings.BillboardAlphaCutoff);
			GrassBillboardCrossed.SetFloat("BillboardHeightAdjustment", Settings.BillboardHeightAdjustment);
			GrassBillboardCrossed.SetFloat("BladeHeightCullingThreshold", Settings.BladeHeightCullingThreshold);
			GrassBillboardCrossed.SetFloat("LodInstancesBillboardCrossed", Settings.LodInstancesBillboardCrossed / (float) Settings.LodBillboardCrossedTransitionSegments);
			GrassBillboardCrossed.SetFloat("LodBillboardCrossedTransitionSegments", Settings.LodBillboardCrossedTransitionSegments);
			GrassBillboardCrossed.SetFloat("LodDistanceBillboardCrossedStart", Settings.LodDistanceBillboardCrossedStart);
			GrassBillboardCrossed.SetFloat("LodDistanceBillboardCrossedPeak", Settings.LodDistanceBillboardCrossedPeak);
			GrassBillboardCrossed.SetFloat("LodDistanceBillboardCrossedEnd", Settings.LodDistanceBillboardCrossedEnd);
			
			GrassBillboardScreen.SetOverrideTag("Queue", "AlphaTest");
			GrassBillboardScreen.SetOverrideTag("RenderType", "TransparentCutout");
			GrassBillboardScreen.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			GrassBillboardScreen.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			GrassBillboardScreen.SetInt("_ZWrite", 0);
			GrassBillboardScreen.SetInt("_AlphaToMask", 1);
			GrassBillboardScreen.renderQueue = 2900;
			GrassBillboardScreen.DisableKeyword("GRASS_GEOMETRY");
			GrassBillboardScreen.DisableKeyword("GRASS_BLOSSOM");
			GrassBillboardScreen.DisableKeyword("GRASS_BILLBOARD_CROSSED");
			GrassBillboardScreen.EnableKeyword("GRASS_BILLBOARD_SCREEN");
			GrassBillboardScreen.SetTexture("GrassBlades0", BladeTexture2DArray0);
			GrassBillboardScreen.SetInt("EnableHeightTransition", Settings.EnableHeightTransition ? 1 : 0);
			GrassBillboardScreen.SetInt("VertexCount", (int) Settings.GetMinAmountBillboardsPerPatch());
			GrassBillboardScreen.SetFloat("BillboardAlphaCutoff", Settings.BillboardAlphaCutoff);
			GrassBillboardScreen.SetFloat("BillboardHeightAdjustment", Settings.BillboardHeightAdjustment);
			GrassBillboardScreen.SetFloat("BladeHeightCullingThreshold", Settings.BladeHeightCullingThreshold);
			GrassBillboardScreen.SetFloat("LodInstancesBillboardScreen", Settings.LodInstancesBillboardScreen / (float) Settings.LodBillboardScreenTransitionSegments);
			GrassBillboardScreen.SetFloat("LodBillboardScreenTransitionSegments", Settings.LodBillboardScreenTransitionSegments);
			GrassBillboardScreen.SetFloat("LodDistanceBillboardScreenStart", Settings.LodDistanceBillboardScreenStart);
			GrassBillboardScreen.SetFloat("LodDistanceBillboardScreenPeak", Settings.LodDistanceBillboardScreenPeak);
			GrassBillboardScreen.SetFloat("LodDistanceBillboardScreenEnd", Settings.LodDistanceBillboardScreenEnd);
			
			GrassSimulationComputeShader.SetFloat("GrassDataResolution", Settings.GrassDataResolution);
			GrassSimulationComputeShader.SetFloat("BladeHeightCullingThreshold", Settings.BladeHeightCullingThreshold);
			GrassSimulationComputeShader.SetFloat("RecoveryFactor", Settings.RecoveryFactor);
			GrassSimulationComputeShader.SetFloat("LodTessellationMin", Settings.LodTessellationMin);
			GrassSimulationComputeShader.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
			GrassSimulationComputeShader.SetFloat("LodDistanceTessellationMin", Settings.LodDistanceTessellationMin);
			GrassSimulationComputeShader.SetFloat("LodDistanceTessellationMax", Settings.LodDistanceTessellationMax);
	
			//If possible initialize the Data Providers
			// ReSharper disable SuspiciousTypeConversion.Global
			if (DimensionsInput is IInitializableWithCtx) ((IInitializableWithCtx) DimensionsInput).Init(this);
			else if (DimensionsInput is IInitializable) ((IInitializable) DimensionsInput).Init();
			
			if (GrassMapInput is IInitializableWithCtx) ((IInitializableWithCtx) GrassMapInput).Init(this);
			else if (GrassMapInput is IInitializable) ((IInitializable) GrassMapInput).Init();
			
			if (HeightInput is IInitializableWithCtx) ((IInitializableWithCtx) HeightInput).Init(this);
			else if (HeightInput is IInitializable) ((IInitializable) HeightInput).Init();
			
			if (NormalInput is IInitializableWithCtx) ((IInitializableWithCtx) NormalInput).Init(this);
			else if (NormalInput is IInitializable) ((IInitializable) NormalInput).Init();
			
			if (PositionInput is IInitializableWithCtx) ((IInitializableWithCtx) PositionInput).Init(this);
			else if (PositionInput is IInitializable) ((IInitializable) PositionInput).Init();


			GrassInstance = new GrassInstance(this);
			
			PatchContainer.Init(this);
			PatchContainer.SetupContainer();
			
			CollisionTextureRenderer = new CollisionTextureRenderer(this, PatchContainer.GetBounds());
			ProceduralWind = new ProceduralWind(this);
			ProceduralWind.Update();

			//Create Billboard Textures
			BillboardTexturePatchContainer.Init(this);
			BillboardTexturePatchContainer.SetupContainer();
			BillboardTexturePatchContainer.Draw();
			
			//Needs to be reset here, since BillboardTexturePatchContainer sets its own NormalHeightTexture
			GrassSimulationComputeShader.SetTexture(KernelPhysics, "NormalHeightTexture", GrassInstance.NormalHeightTexture);
			GrassSimulationComputeShader.SetTexture(KernelSimulationSetup, "NormalHeightTexture", GrassInstance.NormalHeightTexture);
			
			GrassBillboardCrossed.SetTexture("GrassBillboards", BillboardTexturePatchContainer.BillboardTextures);
			GrassBillboardCrossed.SetFloat("BillboardAspect", BillboardTexturePatchContainer.BillboardAspect);
			GrassBillboardCrossed.SetFloat("RepetitionCount", PositionInput.GetRepetitionCount());
			GrassBillboardScreen.SetTexture("GrassBillboards", BillboardTexturePatchContainer.BillboardTextures);
			GrassBillboardScreen.SetFloat("BillboardAspect", BillboardTexturePatchContainer.BillboardAspect);
			GrassBillboardScreen.SetFloat("RepetitionCount", PositionInput.GetRepetitionCount());
			
			//Everything is ready.
			IsReady = true;
		}

		public int GetBufferLength()
		{
			return (int) (Mathf.Max(Settings.GetMaxAmountBladesPerPatch(),
				                      Settings.GetMaxAmountBillboardsPerPatch() * PositionInput.GetRepetitionCount()) *
			                      Settings.InstancedGrassFactor * Settings.InstancedGrassFactor);
		}

		public void OnGUI()
		{
			PatchContainer.OnGUI();
		}
	}
}