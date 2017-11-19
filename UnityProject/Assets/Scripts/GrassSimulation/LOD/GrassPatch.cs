using UnityEngine;
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
		private bool _applyTransition;
		private Mesh _dummyMesh;
		private ComputeBuffer _grassDataABuffer;
		private ComputeBuffer _grassDataBBuffer;
		private ComputeBuffer _grassDataCBuffer;

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
		private ComputeBuffer _tessBuffer;
		private uint _transitionGroupId;

		public GrassPatch(SimulationContext ctx, Vector4 patchTexCoord, UnityEngine.Bounds bounds) : base(ctx)
		{
			Bounds = bounds;
			_boundsVertices = new Bounds.BoundsVertices(bounds);
			_patchTexCoord = patchTexCoord;
			_startIndex = Ctx.Random.Next(0,
				(int) (Ctx.Settings.GetAmountInstancedBlades() - Ctx.Settings.GetMaxAmountBladesPerPatch()));
			_materialPropertyBlock = new MaterialPropertyBlock();

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
			CreateGrassData();
			CreateDummyMesh();
			SetupMaterialPropertyBlock();
		}

		public override bool IsLeaf { get { return true; } }

		public void Destroy()
		{
			_argsGeometryGrassBuffer.Release();
			_grassDataABuffer.Release();
			_grassDataBBuffer.Release();
			_grassDataCBuffer.Release();
			_tessBuffer.Release();
		}

		public void Draw()
		{
			//TODO: Add CPU LOD algorithm
			//TODO: Actually use _argsGeometryGrassBuffer in computeShader or if CPU only, don't use Indirect Draw Methd
			//TODO: Add settings for options in computeShader
			ComputeLod();
			RunSimulationComputeShader();

			//SetupMaterialPropertyBlock();

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
				var posY = Ctx.Heightmap.GetPixel((int) (bladePosition.x * Ctx.Heightmap.width),
					(int) (bladePosition.y * Ctx.Heightmap.height)).r;
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
			_materialPropertyBlock.SetFloat("startIndex", _startIndex);
			_materialPropertyBlock.SetBuffer("grassDataABuffer", _grassDataABuffer);
			_materialPropertyBlock.SetBuffer("grassDataBBuffer", _grassDataBBuffer);
			_materialPropertyBlock.SetBuffer("grassDataCBuffer", _grassDataCBuffer);
			_materialPropertyBlock.SetBuffer("tessDataBuffer", _tessBuffer);
			_materialPropertyBlock.SetMatrix("patchModelMatrix", _patchModelMatrix);
		}

		private void RunSimulationComputeShader()
		{
			//Set per patch data for whole compute shader
			Ctx.GrassSimulationComputeShader.SetBool("applyTransition", _applyTransition);
			Ctx.GrassSimulationComputeShader.SetInt("startIndex", _startIndex);
			Ctx.GrassSimulationComputeShader.SetMatrix("patchModelMatrix", _patchModelMatrix);
			Ctx.GrassSimulationComputeShader.SetMatrix("patchModelMatrixInverse", _patchModelMatrix.transpose.inverse);

			//Set buffers for Physics Kernel
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysics, "grassDataABuffer", _grassDataABuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysics, "grassDataBBuffer", _grassDataBBuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysics, "grassDataCBuffer", _grassDataCBuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysics, "pressureDataBuffer", _pressureDataBuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysics, "tessDataBuffer", _tessBuffer);

			//Run Physics Simulation
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelPhysics, (int) Ctx.Settings.LodDensityFullDetailDistance, 1, 1);

			//Set buffers for Culling Kernel
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelCulling, "grassDataABuffer", _grassDataABuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelCulling, "grassDataBBuffer", _grassDataBBuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelCulling, "grassDataCBuffer", _grassDataCBuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelCulling, "tessDataBuffer", _tessBuffer);

			//Perform Culling
			//TODO: threadgroupsX correct?
			Ctx.GrassSimulationComputeShader.Dispatch(Ctx.KernelCulling, (int) _argsGeometryGrass[1], 1, 1);
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
				for (var i = 0; i < Ctx.Settings.GetMaxAmountBladesPerPatch(); i++)
				{
					var pos = new Vector3(Ctx.SharedGrassData.GrassData[_startIndex + i].x,
						_grassDataA[i].w, Ctx.SharedGrassData.GrassData[_startIndex + i].y);
					var bladeUp = new Vector3(_grassDataB[i].x, _grassDataB[i].y, _grassDataB[i].z).normalized;
					pos = _patchModelMatrix.MultiplyPoint3x4(pos);

					if (Ctx.EditorSettings.DrawGrassDataDetailGizmo)
					{
						var sd = Mathf.Sin(_grassDataC[i].w);
						var cd = Mathf.Cos(_grassDataC[i].w);
						var tmp = new Vector3(sd, sd + cd, cd).normalized;
						var bladeDir = Vector3.Cross(bladeUp, tmp).normalized;
						var bladeFront = Vector3.Cross(bladeUp, bladeDir).normalized;
						var camdir = (pos - Ctx.Camera.transform.position).normalized;

						Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeUp);
						Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeDir);
						Gizmos.color = new Color(0f, 0f, 1f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeFront);
						Gizmos.color = new Color(1f, 0f, 1f, 0.8f);
						Gizmos.DrawLine(pos, pos + camdir);
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
		private Vector4[] _tessData; //x: tessLevel
#endif
	}
}