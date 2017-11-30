using System;
using GrassSimulation.Core.Attribute;
using GrassSimulation.Core.ClassTypeReference;
using GrassSimulation.Core.Inputs;
using GrassSimulation.Core.Patches;
using UnityEngine;
using Random = System.Random;

namespace GrassSimulation.Core
{
	[Serializable]
	public class SimulationContext : ScriptableObject
	{
		[NonSerialized]
		public GameObject Parent;
		
		[Header("Requirements")]
		public Transform Transform;
		public Camera Camera;
		public ComputeShader GrassSimulationComputeShader;
		public Shader CollisionDepthShader;
		[HideInInspector]
		public CollisionTextureRenderer CollisionTextureRenderer;
		public Material GrassGeometry;
		[NonSerialized]
		public Material GrassBillboardCrossed;
		[NonSerialized]
		public Material GrassBillboardScreen;
		[HideInInspector]
		public Camera CollisionCamera;

		[EmbeddedScriptableObject]
		public BladeContainer BladeContainer;
		public Texture2DArray BladeTexture2DArray0;
		public Texture2DArray BladeTexture2DArray1;
		
		[Header("PatchContainer")]
		
		[ClassExtends(typeof(PatchContainer), " ")]
		public ClassTypeReference.ClassTypeReference PatchContainerType;
		[EmbeddedScriptableObject(false, true)]
		public PatchContainer PatchContainer;
		
		[Header("Inputs")]
		
		[ClassExtends(typeof(DimensionsInput), " ")]
		public ClassTypeReference.ClassTypeReference DimensionsInputType;
		[EmbeddedScriptableObject(false, true)]
		public DimensionsInput DimensionsInput;

		[ClassExtends(typeof(GrassMapInput), " ")]
		public ClassTypeReference.ClassTypeReference GrassMapInputType;
		[EmbeddedScriptableObject(false, true)]
		public GrassMapInput GrassMapInput;
		
		[ClassExtends(typeof(HeightInput), " ")]
		public ClassTypeReference.ClassTypeReference HeightInputType;
		[EmbeddedScriptableObject(false, true)]
		public HeightInput HeightInput;
		
		[ClassExtends(typeof(NormalInput), " ")]
		public ClassTypeReference.ClassTypeReference NormalInputType;
		[EmbeddedScriptableObject(false, true)]
		public NormalInput NormalInput;
		
		[ClassExtends(typeof(PositionInput), " ")]
		public ClassTypeReference.ClassTypeReference PositionInputType;
		[EmbeddedScriptableObject(false, true)]
		public PositionInput PositionInput;
		
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

