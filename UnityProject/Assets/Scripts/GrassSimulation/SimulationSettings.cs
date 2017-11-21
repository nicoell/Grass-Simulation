using System;
using UnityEditor;
using UnityEngine;

namespace GrassSimulation
{
	[Serializable]
	public class SimulationSettings
	{
		[Header("General Settings")]
		public int RandomSeed = 42;
		
		[Header("Grass Settings")]
		public float BladeMaxBend = 2f;
		public float BladeMaxHeight = 1f;
		public float BladeMaxWidth = 0.5f;
		public float BladeMinBend = 0.5f;
		public float BladeMinHeight = 0.5f;
		public float BladeMinWidth = 0.1f;
		public Texture2D GrassBlade;
		
		[Header("Physics Settings")]
		public Vector4 Gravity = new Vector4(0f, -1f, 0f, 2f); //xyz: vector    w: acceleration

		[Header("LOD Settings")]
		public int GrassDataResolution = 16;
		public bool GrassDataTrilinearFiltering = true;
		//public int GrassDataMultisamplingLevel = 0;
		[Tooltip("The width and depth of a patch.")]
		public uint PatchSize = 8;

		[Tooltip("How much more instanced grass data than the max possible amount of blades per patch gets created.")]
		public uint InstancedGrassFactor = 4;
		
		[Tooltip("There are max (PatchSize * PatchSize * GrassDensity) Blades per Patch.")]
		public uint LodDensityFullDetailDistance = 8;
		public uint LodDensityBillboardDistance = 2;
		public uint LodDensityMaxDistance = 1;
		
		[Tooltip("The max range grass will be rendered.")]
		public float LodDistanceMax = 1000f;
		[Tooltip("The distance billboard grass will be used over single blades. Up to this distance the density will be scaled down to 1.")]
		public float LodDistanceBillboard = 500f;
		[Tooltip("The distance up to which grass will be rendered in full detail.")]
		public float LodDistanceFullDetail = 50f;

		public bool EnableHeightTransition = true;

		public uint GetMaxAmountBladesPerPatch()
		{
			return PatchSize * PatchSize * LodDensityFullDetailDistance;
		}

		public uint GetMinAmountBladesPerPatch()
		{
			return PatchSize * PatchSize;
		}

		public uint GetSharedBufferLength() { return GetMaxAmountBladesPerPatch() * InstancedGrassFactor * InstancedGrassFactor; }

		public uint GetSharedTextureLength() { return (uint) (GrassDataResolution * GrassDataResolution * InstancedGrassFactor * InstancedGrassFactor); }
		
		public int GetSharedTextureWidthHeight() { return (int) (GrassDataResolution * InstancedGrassFactor); }

		public uint GetPerPatchTextureLength() { return (uint) (GrassDataResolution * GrassDataResolution); }
		
		public int GetPerPatchTextureWidthHeight() { return GrassDataResolution; }
	}
	
	[Serializable]
	public class EditorSettings
	{
		public bool DrawBoundingPatchGizmo = true;
		public bool DrawGrassDataDetailGizmo;
		public bool DrawGrassDataGizmo = true;
		public bool DrawGrassPatchGizmo = true;
	}
}