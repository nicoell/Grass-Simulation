using System;
using UnityEngine;

namespace GrassSimulation.Core
{
	[Serializable]
	public class SimulationSettings
	{
		[Header("General Settings")]
		public int RandomSeed = 42;

		[Header("Grass Settings")]
		[Range(0,1)]
		public float BillboardAlphaCutoff = 0.4f;
		[Range(0,1)]
		public float BladeMinBend = 0.5f;
		[Range(0,1)]
		public float BladeMaxBend = 1f;
		[Range(0,5)]
		public float BladeMinHeight = 0.5f;
		[Range(0,5)]
		public float BladeMaxHeight = 1f;
		[Range(0,3)]
		public float BladeMinWidth = 0.1f;
		[Range(0,3)]
		public float BladeMaxWidth = 0.5f;
		[Range(0, 6)]
		public float BladeTextureMaxMipmapLevel = 5;
		[Range(0,1)]
		public float BladeHeightCullingThreshold = 0.01f;
		
		[Header("Billboard Grass Settings")]
		public uint BillboardGrassCount = 64;
		[Range(0.1f, 5)]
		public float BillboardGrassSpacingFactor = 0.5f;
		[Range(0,1)]
		public float BillboardGrassWidthCorrectionFactor = 0.5f;
		

		[Header("Gravity")]
		public Vector4 Gravity = new Vector4(0f, -1f, 0f, 2f); //xyz: vector    w: acceleration

		[Header("Collisions")]
		[Range(0.01f, 10f)]
		public float RecoveryFactor = 0.1f;

		[Header("Procedural Wind")]
		[Range(0, 32)]
		public float WindFrequencyDirMin = 5f;
		[Range(0, 32)]
		public float WindFrequencyDirMax = 8f;
		[Range(0, 32)]
		public float WindFrequencyMagMin = 1f;
		[Range(0, 32)]
		public float WindFrequencyMagMax = 2f;
		[Range(0, 1024)]
		public float WindMagnitudeMax = 8f;

		[Header("Texture Resolutions")]
		//TODO: Handle width and height seperately for non-quad containers
		public int GrassMapResolution = 256;
		public int CollisionDepthResolution = 512;
		public int GrassDataResolution = 16;
		public int BillboardTextureResolution = 64;
		
		[Header("LOD Settings")]
		[Tooltip("The width and depth of a patch.")]
		public uint PatchSize = 8;
		[Tooltip("How much more instanced grass data than the max possible amount of blades per patch gets created.")]
		[Range(1,32)]
		public uint InstancedGrassFactor = 4;
		[Range(0,64)]
		public float LodTessellationMin = 1;
		[Range(0,64)]
		public float LodTessellationMax = 64;
		[Range(0,128)]
		public float LodDistanceTessellationMin = 0;
		[Range(0,128)]
		public float LodDistanceTessellationMax = 20;
		[Range(1,1023)]
		public uint LodInstancesGeometry = 64;
		[Range(1,1023)]
		public uint LodInstancesBillboardCrossed = 1;
		[Range(1,1023)]
		public uint LodInstancesBillboardScreen = 1;
		[Range(0,1024)]
		public float LodDistanceGeometryStart = 1;
		[Range(0,1024)]
		public float LodDistanceGeometryEnd = 200;
		[Range(0,2048)]
		public float LodDistanceBillboardCrossedStart = 150;		
		[Range(0,2048)]
		public float LodDistanceBillboardCrossedPeak = 200;		
		[Range(0,2048)]
		public float LodDistanceBillboardCrossedEnd = 300;		
		[Range(0,2048)]
		public float LodDistanceBillboardScreenStart = 250;		
		[Range(0,2048)]
		public float LodDistanceBillboardScreenPeak = 300;		
		[Range(0,2048)]
		public float LodDistanceBillboardScreenEnd = 400;

		public bool EnableHeightTransition = true;

		public uint GetMaxAmountBladesPerPatch()
		{
			return PatchSize * PatchSize * LodInstancesGeometry;
		}

		public uint GetMinAmountBladesPerPatch()
		{
			return PatchSize * PatchSize;
		}
		
		public uint GetMinAmountBillboardsPerPatch()
		{
			return PatchSize;
		}

		public uint GetSharedBufferLength() { return GetMaxAmountBladesPerPatch() * InstancedGrassFactor * InstancedGrassFactor; }

		//TODO: Multiply with InstancedGrassFactor??
		public uint GetSharedTextureLength() { return (uint) (GrassDataResolution * GrassDataResolution * InstancedGrassFactor * InstancedGrassFactor); }
		
		public int GetSharedTextureWidthHeight() { return (int) (GrassDataResolution * InstancedGrassFactor); }

		public uint GetPerPatchTextureLength() { return (uint) (GrassDataResolution * GrassDataResolution); }
		
		public int GetPerPatchTextureWidthHeight() { return GrassDataResolution; }

		public float GetPerPatchTextureUvStep() { return 1f / GetPerPatchTextureWidthHeight(); }
		public float GetPerPatchTextureUvStepNarrowed() { return 0.5f / GetPerPatchTextureWidthHeight(); }
	}
	
	[Serializable]
	public class EditorSettings
	{
		[Header("Editor Settings")]
		public bool EnableLodDistanceGizmo = true;
		public bool EnableHierarchyGizmo = true;
		public bool EnablePatchGizmo = true;
		public bool EnableBladeUpGizmo = false;
		public bool EnableFullBladeGizmo = false;
	}
}