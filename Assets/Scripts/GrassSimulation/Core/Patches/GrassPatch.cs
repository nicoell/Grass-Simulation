using UnityEngine;
using UnityEngine.Rendering;

namespace GrassSimulation.Core.Patches
{
	/**
	 * Info on GrassBlades:
	 * 	X,Z Coordinates ]0.0, 1.0[
	 * 	 - relative to the patch
	 * 	 - Applying _patchModelMatrix:
	 * 	 	 - translates to lower corner of bounding box
	 * 	 	 - scales to PatchSize
	 * 
	 * Y Coordinate ]0.0, 1.0[
	 * 	 - is the sampled height of the terrains heightmap
	 * 	 - Applying _patchModelMatrix:
	 * 		 - translates to Transform.position.y
	 * 		 - scales to TerrainSize
	 */
	public class GrassPatch : Patch
	{
		private const int MultiSampleLevel = 1;
		private readonly uint[] _argsBillboardCrossed = {0, 0, 0, 0, 0};
		private readonly ComputeBuffer _argsBillboardCrossedBuffer;
		private readonly uint[] _argsBillboardScreen = {0, 0, 0, 0, 0};
		private readonly ComputeBuffer _argsBillboardScreenBuffer;
		private readonly uint[] _argsGeometry = {0, 0, 0, 0, 0};

		private readonly ComputeBuffer _argsGeometryBuffer;

		//private readonly Bounds.BoundsVertices _boundsVertices;
		private readonly MaterialPropertyBlock _materialPropertyBlock;

		private readonly float _parameterOffsetX;
		private readonly float _parameterOffsetY;
		private readonly Vector4 _patchTexCoord; //x: xStart, y: yStart, z: width, w:height
		private readonly Vector2 _sampleSize;
		private readonly Vector2 _sampleStep;
		private readonly int _startIndex;
		private bool _applyTransition;
		private Mesh _dummyMesh;
		private Bounds _inputBounds;
		private Texture2D _normalHeightTexture;

		/*
		 * _patchModelMatrix Notes:
		 * 		Translation
		 * 			X: bounds.center.x - bounds.extents.x
		 * 			Y: Context.Transform.position.y
		 * 			Z: bounds.center.z - bounds.extents.z
		 * 		Rotation
		 * 			None as Unity Terrain doesn't take rotation into account either
		 * 		Scale
		 * 			X: PatchSize
		 * 			Y: TerrainHeight
		 * 			Z: PatchSize
		 */
		private Matrix4x4 _patchModelMatrix;

		private RenderTexture _simulationTexture;

