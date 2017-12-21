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
		public float BladeMaxBend = 2f;
		public float BladeMaxHeight = 1f;
		public float BladeMaxWidth = 0.5f;
		public float BladeMinBend = 0.5f;
		public float BladeMinHeight = 0.5f;
		public float BladeMinWidth = 0.1f;
		[Range(0, 6)]
		public float BladeTextureMaxMipmapLevel = 5;
		public uint BillboardTextureGrassCount = 64;

		[Header("Lighting Settings")]
		[Range(0, 1)]
		public float Specular;
		public float Gloss;

		[Header("Physics Settings")]
		public Vector4 Gravity = new Vector4(0f, -1f, 0f, 2f); //xyz: vector    w: acceleration

		[Range(0, 1)]
		public float WindAmplitude = 1f;

		[Header("Wind Fluid Settings")]
		public float FluidTimeFactor = 1;
		public int FluidIterationSteps = 10;
		public float FluidViscosity = 0.03f;
		public float FluidPressureScale = 0.15f;

		[Header("Texture Resolutions")]
		//TODO: Handle width and height seperately for non-quad containers
		public int GrassMapResolution = 256;
		public int CollisionDepthResolution = 512;
		public int WindFieldResolution = 128;
		public int GrassDataResolution = 16;
		public int BillboardTextureResolution = 64;
		
		[Header("LOD Settings")]
		public bool GrassDataTrilinearFiltering = true;
		[Tooltip("The width and depth of a patch.")]
		public uint PatchSize = 8;

		[Tooltip("How much more instanced grass data than the max possible amount of blades per patch gets created.")]
		public uint InstancedGrassFactor = 4;

		public float LodTessellationMin = 1;
		public float LodTessellationMax = 64;
		public float LodDistanceTessellationMin = 0;
		public float LodDistanceTessellationMax = 20;
		public uint LodInstancesGeometry = 64;
		public uint LodInstancesBillboardCrossed = 1;
		public uint LodInstancesBillboardScreen = 1;
		public float LodDistanceGeometryStart = 0;
		public float LodDistanceGeometryPeak = 1;
		public float LodDistanceGeometryEnd = 200;
		public float LodDistanceBillboardCrossedStart = 150;		
		public float LodDistanceBillboardCrossedPeak = 200;		
		public float LodDistanceBillboardCrossedEnd = 300;		
		public float LodDistanceBillboardScreenStart = 250;		
		public float LodDistanceBillboardScreenPeak = 300;		
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

		//TODO: Change for non-quad Containers
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