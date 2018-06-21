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
		[Range(0,1)]
		public float BillboardAlphaCutoff = 0.4f;
		public uint BillboardGrassCount = 64;
		[Range(0.1f, 5)]
		public float BillboardGrassSpacingFactor = 0.5f;
		[Range(0,1)]
		public float BillboardGrassWidthCorrectionFactor = 0.5f;
		[Range(1,2)]
		public float BillboardHeightAdjustment = 1.3f;

		[Header("Lightning")]
		[Range(0,1)]
		public float AmbientLightFactor = 0.1f;

		[Header("Gravity")]
		public Vector4 Gravity = new Vector4(0f, -1f, 0f, 2f); //xyz: vector    w: acceleration

		[Header("Collisions")]
		[Range(0.01f, 10f)]
		public float RecoveryFactor = 0.1f;

		[Header("Layered Wind")]
		[Range(1, 256)]
		public int WindLayerCount;
		
		[Header("Texture Resolutions")]
		//TODO: Handle width and height seperately for non-quad containers
		public int GrassMapResolution = 256;
		public int CollisionDepthResolution = 512;
		public int GrassDataResolution = 16;
		public int BillboardTextureResolution = 64;
		
		[Header("LOD Settings")]
		[Tooltip("The width and depth of a patch.")]
		public uint PatchSize = 8;
		public uint GrassPerInstancemodifer = 1;
		[Tooltip("How much more instanced grass data than the max possible amount of blades per patch gets created.")]
		[Range(1,32)]
		public uint InstancedGrassFactor = 4;
		[Range(1,128)]
		public uint ParameterVariance = 1;
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
		[Range(1,256)]
		public uint LodGeometryTransitionSegments = 8;
		[Range(1,256)]
		public uint LodBillboardCrossedTransitionSegments = 8;
		[Range(1,256)]
		public uint LodBillboardScreenTransitionSegments = 4;
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
			return GetMinAmountBladesPerPatch() * LodInstancesGeometry;
		}
		
		public uint GetMaxAmountBillboardsPerPatch()
		{
			return GetMinAmountBillboardsPerPatch() * (uint) Mathf.Max(LodInstancesBillboardCrossed, LodInstancesBillboardScreen);
		}

		public uint GetMinAmountBladesPerPatch()
		{
			return PatchSize * PatchSize * GrassPerInstancemodifer;
		}
		
		public uint GetMinAmountBillboardsPerPatch()
		{
			return PatchSize * GrassPerInstancemodifer;
		}

		//public uint GetSharedBufferLength() { return GetMaxAmountBladesPerPatch() * InstancedGrassFactor * InstancedGrassFactor; }

		//TODO: Multiply with InstancedGrassFactor??
		public uint GetSharedTextureLength() { return (uint) (GrassDataResolution * GrassDataResolution * ParameterVariance * ParameterVariance); }
		
		public int GetSharedTextureWidthHeight() { return (int) (GrassDataResolution * ParameterVariance); }

		public uint GetPerPatchTextureLength() { return (uint) (GrassDataResolution * GrassDataResolution); }
		
		public int GetPerPatchTextureWidthHeight() { return GrassDataResolution; }

		public float GetPerPatchTextureUvStep() { return 1f / GetPerPatchTextureWidthHeight(); }
		public float GetPerPatchTextureUvStepNarrowed() { return 0.5f / GetPerPatchTextureWidthHeight(); }

		public void LogSettings()
		{
			var logString = "### Settings ###" + "\n";
			
			logString += "General Settings" + "\n";
			logString += "\t RandomSeed: " + RandomSeed + "\n";
			
			logString += "Grass Settings" + "\n";
			logString += "\t Min Bend: " + BladeMinBend + "\n";
			logString += "\t Max Bend: " + BladeMaxBend + "\n";
			logString += "\t Min Height: " + BladeMinHeight + "\n";
			logString += "\t Max Height: " + BladeMaxHeight + "\n";
			logString += "\t Min Width: " + BladeMinWidth + "\n";
			logString += "\t Max Width: " + BladeMaxWidth + "\n";
			logString += "\t Texture Max Mipmap Level: " + BladeTextureMaxMipmapLevel + "\n";
			
			logString += "Billboard Grass Settings" + "\n";
			logString += "\t Alpha Threshold: " + BillboardAlphaCutoff + "\n";
			logString += "\t Texture Grass Count: " + BillboardGrassCount + "\n";
			logString += "\t Texture Spacing Factor: " + BillboardGrassSpacingFactor + "\n";
			logString += "\t Texture Volume Correction Factor: " + BillboardGrassWidthCorrectionFactor + "\n";
			
			logString += "Lighting Settings" + "\n";
			logString += "\t Ambient Light Intensity: " + AmbientLightFactor + "\n";
			
			logString += "Gravity" + "\n";
			logString += "\t Direction: " + Vector3.Normalize(new Vector3(Gravity.x, Gravity.y, Gravity.z)) + "\n";
			logString += "\t Strength: " + Gravity.w + "\n";
			
			logString += "Wind Settings" + "\n";
			logString += "\t Layer Count: " + WindLayerCount + "\n";
			
			logString += "Texture Resolutions" + "\n";
			logString += "\t Grass Map Resolution: " + GrassMapResolution + "\n";
			logString += "\t Collision Map Resolution: " + CollisionDepthResolution + "\n";
			logString += "\t Grass Data Resolution: " + GrassDataResolution + "\n";
			logString += "\t Billboard Texture Resolution: " + BillboardTextureResolution + "\n";
			
			logString += "Lod Settings" + "\n";
			logString += "\t Blade Height Culling Threshold: " + BladeHeightCullingThreshold + "\n";
			logString += "\t Patch Size: " + PatchSize + "\n";
			logString += "\t Parameter Variance: " + ParameterVariance + "\n";
			logString += "\t Tesselation Min: " + LodTessellationMin + "\n";
			logString += "\t Tesselation Max: " + LodTessellationMax + "\n";
			logString += "\t Distance Tessellation Min: " + LodDistanceTessellationMin + "\n";
			logString += "\t Distance Tessellation Max: " + LodDistanceTessellationMax + "\n";
			logString += "\t Geometry Instances: " + LodInstancesGeometry + "\n";
			logString += "\t Geometry Transition Segments: " + LodGeometryTransitionSegments + "\n";
			logString += "\t Geometry Distance Start: " + LodDistanceGeometryStart + "\n";
			logString += "\t Geometry Distance End: " + LodDistanceGeometryEnd + "\n";
			logString += "\t CrossedBillboard Instances: " + LodInstancesBillboardCrossed + "\n";
			logString += "\t CrossedBillboard Transition Segments: " + LodBillboardCrossedTransitionSegments + "\n";
			logString += "\t CrossedBillboard Distance Start: " + LodDistanceBillboardCrossedStart + "\n";
			logString += "\t CrossedBillboard Distance Peak: " + LodDistanceBillboardCrossedPeak + "\n";
			logString += "\t CrossedBillboard Distance End: " + LodDistanceBillboardCrossedEnd + "\n";
			logString += "\t ScreenBillboard Instances: " + LodInstancesBillboardScreen + "\n";
			logString += "\t ScreenBillboard Transition Segments: " + LodBillboardScreenTransitionSegments + "\n";
			logString += "\t ScreenBillboard Distance Start: " + LodDistanceBillboardScreenStart + "\n";
			logString += "\t ScreenBillboard Distance Peak: " + LodDistanceBillboardScreenPeak + "\n";
			logString += "\t ScreenBillboard Distance End: " + LodDistanceBillboardScreenEnd + "\n";
			
			logString += "Derived Data" + "\n";
			logString += "\t Grass Blades per Instance: " + GetMinAmountBladesPerPatch() + "\n";
			logString += "\t Billboards per Instance: " + GetMinAmountBillboardsPerPatch()+ "\n";
			logString += "\t Max Grass Blades per Patch: " + GetMaxAmountBladesPerPatch()+ "\n";
			logString += "\t Max CrossedBillboards per Patch: " + (GetMinAmountBillboardsPerPatch() * LodInstancesBillboardCrossed) + "\n";
			logString += "\t Max ScreenBillboards per Patch: " + (GetMinAmountBillboardsPerPatch() * LodInstancesBillboardScreen) + "\n";
			
			Debug.Log(logString);
		}
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