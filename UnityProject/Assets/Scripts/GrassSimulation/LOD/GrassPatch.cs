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
	public class GrassPatch : Patch, IDestroyable
	{
		private readonly uint[] _args = {0, 0, 0, 0, 0};

		private readonly MaterialPropertyBlock _materialPropertyBlock;

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
		private readonly Matrix4x4 _patchModelMatrix;

		//TODO: Remove this?
		private readonly float[] _patchModelMatrixTransposeInverse;

		private readonly Vector4 _patchTexCoord; //x: xStart, y: yStart, z: width, w:height
		private readonly int _startIndex;
		private ComputeBuffer _argsBuffer;
		private Mesh _dummyMesh;
		//TODO: Maybe we don't need to seperatly store grassData as soon it's in computebuffer
		private Vector4[] _grassDataA; //xyz: upVector, w: pos.y
		private ComputeBuffer _grassDataABuffer;
		private Vector4[] _grassDataB; //xyz: v1, w: height
		private ComputeBuffer _grassDataBBuffer;
		private Vector4[] _grassDataC; //xyz: v2, w: dirAlpha
		private ComputeBuffer _grassDataCBuffer;
		private ComputeBuffer _tessBuffer;
		private Vector4[] _tessData; //x: tessLevel
		//TODO: Remove
		private ComputeShader _visibilityShader;

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

			
			//TODO: Do we need this?
			var transInv = _patchModelMatrix.transpose.inverse;
			_patchModelMatrixTransposeInverse = new[]
			{
				transInv.m00, transInv.m01, transInv.m02,
				transInv.m10, transInv.m11, transInv.m12,
				transInv.m20, transInv.m21, transInv.m22
			};
			/*_patchModelMatrixTransposeInverse = new[]
			{
				transInv.m00, transInv.m10, transInv.m20, 
				transInv.m01, transInv.m11, transInv.m21, 
				transInv.m02, transInv.m12, transInv.m22
			};*/


			CreatePerBladeData();
			CreateDummyMesh();
			SetupComputeBuffers();
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

		private void CreatePerBladeData()
		{
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

			_dummyMesh = new Mesh();
			_dummyMesh.vertices = dummyVertices;
			_dummyMesh.SetIndices(indices, MeshTopology.Points, 0);
			_dummyMesh.RecalculateBounds();
		}

		private void SetupComputeBuffers()
		{
			_grassDataABuffer = new ComputeBuffer(_grassDataA.Length, 16, ComputeBufferType.Default);
			_grassDataBBuffer = new ComputeBuffer(_grassDataB.Length, 16, ComputeBufferType.Default);
			_grassDataCBuffer = new ComputeBuffer(_grassDataC.Length, 16, ComputeBufferType.Default);
			_grassDataABuffer.SetData(_grassDataA);
			_grassDataBBuffer.SetData(_grassDataB);
			_grassDataCBuffer.SetData(_grassDataC);
			_tessBuffer = new ComputeBuffer(_tessData.Length, 16, ComputeBufferType.Default);
			_tessBuffer.SetData(_tessData);
			_argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_args[0] = Context.Settings.GetDummyMeshSize();
			_args[1] = (uint) Context.Settings.GrassDensity;
			_argsBuffer.SetData(_args);
		}

		private void SetupMaterialPropertyBlock()
		{
			//TODO: Add option to update things like matrix not only on startup but also on update
			_materialPropertyBlock.SetFloat("startIndex", _startIndex);
			//TODO: Bind SharedGrassDataBuffer only once per material since its shared and readonly
			_materialPropertyBlock.SetBuffer("SharedGrassDataBuffer", Context.SharedGrassData.SharedGrassBuffer);
			_materialPropertyBlock.SetBuffer("grassDataABuffer", _grassDataABuffer);
			_materialPropertyBlock.SetBuffer("grassDataBBuffer", _grassDataBBuffer);
			_materialPropertyBlock.SetBuffer("grassDataCBuffer", _grassDataCBuffer);
			_materialPropertyBlock.SetBuffer("tessDataBuffer", _tessBuffer);
			_materialPropertyBlock.SetMatrix("patchModelMatrix", _patchModelMatrix);
		}

		private void UpdateForces()
		{
			//TODO: Clean this up and match things with visibility shader
			Context.ForcesComputeShader.SetInt("startIndex", _startIndex);
			//Context.VisibilityComputeShader.SetMatrix("patchModelMatrix", _patchModelMatrix);
			/*Context.VisibilityComputeShader.SetMatrix("viewProjMatrix",
				Context.Camera.projectionMatrix * Context.Camera.worldToCameraMatrix);*/
			Context.ForcesComputeShader.SetBuffer(Context.VisibilityComputeShaderKernel, "SharedGrassData",
				Context.SharedGrassData.SharedGrassBuffer);
			Context.ForcesComputeShader.SetBuffer(Context.ForcesComputeShaderKernel, "grassDataA", _grassDataABuffer);
			Context.ForcesComputeShader.SetBuffer(Context.ForcesComputeShaderKernel, "grassDataB", _grassDataBBuffer);
			Context.ForcesComputeShader.SetBuffer(Context.ForcesComputeShaderKernel, "grassDataC", _grassDataCBuffer);

			Context.ForcesComputeShader.Dispatch(Context.ForcesComputeShaderKernel, (int) Context.Settings.GrassDensity, 1, 1);
		}

		private void UpdateVisibility()
		{
			//TODO: Split PerPatch and PerFrame Stuff, maybe use different kernels instead of different Shaders to only bind constants once
			//TODO: Bind SharedGrassDataBuffer only once since its readonly
			Context.VisibilityComputeShader.SetInt("startIndex", _startIndex);
			Context.VisibilityComputeShader.SetMatrix("patchModelMatrix", _patchModelMatrix);
			Context.VisibilityComputeShader.SetMatrix("patchModelMatrixInverse", _patchModelMatrix.transpose.inverse);
			//Context.VisibilityComputeShader.SetFloats("patchModelMatrixInverse", _patchModelMatrixTransposeInverse);
			/*Context.VisibilityComputeShader.SetMatrix("viewProjMatrix",
				Context.Camera.projectionMatrix * Context.Camera.worldToCameraMatrix);*/
			Context.VisibilityComputeShader.SetFloats("camPos", Context.Camera.transform.position.x,
				Context.Camera.transform.position.y, Context.Camera.transform.position.z);
			Context.VisibilityComputeShader.SetBuffer(Context.VisibilityComputeShaderKernel, "SharedGrassDataBuffer",
				Context.SharedGrassData.SharedGrassBuffer);
			Context.VisibilityComputeShader.SetBuffer(Context.VisibilityComputeShaderKernel, "grassDataABuffer",
				_grassDataABuffer);
			Context.VisibilityComputeShader.SetBuffer(Context.VisibilityComputeShaderKernel, "grassDataBBuffer",
				_grassDataBBuffer);
			Context.VisibilityComputeShader.SetBuffer(Context.VisibilityComputeShaderKernel, "grassDataCBuffer",
				_grassDataCBuffer);
			Context.VisibilityComputeShader.SetBuffer(Context.VisibilityComputeShaderKernel, "tessDataBuffer", _tessBuffer);

			Context.VisibilityComputeShader.Dispatch(Context.VisibilityComputeShaderKernel, (int) Context.Settings.GrassDensity,
				1, 1);
		}

		public void Draw()
		{
			//TODO: Add CPU LOD algorithm
			//TODO: CleanUp ComputeShader Update methods
			//TODO: Actually use _argsBuffer in computeShader or if CPU only, don't use Indirect Draw Methd
			//TODO: Add settings for options in computeShader
			//UpdateForces();
			UpdateVisibility();
			//SetupMaterialPropertyBlock();

			Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Context.GrassSimulationMaterial, Bounds, _argsBuffer, 0,
				_materialPropertyBlock);
		}

		public override void DrawGizmo()
		{
			if (Context.EditorSettings.DrawGrassPatchGizmo)
			{
				Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
				Gizmos.DrawWireSphere(Bounds.center, 0.5f);
				Gizmos.DrawWireCube(Bounds.center, Bounds.size);
			}
			if (Context.EditorSettings.DrawGrassDataGizmo)
			{
				Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
				for (var i = 0; i < Context.Settings.GetAmountBlades(); i++)
				{
					var pos = new Vector3(Context.SharedGrassData.GrassData[_startIndex + i].x,
						_grassDataA[i].w, Context.SharedGrassData.GrassData[_startIndex + i].y);

					var bladeUp = new Vector3(_grassDataB[i].x, _grassDataB[i].y, _grassDataB[i].z).normalized;

					//bladeUp = _patchModelMatrix.transpose.inverse.MultiplyPoint3x4(bladeUp).normalized;

					/*bladeUp = normalize(mul(patchModelMatrixInverse, float4(bladeUp, 1))).xyz;
					bladeDir = normalize(mul(patchModelMatrixInverse, float4(bladeDir, 1))).xyz;
					bladeFront = normalize(mul(patchModelMatrixInverse, float4(bladeFront, 1))).xyz;*/

					pos = _patchModelMatrix.MultiplyPoint3x4(pos);

					//TODO: Add setting to toggle the used drawmode
					if (i == 0)
					{
						var sd = Mathf.Sin(_grassDataC[i].w);
						var cd = Mathf.Cos(_grassDataC[i].w);
						var tmp = new Vector3(sd, sd + cd, cd).normalized;
						var bladeDir = Vector3.Cross(bladeUp, tmp).normalized;
						var bladeFront = Vector3.Cross(bladeUp, bladeDir).normalized;

						/*bladeUp = _patchModelMatrix.transpose.inverse.MultiplyPoint3x4(bladeUp).normalized;
						bladeDir = _patchModelMatrix.transpose.inverse.MultiplyPoint3x4(bladeDir).normalized;
						bladeFront = _patchModelMatrix.transpose.inverse.MultiplyPoint3x4(bladeFront).normalized;*/
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
					else
					{
						Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
						//Gizmos.DrawLine(pos, pos + bladeUp);
					}
				}
			}
		}
	}
}