		public GrassPatch(SimulationContext ctx, Vector4 patchTexCoord, Bounds bounds) : base(ctx)
		{
			_inputBounds = bounds;
			//Extend BoundingBox by blades maxheight to avoid false culling
			var boundsCorrected = bounds;
			//TODO: Unify bound correction
			boundsCorrected.size += new Vector3(Ctx.Settings.BladeMaxHeight * 2, Ctx.Settings.BladeMaxHeight * 2,
				Ctx.Settings.BladeMaxHeight * 2);
			Bounds = boundsCorrected;
			//_boundsVertices = new Bounds.BoundsVertices(bounds);
			_patchTexCoord = patchTexCoord;
			_sampleSize = new Vector2(_patchTexCoord.z * (1f / (2 * Ctx.Settings.GetPerPatchTextureWidthHeight())),
				_patchTexCoord.w * (1f / (2 * Ctx.Settings.GetPerPatchTextureWidthHeight())));
			_sampleStep = _sampleSize / MultiSampleLevel;
			_startIndex = Ctx.Random.Next(0,
				(int) (Ctx.Settings.GetSharedBufferLength() - Ctx.Settings.GetMaxAmountBladesPerPatch()));
			_materialPropertyBlock = new MaterialPropertyBlock();
			_parameterOffsetX = (float) Ctx.Random.NextDouble(); // * Ctx.Settings.InstancedGrassFactor);
			_parameterOffsetY = (float) Ctx.Random.NextDouble(); // * Ctx.Settings.InstancedGrassFactor);

			_patchModelMatrix = Matrix4x4.TRS(
				new Vector3(bounds.center.x - bounds.extents.x, Ctx.Transform.position.y, bounds.center.z - bounds.extents.z),
				Quaternion.identity,
				new Vector3(Ctx.Settings.PatchSize, Ctx.DimensionsInput.GetHeight(), Ctx.Settings.PatchSize));

			// Create the IndirectArguments Buffer
			_argsGeometryBuffer =
				new ComputeBuffer(1, _argsGeometry.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsGeometry[0] = Ctx.Settings.GetMinAmountBladesPerPatch(); //Vertex Count
			_argsGeometry[1] = Ctx.Settings.LodInstancesGeometry; //Instance Count
			_argsGeometryBuffer.SetData(_argsGeometry);

			_argsBillboardCrossedBuffer =
				new ComputeBuffer(1, _argsBillboardCrossed.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsBillboardCrossed[0] = Ctx.Settings.GetMinAmountBillboardsPerPatch(); //Vertex Count
			_argsBillboardCrossed[1] = 1; //Instance Count
			_argsBillboardCrossedBuffer.SetData(_argsBillboardCrossed);

			_argsBillboardScreenBuffer =
				new ComputeBuffer(1, _argsBillboardScreen.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsBillboardScreen[0] = Ctx.Settings.GetMinAmountBillboardsPerPatch(); //Vertex Count
			_argsBillboardScreen[1] = 1; //Instance Count
			_argsBillboardScreenBuffer.SetData(_argsBillboardScreen);
			
			
			CreateGrassDataTexture();
			CreateDummyMesh();
			SetupMaterialPropertyBlock();
		}

		public override bool IsLeaf { get { return true; } }

		public void Destroy()
		{
			//TODO: Clean up buffers and textures
			_argsGeometryBuffer.Release();
		}

		public void Draw()
		{
			//TODO: Add CPU LOD algorithm
			//TODO: Actually use _argsGeometryBuffer in computeShader or if CPU only, don't use Indirect Draw Methd
			//TODO: Add settings for options in computeShader
			ComputeLod();
			RunSimulationComputeShader();

			if (_argsGeometry[1] > 0)
				Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassGeometry, Bounds, _argsGeometryBuffer, 0,
					_materialPropertyBlock);

			if (_argsBillboardCrossed[1] > 0)
				Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassBillboardCrossed, Bounds, _argsBillboardCrossedBuffer, 0,
					_materialPropertyBlock);

			if (_argsBillboardScreen[1] > 0)
				Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassBillboardScreen, Bounds, _argsBillboardScreenBuffer, 0,
					_materialPropertyBlock);
		}

		private void ComputeLod()
		{
			//Distance between Camera and closest Point on BoundingBox from Camera
			var distance = Vector3.Distance(Ctx.Camera.transform.position, Bounds.ClosestPoint(Ctx.Camera.transform.position));

			//Calculate InstanceCounts of different LODs (Geometry, BillboardsCrossed, BillboardsScreen)
			var geometryInstanceCount = (uint) Mathf.Ceil(SingleLerp(Ctx.Settings.LodInstancesGeometry, distance,
				Ctx.Settings.LodDistanceGeometryPeak, Ctx.Settings.LodDistanceGeometryEnd));
			var billboardCrossedInstanceCount = (uint) Mathf.Ceil(DoubleLerp(Ctx.Settings.LodInstancesBillboardCrossed, distance,
				Ctx.Settings.LodDistanceBillboardCrossedStart, Ctx.Settings.LodDistanceBillboardCrossedPeak,
				Ctx.Settings.LodDistanceBillboardCrossedEnd));
			var billboardScreenInstanceCount = (uint) Mathf.Ceil(DoubleLerp(Ctx.Settings.LodInstancesBillboardScreen, distance,
				Ctx.Settings.LodDistanceBillboardScreenStart, Ctx.Settings.LodDistanceBillboardScreenPeak,
				Ctx.Settings.LodDistanceBillboardScreenEnd));

			_argsGeometry[1] = geometryInstanceCount;
			_argsBillboardCrossed[1] = billboardCrossedInstanceCount;
			_argsBillboardScreen[1] = billboardScreenInstanceCount;

			_argsGeometryBuffer.SetData(_argsGeometry);
			_argsBillboardCrossedBuffer.SetData(_argsBillboardCrossed);
			_argsBillboardScreenBuffer.SetData(_argsBillboardScreen);
		}