		public bool Init(GameObject parent)
		{
			Parent = parent;
			if (Settings == null)
			{
				Settings = new SimulationSettings();
			}
			if (EditorSettings == null) EditorSettings = new EditorSettings();
			if (CollisionCamera == null) CollisionCamera = GameObject.FindWithTag("GrassSimulationCollisionCamera").GetComponent<Camera>();
			
			if (BladeContainer == null) BladeContainer = CreateInstance<BladeContainer>();
			BladeContainer.Init(this);
			BladeTexture2DArray0 = BladeContainer.GetGeoemetryTexture2DArray(0);
			BladeTexture2DArray1 = BladeContainer.GetGeoemetryTexture2DArray(1);
			if (!Transform || !Camera || !CollisionCamera || !GrassSimulationComputeShader || !CollisionDepthShader || !GrassGeometry || !DimensionsInput || !GrassMapInput || !HeightInput || !NormalInput || !PositionInput || !PatchContainer || BladeTexture2DArray0 == null || BladeTexture2DArray1 == null)
			{
				Debug.LogWarning("GrassSimulation: Not all dependencies are set.");
				if (!Transform) Debug.Log("GrassSimulation: Transform not set.");
				if (!Camera) Debug.Log("GrassSimulation: Camera not set.");
				if (!CollisionCamera) Debug.Log("GrassSimulation: Could not find Camera on GameObject with Tag GrassSimulationCollisionCamera");
				if (!GrassSimulationComputeShader) Debug.Log("GrassSimulation: GrassSimulationComputeShader not set.");
				if (!CollisionDepthShader) Debug.Log("GrassSimulation: CollisionDepthShader not set.");
				if (!GrassGeometry) Debug.Log("GrassSimulation: Material not set.");
				if (!DimensionsInput) Debug.Log("GrassSimulation: DimensionsInput not set.");
				if (!GrassMapInput) Debug.Log("GrassSimulation: GrassMapInput not set.");
				if (!HeightInput) Debug.Log("GrassSimulation: HeightInput not set.");
				if (!NormalInput) Debug.Log("GrassSimulation: NormalInput not set.");
				if (!PositionInput) Debug.Log("GrassSimulation: PositionInput not set.");
				if (!PatchContainer) Debug.Log("GrassSimulation: PatchContainer not set.");
				if (BladeTexture2DArray0 == null || BladeTexture2DArray1 == null) Debug.Log("GrassSimulation: No Grass Blades set. Cannot create Textures.");
				IsReady = false;
				return false;
			}

			//BladeContainer = new BladeContainer();
			
			//Create a single random object
			Random = new Random(Settings.RandomSeed);

			//Find kernels for ComputeShaders
			KernelPhysics = GrassSimulationComputeShader.FindKernel("PhysicsMain");
			KernelSimulationSetup =  GrassSimulationComputeShader.FindKernel("SimulationSetup"); 
			
			GrassBillboardCrossed = new Material(GrassGeometry);
			GrassBillboardScreen = new Material(GrassGeometry);

			GrassGeometry.EnableKeyword("GRASS_GEOMETRY");
			GrassGeometry.DisableKeyword("GRASS_BILLBOARD_CROSSED");
			GrassGeometry.DisableKeyword("GRASS_BILLBOARD_SCREEN");
			GrassGeometry.SetTexture("GrassBlades0", BladeTexture2DArray0);
			GrassGeometry.SetTexture("GrassBlades1", BladeTexture2DArray1);
			GrassGeometry.SetInt("vertexCount", (int) Settings.GetMinAmountBladesPerPatch());
			GrassGeometry.SetFloat("billboardSize", Settings.BillboardSize);
			GrassBillboardCrossed.DisableKeyword("GRASS_GEOMETRY");
			GrassBillboardCrossed.EnableKeyword("GRASS_BILLBOARD_CROSSED");
			GrassBillboardCrossed.DisableKeyword("GRASS_BILLBOARD_SCREEN");
			GrassBillboardCrossed.SetTexture("GrassBlades0", BladeTexture2DArray0);
			GrassBillboardCrossed.SetTexture("GrassBlades1", BladeTexture2DArray1);
			GrassBillboardCrossed.SetInt("vertexCount", (int) Settings.GetMinAmountBillboardsPerPatch());
			GrassBillboardCrossed.SetFloat("billboardSize", Settings.BillboardSize);
			GrassBillboardScreen.DisableKeyword("GRASS_GEOMETRY");
			GrassBillboardScreen.DisableKeyword("GRASS_BILLBOARD_CROSSED");
			GrassBillboardScreen.EnableKeyword("GRASS_BILLBOARD_SCREEN");
			GrassBillboardScreen.SetTexture("GrassBlades0", BladeTexture2DArray0);
			GrassBillboardScreen.SetTexture("GrassBlades1", BladeTexture2DArray1);
			GrassBillboardScreen.SetInt("vertexCount", (int) Settings.GetMinAmountBillboardsPerPatch());
			GrassBillboardScreen.SetFloat("billboardSize", Settings.BillboardSize);
			
			GrassSimulationComputeShader.SetBool("applyTransition", Settings.EnableHeightTransition);
			GrassSimulationComputeShader.SetFloat("GrassDataResolution", Settings.GrassDataResolution);
			GrassSimulationComputeShader.SetFloat("LodTessellationMin", Settings.LodTessellationMin);
			GrassSimulationComputeShader.SetFloat("LodTessellationMax", Settings.LodTessellationMax);
			GrassSimulationComputeShader.SetFloat("LodDistanceTessellationMin", Settings.LodDistanceTessellationMin);
			GrassSimulationComputeShader.SetFloat("LodDistanceTessellationMax", Settings.LodDistanceTessellationMax);
			
			Shader.SetGlobalFloat("BladeTextureMaxMipmapLevel", Settings.BladeTextureMaxMipmapLevel);
			Shader.SetGlobalFloat("LodTessellationMax", Settings.LodTessellationMax);
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
			
			

			//TODO: Use same setup pattern for all classes
			
			
			GrassInstance = new GrassInstance(this);
			GrassInstance.Init();
			
			PatchContainer.Init(this);
			PatchContainer.SetupContainer();
			
			CollisionTextureRenderer = new CollisionTextureRenderer(this, PatchContainer.GetBounds());
			
			//Everything is ready.
			IsReady = true;
			return true;
		}

		public void OnGUI() { PatchContainer.OnGUI(); }
	}
}