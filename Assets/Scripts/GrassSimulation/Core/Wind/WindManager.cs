using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GrassSimulation.Core.Wind {
	public class WindManager : ContextRequirement
	{
		private WindLayerData[] _windLayerData;
		private readonly List<WindLayer> _windLayers;
		public ComputeBuffer WindLayerBuffer;

		public WindManager(SimulationContext ctx) : base(ctx)
		{
			//Create and fill UvData
			InitBuffer();

			_windLayers = new List<WindLayer>();
		}

		public void InitBuffer()
		{
			var bufferLength = Ctx.Settings.WindLayerCount;
			_windLayerData = new WindLayerData[bufferLength];

			WindLayerBuffer = new ComputeBuffer(bufferLength, 2 * 4 * sizeof(float) + sizeof(int),
				ComputeBufferType.Default);
			WindLayerBuffer.SetData(_windLayerData);
		}

		public void Update()
		{
			int i = 0;
			
			foreach (var windLayer in _windLayers)
			{
				_windLayerData[i] = windLayer.GetWindData();
				i++;
				if (i >= _windLayerData.Length) break;
			}

			for (int c = i; c < _windLayerData.Length; c++)
			{
				_windLayerData[c].WindType = -1;
			}
			
			WindLayerBuffer.SetData(_windLayerData);
			
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysics, "WindLayerBuffer", WindLayerBuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysicsBillboardGeneration, "WindLayerBuffer", WindLayerBuffer);
			//Ctx.GrassSimulationComputeShader.SetVector("WindDataTest", _windLayerData[0].WindData);
		}

		public void RegisterLayer(WindLayer windLayer)
		{
			if (_windLayers.All(item => item.GetInstanceID() != windLayer.GetInstanceID())) _windLayers.Add(windLayer);
			//_windLayers.Add(windLayer);
		}

		public void CleanLayers() { _windLayers.RemoveAll(layer => layer == null || !layer.IsActive); }

		public void Unload()
		{
			WindLayerBuffer.Release();
			//_windLayers.ForEach(Object.DestroyImmediate);
		}
	}
}