		private static float SingleLerp(uint value, float cur, float peak, float end)
		{
			var t1 = Mathf.Clamp01((cur - peak) / (end - peak));
			return value - Mathf.LerpUnclamped(0, value, t1);
		}

		private static float DoubleLerp(uint value, float cur, float start, float peak, float end)
		{
			var t0 = Mathf.Clamp01((cur - start) / (peak - start));
			var t1 = Mathf.Clamp01((cur - peak) / (end - peak));
			return value - (Mathf.LerpUnclamped(value, 0, t0) + Mathf.LerpUnclamped(0, value, t1));
		}

		private float MultiSampleHeight(Vector2 sampleLoc)
		{
			float output = 0;
			var count = 0;

			for (var y = 0f; y <= _sampleSize.y; y += _sampleStep.y)
			for (var x = 0f; x <= _sampleSize.x; x += _sampleStep.x)
			{
				output += Ctx.HeightInput.GetHeight(Mathf.Clamp01(sampleLoc.x + x), Mathf.Clamp01(sampleLoc.y + y));
				count++;
			}
			return output / count;
		}

		private Vector3 MultiSampleNormal(Vector2 sampleLoc)
		{
			var output = new Vector3(0, 0, 0);
			var count = 0;

			for (var y = Mathf.Clamp01(sampleLoc.y - _sampleSize.y);
				y < Mathf.Clamp01(sampleLoc.y + _sampleSize.y);
				y += _sampleStep.y)
			for (var x = Mathf.Clamp01(sampleLoc.x - _sampleSize.x);
				x < Mathf.Clamp01(sampleLoc.x + _sampleSize.x);
				x += _sampleStep.x)
			{
				output += Ctx.NormalInput.GetNormal(x, y);
				count++;
			}
			return output / count;
		}

