using System.Collections.Generic;
using UnityEngine;

namespace GrassSimulation.Core.Lod
{
	public class GrassDrawGroup : ContextRequirement
	{
		private readonly uint[] _argsData = {0, 0, 0, 0, 0};
		private readonly ComputeBuffer _argumentBuffer;
		private readonly bool _drawBlossoms;
		private readonly List<GrassPatch> _groupedPatches;
		private readonly uint _instanceCount;
		private readonly ComputeBuffer _instanceToPatchIdBuffer;
		private readonly MaterialPropertyBlock _materialPropertyBlock;
		private readonly Mesh _mesh;

		private readonly Material _renderMaterial;
		private Bounds _bounds;
		private uint[] _instanceToPatchIdData;

		public GrassDrawGroup(SimulationContext ctx, Material material, Mesh mesh, int maxGroupElements,
			uint instanceCount, bool drawBlossoms = false) : base(ctx)
		{
			_renderMaterial = material;
			_mesh = mesh;
			_groupedPatches = new List<GrassPatch>();
			_materialPropertyBlock = new MaterialPropertyBlock();
			_argumentBuffer =
				new ComputeBuffer(1, _argsData.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			_argsData[0] = (uint) _mesh.vertexCount;
			_argsData[1] = instanceCount;
			_argumentBuffer.SetData(_argsData);

			_drawBlossoms = drawBlossoms;
			_instanceCount = instanceCount;

			_instanceToPatchIdBuffer = new ComputeBuffer(maxGroupElements, sizeof(int), ComputeBufferType.Append);
			_instanceToPatchIdBuffer.SetCounterValue((uint) maxGroupElements);
			_instanceToPatchIdData = new uint[maxGroupElements];

			_materialPropertyBlock.SetBuffer("InstanceToPatchIdBuffer", _instanceToPatchIdBuffer);
			_materialPropertyBlock.SetFloat("StaticInstanceCount", instanceCount);
			_materialPropertyBlock.SetFloat("DrawGroupElements", 0);
		}

		/* TODO
		 * X Combine Bounds
		 * Functionality to perform to do a single DrawMeshInstancedIndirect call like this:
		 * TODO: Create and maintain InstanceToPatchIdBuffer
		 * Hold shader data 
		 * if (_argsGeometry[1] > 0)
			{
				Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassGeometry, Bounds, _argsGeometryBuffer, 0,
					_materialPropertyBlock);
				if (Ctx.BlossomCount > 0)
					Graphics.DrawMeshInstancedIndirect(_dummyMesh, 0, Ctx.GrassBlossom, Bounds, _argsGeometryBuffer, 0,
						_materialPropertyBlock);
			}
		 */
		public void Draw()
		{
			if (_groupedPatches.Count == 0) return;
			UpdateInstanceToPatchIdBuffer();
			CombineBounds();
			//Update instance count for draw call
			_argsData[1] = (uint) (_instanceCount * _groupedPatches.Count);
			_argumentBuffer.SetData(_argsData);
			
			_materialPropertyBlock.SetFloat("DrawGroupElements", _groupedPatches.Count);

			Graphics.DrawMeshInstancedIndirect(_mesh, 0, _renderMaterial, _bounds, _argumentBuffer, 0,
				_materialPropertyBlock);

			if (_drawBlossoms && Ctx.BlossomCount > 0)
				Graphics.DrawMeshInstancedIndirect(_mesh, 0, Ctx.GrassBlossom, _bounds, _argumentBuffer, 0,
					_materialPropertyBlock);
			//Clear list since work is done
			_groupedPatches.Clear();
		}

		public void RegisterPatch(GrassPatch grassPatch) { _groupedPatches.Add(grassPatch); }

		public void Unload()
		{
			_instanceToPatchIdBuffer.Release();
			_argumentBuffer.Release();
		}

		private void CombineBounds()
		{
			//We need this bounds so that unity knows when to cull the grass and when not.
			_bounds = new Bounds();
			foreach (var grassPatch in _groupedPatches) _bounds.Encapsulate(grassPatch.Bounds);
		}

		private void UpdateInstanceToPatchIdBuffer()
		{
			//Fill array with grasspatch indices
			int i = 0;
			foreach (var grassPatch in _groupedPatches)
			{
				_instanceToPatchIdData[i] = grassPatch.TextureIndex;
				i++;
			}
			//Update data
			_instanceToPatchIdBuffer.SetData(_instanceToPatchIdData);
			_materialPropertyBlock.SetBuffer("InstanceToPatchIdBuffer", _instanceToPatchIdBuffer); //TODO: Temporary test
		}
	}
}