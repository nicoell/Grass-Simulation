using UnityEngine;

namespace GrassSimulation.Grass
{
	public class SharedGrassData : RequiredContext, IInitializable, IDestroyable
	{
		private ComputeBuffer SharedGrassBuffer;

		public SharedGrassData(SimulationContext context) : base(context)
		{
		}

#if UNITY_EDITOR
		public Vector4[] GrassData { get; private set; } //pos.xy, width, bend
#endif

		public void Destroy()
		{
			SharedGrassBuffer.Release();
		}

		public bool Init()
		{
			var amountBlades = Context.Settings.GetAmountPrecomputedBlades();

#if !UNITY_EDITOR
			Vector4[] GrassData;
#endif
			GrassData = new Vector4[amountBlades];

			for (var i = 0; i < amountBlades; i++)
			{
				var randPos = new Vector2((float) Context.Random.NextDouble(), (float) Context.Random.NextDouble());
				var width = (float) (Context.Settings.BladeMinWidth +
				                     Context.Random.NextDouble() *
				                     (Context.Settings.BladeMaxWidth - Context.Settings.BladeMinWidth));
				var bend = (float) (Context.Settings.BladeMinBend +
				                    Context.Random.NextDouble() * (Context.Settings.BladeMaxBend - Context.Settings.BladeMinBend));

				GrassData[i].x = randPos.x;
				GrassData[i].y = randPos.y;
				GrassData[i].z = width;
				GrassData[i].w = bend;
			}

			SharedGrassBuffer = new ComputeBuffer(GrassData.Length, 16, ComputeBufferType.Default);
			SharedGrassBuffer.SetData(GrassData);

			Context.GrassMaterial.SetBuffer("SharedGrassDataBuffer", SharedGrassBuffer);
			Context.GrassSimulationComputeShader.SetBuffer(Context.KernelPhysics, "SharedGrassDataBuffer", SharedGrassBuffer);
			Context.GrassSimulationComputeShader.SetBuffer(Context.KernelCulling, "SharedGrassDataBuffer", SharedGrassBuffer);
			
			return true;
		}
	}
}