		private void CreateGrassDataTexture()
		{
			_normalHeightTexture = new Texture2D(Ctx.Settings.GetPerPatchTextureWidthHeight(),
				Ctx.Settings.GetPerPatchTextureWidthHeight(),
				TextureFormat.RGBAFloat, false, true)
			{
				//TODO: mipmaps not used (yet?)
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
				
			};
			var textureData = new Color[Ctx.Settings.GetPerPatchTextureLength()];
			var i = 0;
			var bladePosition = new Vector2(0, 0);
			var uvLocal = new Vector2(0, 0);
			var uvGlobal = new Vector2(0, 0);
			var uvStep = Ctx.Settings.GetPerPatchTextureUvStep();
			var uvNarrowed = Ctx.Settings.GetPerPatchTextureUvStepNarrowed();
			//TODO: Smooth the edges with neighbouring pixels for smooth transitions between patches.
			for (var y = 0; y < Ctx.Settings.GetPerPatchTextureWidthHeight(); y++)
			{
				uvLocal.y = Mathf.Lerp(-uvNarrowed, 1+uvNarrowed, (y + 0.5f) * uvStep);
				//uvLocal.y = (y + 0.5f) / Ctx.Settings.GetPerPatchTextureWidthHeight();

				uvGlobal.y = Mathf.Clamp(Mathf.LerpUnclamped(_patchTexCoord.y, _patchTexCoord.y + _patchTexCoord.w, uvLocal.y), 0, 1f - uvStep);

				bladePosition.y = Mathf.Clamp01(_patchTexCoord.y + _patchTexCoord.w * uvLocal.y);
				for (var x = 0; x < Ctx.Settings.GetPerPatchTextureWidthHeight(); x++)
				{
					uvLocal.x = Mathf.Lerp(-uvNarrowed, 1+uvNarrowed, (x + 0.5f) * uvStep);
					//uvLocal.x = (x + 0.5f) / Ctx.Settings.GetPerPatchTextureWidthHeight();
					bladePosition.x = Mathf.Clamp01(_patchTexCoord.x + _patchTexCoord.z * uvLocal.x);
					uvGlobal.x = Mathf.Clamp(Mathf.LerpUnclamped(_patchTexCoord.x, _patchTexCoord.x + _patchTexCoord.z, uvLocal.x), 0, 1f - uvStep);
					
					var posY = Ctx.HeightInput.GetHeight(uvGlobal.x, uvGlobal.y);
					//var posY = MultiSampleHeight(bladePosition);
					var up = Ctx.NormalInput.GetNormal(uvGlobal.x, uvGlobal.y);
					//var up = MultiSampleNormal(bladePosition);

					textureData[i] = new Color(up.x, up.y, up.z, posY);
					i++;
				}
			}
			/*
			for (var x = -1; x < Ctx.Settings.GetPerPatchTextureWidthHeight() - 1; x++)
			{
				uvLocal.y = (x + 0.5f) * uvStep;
				bladePosition.y = Mathf.Clamp01(_patchTexCoord.y + _patchTexCoord.w * uvLocal.y);
				for (var y = -1; y < Ctx.Settings.GetPerPatchTextureWidthHeight() - 1; y++)
				{
					uvLocal.x = (y + 0.5f) * uvStep;
					bladePosition.x = Mathf.Clamp01(_patchTexCoord.x + _patchTexCoord.z * uvLocal.x);

					var posY = Ctx.HeightInput.GetHeight(bladePosition.x, bladePosition.y);
					//var posY = MultiSampleHeight(bladePosition);
					var up = Ctx.NormalInput.GetNormal(bladePosition.x, bladePosition.y);
					//var up = MultiSampleNormal(bladePosition);

					textureData[i] = new Color(up.x, up.y, up.z, posY);
					i++;
				}
			}
			*/
			_normalHeightTexture.SetPixels(textureData);
			_normalHeightTexture.Apply();

			_simulationTexture = new RenderTexture(Ctx.Settings.GetPerPatchTextureWidthHeight(),
				Ctx.Settings.GetPerPatchTextureWidthHeight(), 0,
				RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
			{
				//TODO: Remove if mipmaps not used or use mipmaps
				filterMode = Ctx.Settings.GrassDataTrilinearFiltering ? FilterMode.Trilinear : FilterMode.Bilinear,
				autoGenerateMips = Ctx.Settings.GrassDataTrilinearFiltering,
				useMipMap = Ctx.Settings.GrassDataTrilinearFiltering,
				dimension = TextureDimension.Tex2DArray,
				volumeDepth = 2,
				enableRandomWrite = true,
				wrapMode = TextureWrapMode.Clamp
			};
			_simulationTexture.Create();

			SetupSimulation();
		}

		private void CreateDummyMesh()
		{
			var dummyMeshSize = Ctx.Settings.GetMinAmountBladesPerPatch();
			var dummyVertices = new Vector3[dummyMeshSize];
			var indices = new int[dummyMeshSize];

			for (var i = 0; i < dummyMeshSize; i++)
			{
				dummyVertices[i] = Vector3.zero;
				indices[i] = i;
			}

			_dummyMesh = new Mesh {vertices = dummyVertices};
			_dummyMesh.SetIndices(indices, MeshTopology.Points, 0);
			_dummyMesh.RecalculateBounds();
		}

		private void SetupMaterialPropertyBlock()
		{
			//TODO: Add option to update things like matrix not only on startup but also on update
			_materialPropertyBlock.SetFloat("StartIndex", _startIndex);
			_materialPropertyBlock.SetFloat("ParameterOffsetX", _parameterOffsetX);
			_materialPropertyBlock.SetFloat("ParameterOffsetY", _parameterOffsetY);
			_materialPropertyBlock.SetVector("PatchTexCoord", _patchTexCoord);
			_materialPropertyBlock.SetTexture("SimulationTexture", _simulationTexture);
			_materialPropertyBlock.SetTexture("NormalHeightTexture", _normalHeightTexture);
			_materialPropertyBlock.SetMatrix("PatchModelMatrix", _patchModelMatrix);
		}

		private void SetupSimulation()
		{
			Ctx.GrassSimulationComputeShader.SetInt("StartIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetX", _parameterOffsetX);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetY", _parameterOffsetY);
			Ctx.GrassSimulationComputeShader.SetMatrix("PatchModelMatrix", _patchModelMatrix);

			//Set buffers for SimulationSetup Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture", _simulationTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "NormalHeightTexture", _normalHeightTexture);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelSimulationSetup, out threadGroupX,
				out threadGroupY, out threadGroupZ);

			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelSimulationSetup,
				(int) (Ctx.Settings.GrassDataResolution / threadGroupX), (int) (Ctx.Settings.GrassDataResolution / threadGroupY),
				1);
		}

