using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Timeline;
using Bounds = GrassSimulation.Utils.Bounds;

namespace GrassSimulation.LOD
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
	public class GrassPatch : Patch, IDestroyable, IDrawable
	{
		private readonly uint[] _argsBillboardGrass = {0, 0, 0, 0, 0};
		private readonly ComputeBuffer _argsBillboardGrassBuffer;
		private readonly uint[] _argsGeometryGrass = {0, 0, 0, 0, 0};
		private readonly ComputeBuffer _argsGeometryGrassBuffer;
		private readonly Bounds.BoundsVertices _boundsVertices;
		private readonly MaterialPropertyBlock _materialPropertyBlock;
		private readonly Vector4 _patchTexCoord; //x: xStart, y: yStart, z: width, w:height
		private readonly int _startIndex;
		private readonly float _parameterOffsetX;
		private readonly float _parameterOffsetY;
		private bool _applyTransition;
		private Mesh _dummyMesh;

		public Texture2D _normalHeightTexture;
		public RenderTexture _simulationTexture;

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
		private ComputeBuffer _pressureDataBuffer;
		private uint _transitionGroupId;

		public GrassPatch(SimulationContext ctx, Vector4 patchTexCoord, UnityEngine.Bounds bounds) : base(ctx)
		{
			Bounds = bounds;
			_boundsVertices = new Bounds.BoundsVertices(bounds);
			_patchTexCoord = patchTexCoord;
			_startIndex = Ctx.Random.Next(0,
				(int) (Ctx.Settings.GetSharedBufferLength() - Ctx.Settings.GetMaxAmountBladesPerPatch()));
			_materialPropertyBlock = new MaterialPropertyBlock();
			_parameterOffsetX = (float) Ctx.Random.NextDouble();// * Ctx.Settings.InstancedGrassFactor);
			_parameterOffsetY = (float) Ctx.Random.NextDouble();// * Ctx.Settings.InstancedGrassFactor);

			_patchModelMatrix = Matrix4x4.TRS(
				new Vector3(bounds.center.x - bounds.extents.x, Ctx.Transform.position.y, bounds.center.z - bounds.extents.z),
				Quaternion.identity,
				new Vector3(Ctx.Settings.PatchSize, Ctx.Terrain.terrainData.size.y, Ctx.Settings.PatchSize));

			// Create the IndirectArguments Buffer
			_argsGeometryGrassBuffer =
				new ComputeBuffer(1, _argsGeometryGrass.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsGeometryGrass[0] = Ctx.Settings.GetMinAmountBladesPerPatch(); //Vertex Count
			_argsGeometryGrass[1] = Ctx.Settings.LodDensityFullDetailDistance; //Instance Count
			_argsGeometryGrassBuffer.SetData(_argsGeometryGrass);

			_argsBillboardGrassBuffer =
				new ComputeBuffer(1, _argsBillboardGrass.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsBillboardGrass[0] = Ctx.Settings.GetMinAmountBladesPerPatch(); //Vertex Count
			_argsBillboardGrass[1] = 1; //Instance Count
			_argsBillboardGrassBuffer.SetData(_argsBillboardGrass);
			CreateGrassDataTexture();
			CreateDummyMesh();
			SetupMaterialPropertyBlock();
		}

		public override bool IsLeaf { get { return true; } }

		public void Destroy()
		{
			//TODO: Clean up buffers and textures
			_argsGeometryGrassBuffer.Release();
		}

		public void Draw()
		{
			//TODO: Add CPU LOD algorithm
			//TODO: Actually use _argsGeometryGrassBuffer in computeShader or if CPU only, don't use Indirect Draw Methd
			//TODO: Add settings for options in computeShader
			ComputeLod();
			RunSimulationComputeShader();

			//SetupMaterialPropertyBlock();
			//Ctx.GrassMaterial.SetInt("startIndex", _startIndex);
			Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassMaterial, Bounds, _argsGeometryGrassBuffer, 0,
				_materialPropertyBlock);
		}

		private void ComputeLod()
		{
			if (!Utils.Bounds.BoundsVertices.IntersectsSphere(_boundsVertices, Ctx.Camera.transform.position,
				Ctx.Settings.LodDistanceMax)) //Bounds outside LodDistanceMax -> Render nothing
			{
				_argsBillboardGrass[1] = 0;
				_argsGeometryGrass[1] = 0;
				_applyTransition = false;
			} else if (!Utils.Bounds.BoundsVertices.IntersectsSphere(_boundsVertices, Ctx.Camera.transform.position,
				Ctx.Settings.LodDistanceBillboard)) //Bounds outside Billboard threshold -> Render billboards
			{
				//TODO: Change this when implementing billboard grass
				_argsBillboardGrass[1] = 0;
				_argsGeometryGrass[1] = Ctx.Settings.LodDensityBillboardDistance;
				_applyTransition = Ctx.Settings.EnableHeightTransition;
			} else //Bounds inside Billboard threshold -> Render geometry
			{
				var distance = Vector3.Distance(Ctx.Camera.transform.position, Bounds.ClosestPoint(Ctx.Camera.transform.position));
				var t = (distance - Ctx.Settings.LodDistanceFullDetail) /
				        (Ctx.Settings.LodDistanceBillboard - Ctx.Settings.LodDistanceFullDetail);
				//Unitys Lerp is already clamping the t value.
				var instanceCount = (uint) Mathf.Ceil(Mathf.Lerp(Ctx.Settings.LodDensityFullDetailDistance,
					Ctx.Settings.LodDensityBillboardDistance, t));

				//TODO: Fade in Billboards
				_argsBillboardGrass[1] = (uint) (instanceCount <= 2 ? 1 : 0);
				_argsGeometryGrass[1] = (uint) Mathf.Min(instanceCount, Ctx.Settings.LodDensityFullDetailDistance);
				_applyTransition = Ctx.Settings.EnableHeightTransition;
			}

			_argsGeometryGrassBuffer.SetData(_argsGeometryGrass);
			_argsBillboardGrassBuffer.SetData(_argsBillboardGrass);
		}

		private void CreateGrassDataTexture()
		{
			
			_normalHeightTexture = new Texture2D(Ctx.Settings.GetPerPatchTextureWidthHeight(), Ctx.Settings.GetPerPatchTextureWidthHeight(),
				TextureFormat.RGBAFloat, true, true)
			{
				filterMode = Ctx.Settings.GrassDataTrilinearFiltering ? FilterMode.Trilinear : FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			var textureData = new Color[Ctx.Settings.GetPerPatchTextureLength()];
			int i = 0;

			for (int x = 0; x < Ctx.Settings.GetPerPatchTextureWidthHeight(); x++)
			for (int y = 0; y < Ctx.Settings.GetPerPatchTextureWidthHeight(); y++)
			{
				var uvLocal = new Vector2((float)y/Ctx.Settings.GrassDataResolution, (float)x/Ctx.Settings.GrassDataResolution);
				var bladePosition = new Vector2(
					_patchTexCoord.x + _patchTexCoord.z * uvLocal.x,
					_patchTexCoord.y + _patchTexCoord.w * uvLocal.y);
				
				var posY = Ctx.Terrain.terrainData.GetInterpolatedHeight(bladePosition.x, bladePosition.y) /
				           Ctx.Terrain.terrainData.size.y;
				var up = Ctx.Terrain.terrainData.GetInterpolatedNormal(bladePosition.x, bladePosition.y);
				
				textureData[i] = new Color(up.x, up.y, up.z, posY);
				i++;
			}
			
			_normalHeightTexture.SetPixels(textureData);
			_normalHeightTexture.Apply();
			
			_simulationTexture = new RenderTexture(Ctx.Settings.GetPerPatchTextureWidthHeight(), Ctx.Settings.GetPerPatchTextureWidthHeight(), 0,
				RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
			{
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
		
		/*
		private void CreateGrassData()
		{
			//Precompute grassData for the all blades (the maximum possible number)
#if !UNITY_EDITOR
			Vector4[] _grassDataA;
			Vector4[] _grassDataB;
			Vector4[] _grassDataC;
			Vector4[] _pressureData;
			Vector4[] _tessData;
#endif
			_grassDataA = new Vector4[Ctx.Settings.GetMaxAmountBladesPerPatch()];
			_grassDataB = new Vector4[Ctx.Settings.GetMaxAmountBladesPerPatch()];
			_grassDataC = new Vector4[Ctx.Settings.GetMaxAmountBladesPerPatch()];
			_pressureData = new Vector4[Ctx.Settings.GetMaxAmountBladesPerPatch()];
			_tessData = new Vector4[Ctx.Settings.GetMaxAmountBladesPerPatch()];
			for (var i = 0; i < Ctx.Settings.GetMaxAmountBladesPerPatch(); i++)
			{
				//Fill _grassDataA
				var bladePosition =
					new Vector2(_patchTexCoord.x + _patchTexCoord.z * Ctx.SharedGrassData.GrassData[_startIndex + i].x,
						_patchTexCoord.y + _patchTexCoord.w * Ctx.SharedGrassData.GrassData[_startIndex + i].y);
				var posY = Ctx.Terrain.terrainData.GetInterpolatedHeight(bladePosition.x, bladePosition.y) / Ctx.Terrain.terrainData.size.y;
				
				var up = Ctx.Terrain.terrainData.GetInterpolatedNormal(bladePosition.x, bladePosition.y);
				_grassDataA[i].Set(up.x, up.y, up.z, posY);
				//Fill _grassDataB
				var height = (float) (Ctx.Settings.BladeMinHeight +
				                      Ctx.Random.NextDouble() *
				                      (Ctx.Settings.BladeMaxHeight - Ctx.Settings.BladeMinHeight));
				_grassDataB[i].Set(up.x * height, up.y * height, up.z * height, height);
				//Fill _grassDataC
				var dirAlpha = (float) (Ctx.Random.NextDouble() * Mathf.PI * 2f);
				_grassDataC[i].Set(up.x * height, up.y * height, up.z * height, dirAlpha);

				_pressureData[i].Set(0, 0, 0, 0);
				_tessData[i].Set(8.0f, 1.0f, 1.0f, 1.0f);
			}

			//Create the computeBuffers and fill them with the just created data
			_grassDataABuffer = new ComputeBuffer(_grassDataA.Length, 16, ComputeBufferType.Default);
			_grassDataBBuffer = new ComputeBuffer(_grassDataB.Length, 16, ComputeBufferType.Default);
			_grassDataCBuffer = new ComputeBuffer(_grassDataC.Length, 16, ComputeBufferType.Default);
			_pressureDataBuffer = new ComputeBuffer(_pressureData.Length, 16, ComputeBufferType.Default);
			_grassDataABuffer.SetData(_grassDataA);
			_grassDataBBuffer.SetData(_grassDataB);
			_grassDataCBuffer.SetData(_grassDataC);
			_pressureDataBuffer.SetData(_pressureData);
			_tessBuffer = new ComputeBuffer(_tessData.Length, 16, ComputeBufferType.Default);
			_tessBuffer.SetData(_tessData);
		}
*/

		private void CreateDummyMesh()
		{
			//TODO: meshSize and computeshader thread count needs to be connected
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
			_materialPropertyBlock.SetFloat("startIndex", _startIndex);
			_materialPropertyBlock.SetFloat("parameterOffsetX", _parameterOffsetX);
			_materialPropertyBlock.SetFloat("parameterOffsetY", _parameterOffsetY);
			_materialPropertyBlock.SetTexture("SimulationTexture", _simulationTexture);
			_materialPropertyBlock.SetTexture("NormalHeightTexture", _normalHeightTexture);
			_materialPropertyBlock.SetMatrix("patchModelMatrix", _patchModelMatrix);
			_materialPropertyBlock.SetVector("PatchTexCoord", _patchTexCoord);
		}

		private void SetupSimulation()
		{
			Ctx.GrassSimulationComputeShader.SetBool("applyTransition", _applyTransition);
			Ctx.GrassSimulationComputeShader.SetInt("startIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetFloat("parameterOffsetX", _parameterOffsetX);
			Ctx.GrassSimulationComputeShader.SetFloat("parameterOffsetY", _parameterOffsetY);
			Ctx.GrassSimulationComputeShader.SetVector("PatchTexCoord", _patchTexCoord);
			Ctx.GrassSimulationComputeShader.SetFloat("GrassDataResolution", Ctx.Settings.GrassDataResolution);
			Ctx.GrassSimulationComputeShader.SetMatrix("patchModelMatrix", _patchModelMatrix);
			Ctx.GrassSimulationComputeShader.SetMatrix("patchModelMatrixInverse", _patchModelMatrix.transpose.inverse);
			
			//Set buffers for SimulationSetup Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "SimulationTexture", _simulationTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelSimulationSetup, "NormalHeightTexture", _normalHeightTexture);
			
			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelSimulationSetup, out threadGroupX, out threadGroupY, out threadGroupZ);
			
			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelSimulationSetup, (int) (Ctx.Settings.GrassDataResolution / threadGroupX), (int) (Ctx.Settings.GrassDataResolution / threadGroupY), 1);
		}
		
		private void RunSimulationComputeShader()
		{
			//Set per patch data for whole compute shader
			Ctx.GrassSimulationComputeShader.SetBool("applyTransition", _applyTransition);
			Ctx.GrassSimulationComputeShader.SetInt("startIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetFloat("parameterOffsetX", _parameterOffsetX);
			Ctx.GrassSimulationComputeShader.SetFloat("parameterOffsetY", _parameterOffsetY);
			Ctx.GrassSimulationComputeShader.SetVector("PatchTexCoord", _patchTexCoord);
			Ctx.GrassSimulationComputeShader.SetFloat("GrassDataResolution", Ctx.Settings.GrassDataResolution);
			Ctx.GrassSimulationComputeShader.SetMatrix("patchModelMatrix", _patchModelMatrix);
			Ctx.GrassSimulationComputeShader.SetMatrix("patchModelMatrixInverse", _patchModelMatrix.transpose.inverse);

			//Set buffers for Physics Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "SimulationTexture", _simulationTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelPhysics, "NormalHeightTexture", _normalHeightTexture);

			uint threadGroupX, threadGroupY, threadGroupZ;
			Ctx.GrassSimulationComputeShader.GetKernelThreadGroupSizes(Ctx.KernelPhysics, out threadGroupX, out threadGroupY, out threadGroupZ);
			
			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelPhysics, (int) (Ctx.Settings.GrassDataResolution / threadGroupX), (int) (Ctx.Settings.GrassDataResolution / threadGroupY), 1);

			//Set buffers for Culling Kernel
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelCulling, "SimulationTexture", _simulationTexture);
			Ctx.GrassSimulationComputeShader.SetTexture(Ctx.KernelCulling, "NormalHeightTexture", _normalHeightTexture);

			//Perform Culling
			//TODO: threadgroupsX correct?
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelCulling, (int) (Ctx.Settings.GrassDataResolution / threadGroupX), (int) (Ctx.Settings.GrassDataResolution / threadGroupY), 1);
		}

#if UNITY_EDITOR
		public override void DrawGizmo()
		{
			if (Ctx.EditorSettings.DrawGrassPatchGizmo)
			{
				Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
				Gizmos.DrawWireSphere(Bounds.center, 0.5f);
				Gizmos.DrawWireCube(Bounds.center, Bounds.size);
			}
			if (Ctx.EditorSettings.DrawGrassDataGizmo || Ctx.EditorSettings.DrawGrassDataDetailGizmo)
			{
				Gizmos.color = new Color(0f, 1f, 0f, 0.8f);

				for (var i = 0; i < _argsGeometryGrass[0] * _argsGeometryGrass[1]; i++)
				{
					var uvLocal = Ctx.SharedGrassData.UvData[_startIndex + i].Position;
					var uvGlobal = new Vector2(_parameterOffsetX, _parameterOffsetY) + uvLocal;
					var normalHeight = _normalHeightTexture.GetPixelBilinear(uvLocal.x, uvLocal.y);
					var pos = new Vector3(uvLocal.x, normalHeight.a, uvLocal.y);
					var bladeUp = new Vector3(normalHeight.r, normalHeight.g, normalHeight.b).normalized;
					pos = _patchModelMatrix.MultiplyPoint3x4(pos);
					var parameters = Ctx.SharedGrassData.ParameterTexture.GetPixelBilinear(uvGlobal.x, uvGlobal.y);

					if (Ctx.EditorSettings.DrawGrassDataDetailGizmo)
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
					if (Ctx.EditorSettings.DrawGrassDataGizmo)
					{
						Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeUp);
					}
				}
			}
		}

		//We only need this for drawing debug Gizmos in Editor
		private Vector4[] _grassDataA; //xyz: upVector, w: pos.y

		private Vector4[] _grassDataB; //xyz: v1, w: height
		private Vector4[] _grassDataC; //xyz: v2, w: dirAlpha
		private Vector4[] _pressureData;
		private Vector2[] _tessData; //x: tessLevel, y: transitionFactor
#endif
	}
}