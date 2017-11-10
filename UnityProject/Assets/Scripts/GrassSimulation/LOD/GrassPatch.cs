using UnityEngine;

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
		private uint[] _args = {0, 0, 0, 0, 0};
		private readonly ComputeBuffer _argsBuffer;
		private readonly MaterialPropertyBlock _materialPropertyBlock;
		private readonly Vector4 _patchTexCoord; //x: xStart, y: yStart, z: width, w:height
		private readonly int _startIndex;
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

		private ComputeBuffer _tessBuffer;

		public GrassPatch(SimulationContext context, Vector4 patchTexCoord, Bounds bounds) : base(context)
		{
			Bounds = bounds;
			_patchTexCoord = patchTexCoord;
			_startIndex = Context.Random.Next(0,
				(int) (Context.Settings.GetAmountPrecomputedBlades() - Context.Settings.GetAmountBlades() - 1));
			_materialPropertyBlock = new MaterialPropertyBlock();

			_patchModelMatrix = Matrix4x4.TRS(
				new Vector3(bounds.center.x - bounds.extents.x, Context.Transform.position.y, bounds.center.z - bounds.extents.z),
				Quaternion.identity,
				new Vector3(Context.Settings.PatchSize, Context.Terrain.terrainData.size.y, Context.Settings.PatchSize));

			// Create the IndirectArguments Buffer
			_argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_args[0] = Context.Settings.GetDummyMeshSize(); //Vertex Count
			_args[1] = (uint) Context.Settings.GrassDensity; //Instance Count
			_argsBuffer.SetData(_args);

			CreateGrassData();
			CreateDummyMesh();
			SetupMaterialPropertyBlock();
		}

		public override bool IsLeaf
		{
			get { return true; }
		}

		public void Destroy()
		{
			_argsBuffer.Release();
			_grassDataABuffer.Release();
			_grassDataBBuffer.Release();
			_grassDataCBuffer.Release();
			_tessBuffer.Release();
		}

		private void CreateGrassData()
		{
			//Precompute grassData for the all blades (the maximum possible number)
#if !UNITY_EDITOR
			Vector4[] _grassDataA;
			Vector4[] _grassDataB;
			Vector4[] _grassDataC;
			Vector4[] _tessData;
#endif
			_grassDataA = new Vector4[Context.Settings.GetAmountBlades()];
			_grassDataB = new Vector4[Context.Settings.GetAmountBlades()];
			_grassDataC = new Vector4[Context.Settings.GetAmountBlades()];
			_tessData = new Vector4[Context.Settings.GetAmountBlades()];
			for (var i = 0; i < Context.Settings.GetAmountBlades(); i++)
			{
				//Fill _grassDataA
				var bladePosition =
					new Vector2(_patchTexCoord.x + _patchTexCoord.z * Context.SharedGrassData.GrassData[_startIndex + i].x,
						_patchTexCoord.y + _patchTexCoord.w * Context.SharedGrassData.GrassData[_startIndex + i].y);
				var posY = Context.Heightmap.GetPixel((int) (bladePosition.x * Context.Heightmap.width),
					(int) (bladePosition.y * Context.Heightmap.height)).r;
				var up = Context.Terrain.terrainData.GetInterpolatedNormal(bladePosition.x, bladePosition.y);
				_grassDataA[i].Set(up.x, up.y, up.z, posY);
				//Fill _grassDataB
				var height = (float) (Context.Settings.BladeMinHeight +
				                      Context.Random.NextDouble() *
				                      (Context.Settings.BladeMaxHeight - Context.Settings.BladeMinHeight));
				_grassDataB[i].Set(up.x * height / 2, up.y * height / 2, up.z * height / 2, height);
				//Fill _grassDataC
				var dirAlpha = (float) (Context.Random.NextDouble() * Mathf.PI * 2f);
				_grassDataC[i].Set(up.x * height, up.y * height, up.z * height, dirAlpha);

				_tessData[i].Set(8.0f, 1.0f, 1.0f, 1.0f);
			}

			//Create the computeBuffers and fill them with the just created data
			_grassDataABuffer = new ComputeBuffer(_grassDataA.Length, 16, ComputeBufferType.Default);
			_grassDataBBuffer = new ComputeBuffer(_grassDataB.Length, 16, ComputeBufferType.Default);
			_grassDataCBuffer = new ComputeBuffer(_grassDataC.Length, 16, ComputeBufferType.Default);
			_grassDataABuffer.SetData(_grassDataA);
			_grassDataBBuffer.SetData(_grassDataB);
			_grassDataCBuffer.SetData(_grassDataC);
			_tessBuffer = new ComputeBuffer(_tessData.Length, 16, ComputeBufferType.Default);
			_tessBuffer.SetData(_tessData);
		}

		private void CreateDummyMesh()
		{
			var dummyMeshSize = Context.Settings.GetDummyMeshSize();
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
			//Set data for whole compute shader
			Context.GrassSimulationComputeShader.SetInt("startIndex", _startIndex);
			Context.GrassSimulationComputeShader.SetMatrix("patchModelMatrix", _patchModelMatrix);
			Context.GrassSimulationComputeShader.SetMatrix("patchModelMatrixInverse", _patchModelMatrix.transpose.inverse);
			
			//Set buffers for Physics Kernel
			Context.GrassSimulationComputeShader.SetBuffer(Context.KernelPhysics, "grassDataABuffer",
				_grassDataABuffer);
			Context.GrassSimulationComputeShader.SetBuffer(Context.KernelPhysics, "grassDataBBuffer",
				_grassDataBBuffer);
			Context.GrassSimulationComputeShader.SetBuffer(Context.KernelPhysics, "grassDataCBuffer",
				_grassDataCBuffer);
			Context.GrassSimulationComputeShader.SetBuffer(Context.KernelPhysics, "tessDataBuffer", _tessBuffer);
			
			//Run Physics Simulation
			Context.GrassSimulationComputeShader.Dispatch(Context.KernelPhysics, (int) Context.Settings.GrassDensity, 1, 1);
			
			//Set buffers for Culling Kernel
			Context.GrassSimulationComputeShader.SetBuffer(Context.KernelCulling, "grassDataABuffer",
				_grassDataABuffer);
			Context.GrassSimulationComputeShader.SetBuffer(Context.KernelCulling, "grassDataBBuffer",
				_grassDataBBuffer);
			Context.GrassSimulationComputeShader.SetBuffer(Context.KernelCulling, "grassDataCBuffer",
				_grassDataCBuffer);
			Context.GrassSimulationComputeShader.SetBuffer(Context.KernelCulling, "tessDataBuffer", _tessBuffer);
			
			//Perform Culling
			Context.GrassSimulationComputeShader.Dispatch(Context.KernelCulling, (int) Context.Settings.GrassDensity, 1, 1);
		}

		public void Draw()
		{
			//TODO: Add CPU LOD algorithm
			//TODO: Actually use _argsBuffer in computeShader or if CPU only, don't use Indirect Draw Methd
			//TODO: Add settings for options in computeShader
			RunSimulationComputeShader();
			//SetupMaterialPropertyBlock();

			Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Context.GrassMaterial, Bounds, _argsBuffer, 0,
				_materialPropertyBlock);
		}