		private void RunSimulationComputeShader()
		{
			//Set per patch data for whole compute shader
			Ctx.GrassSimulationComputeShader.SetInt("StartIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetX", _parameterOffsetX);
			Ctx.GrassSimulationComputeShader.SetVector("PatchTexCoord", _patchTexCoord);
			Ctx.GrassSimulationComputeShader.SetFloat("ParameterOffsetY", _parameterOffsetY);
			Ctx.GrassSimulationComputeShader.SetFloat("GrassDataResolution", Ctx.Settings.GrassDataResolution);
			Ctx.GrassSimulationComputeShader.SetMatrix("PatchModelMatrix", _patchModelMatrix);

			//Set buffers for Physics Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture", _simulationTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "NormalHeightTexture", _normalHeightTexture);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelPhysics, out threadGroupX, out threadGroupY,
				out threadGroupZ);

			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelPhysics, (int) (Ctx.Settings.GrassDataResolution / threadGroupX),
				(int) (Ctx.Settings.GrassDataResolution / threadGroupY), 1);

			/*
			//Set buffers for Culling Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelCulling, "SimulationTexture", _simulationTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelCulling, "NormalHeightTexture", _normalHeightTexture);

			//Perform Culling
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelCulling, (int) (Ctx.Settings.GrassDataResolution / threadGroupX), (int) (Ctx.Settings.GrassDataResolution / threadGroupY), 1);
			*/
		}

		public override void OnGUI() {}

#if UNITY_EDITOR
		public override void DrawGizmo()
		{
			if (Ctx.EditorSettings.EnablePatchGizmo)
			{
				Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
				Gizmos.DrawWireSphere(Bounds.center, 0.5f);
				Gizmos.DrawWireCube(Bounds.center, Bounds.size);
				Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
				Gizmos.DrawWireCube(_inputBounds.center, _inputBounds.size);
			}
			if (Ctx.EditorSettings.EnableBladeUpGizmo || Ctx.EditorSettings.EnableFullBladeGizmo)
			{
				Gizmos.color = new Color(0f, 1f, 0f, 0.8f);

				for (var i = 0; i < _argsGeometry[0] * _argsGeometry[1]; i++)
				{
					var uvLocal = Ctx.GrassInstance.UvData[_startIndex + i].Position;
					var uvGlobal = new Vector2(_parameterOffsetX, _parameterOffsetY) + uvLocal;
					var normalHeight = _normalHeightTexture.GetPixelBilinear(uvLocal.x, uvLocal.y);
					var pos = new Vector3(uvLocal.x, normalHeight.a, uvLocal.y);
					var bladeUp = new Vector3(normalHeight.r, normalHeight.g, normalHeight.b).normalized;
					pos = _patchModelMatrix.MultiplyPoint3x4(pos);
					var parameters = Ctx.GrassInstance.ParameterTexture.GetPixelBilinear(uvGlobal.x, uvGlobal.y);

					if (Ctx.EditorSettings.EnableFullBladeGizmo)
					{
						var sd = Mathf.Sin(parameters.a);
						var cd = Mathf.Cos(parameters.a);
						var tmp = new Vector3(sd, sd + cd, cd).normalized;
						var bladeDir = Vector3.Cross(bladeUp, tmp).normalized;
						var bladeFront = Vector3.Cross(bladeUp, bladeDir).normalized;
						//var camdir = (pos - Ctx.Camera.transform.position).normalized;

						Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeUp);
						Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeDir);
						Gizmos.color = new Color(0f, 0f, 1f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeFront);
						//Gizmos.color = new Color(1f, 0f, 1f, 0.8f);
						//Gizmos.DrawLine(pos, pos + camdir);
					}
					if (Ctx.EditorSettings.EnableBladeUpGizmo)
					{
						Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeUp);
					}
				}
			}
		}
#endif
	}
}