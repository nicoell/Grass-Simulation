using System;
using UnityEngine;

namespace GrassSimulation
{
	[Serializable]
	public class SimulationSettings
	{
		[Header("General Settings")]
		public int RandomSeed = 42;
		
		[Header("Grass Settings")]
		public float BladeMaxBend = 10f;
		public float BladeMaxHeight = 10f;
		public float BladeMaxWidth = 3f;
		public float BladeMinBend = 3f;
		public float BladeMinHeight = 2f;
		public float BladeMinWidth = 0.1f;
		public Texture2D GrassBlade;
		
		[Header("Physics Settings")]
		public Vector4 Gravity = new Vector4(0f, -1f, 0f, 2f); //xyz: vector    w: acceleration
		
		[Header("LOD Settings")]
		[Tooltip("The width and depth of a patch.")]
		public uint PatchSize = 8;
		[SerializeField]
		[Tooltip("How much more instanced grass data than the max possible amount of blades per patch gets created.")]
		private uint _instancedGrassFactor = 2;
		
		[Tooltip("There are max (PatchSize * PatchSize * GrassDensity) Blades per Patch.")]
		public uint LodDensityFullDetailDistance = 16;
		public uint LodDensityBillboardDistance = 4;
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
		
		public uint GetAmountInstancedBlades()
		{
			return GetMaxAmountBladesPerPatch() * _instancedGrassFactor;
		}
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