#if UNITY_EDITOR
		public override void DrawGizmo()
		{
			if (Context.EditorSettings.DrawGrassPatchGizmo)
			{
				Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
				Gizmos.DrawWireSphere(Bounds.center, 0.5f);
				Gizmos.DrawWireCube(Bounds.center, Bounds.size);
			}
			if (Context.EditorSettings.DrawGrassDataGizmo || Context.EditorSettings.DrawGrassDataDetailGizmo)
			{
				Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
				for (var i = 0; i < Context.Settings.GetAmountBlades(); i++)
				{
					var pos = new Vector3(Context.SharedGrassData.GrassData[_startIndex + i].x,
						_grassDataA[i].w, Context.SharedGrassData.GrassData[_startIndex + i].y);
					var bladeUp = new Vector3(_grassDataB[i].x, _grassDataB[i].y, _grassDataB[i].z).normalized;
					pos = _patchModelMatrix.MultiplyPoint3x4(pos);

					if (Context.EditorSettings.DrawGrassDataDetailGizmo)
					{
						var sd = Mathf.Sin(_grassDataC[i].w);
						var cd = Mathf.Cos(_grassDataC[i].w);
						var tmp = new Vector3(sd, sd + cd, cd).normalized;
						var bladeDir = Vector3.Cross(bladeUp, tmp).normalized;
						var bladeFront = Vector3.Cross(bladeUp, bladeDir).normalized;
						var camdir = (pos - Context.Camera.transform.position).normalized;

						Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeUp);
						Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeDir);
						Gizmos.color = new Color(0f, 0f, 1f, 0.8f);
						Gizmos.DrawLine(pos, pos + bladeFront);
						Gizmos.color = new Color(1f, 0f, 1f, 0.8f);
						Gizmos.DrawLine(pos, pos + camdir);
					}
					if (Context.EditorSettings.DrawGrassDataGizmo)
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
		private Vector4[] _tessData; //x: tessLevel
#endif
	}
}