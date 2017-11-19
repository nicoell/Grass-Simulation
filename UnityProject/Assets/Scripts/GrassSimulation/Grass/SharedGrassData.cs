using UnityEngine;

namespace GrassSimulation.Grass
{
	public class SharedGrassData : RequiredContext, IInitializable, IDestroyable
	{
		private ComputeBuffer SharedGrassBuffer;

		public SharedGrassData(SimulationContext ctx) : base(ctx)
		{
		}

		public Vector4[] GrassData { get; private set; } //pos.xy, width, bend

		public void Destroy()
		{
			SharedGrassBuffer.Release();
		}

		public bool Init()
		{
			var amountBlades = Ctx.Settings.GetAmountInstancedBlades();

			GrassData = new Vector4[amountBlades];

			for (var i = 0; i < amountBlades; i++)
			{
				var randPos = new Vector2((float) Ctx.Random.NextDouble(), (float) Ctx.Random.NextDouble());
				var width = (float) (Ctx.Settings.BladeMinWidth +
				                     Ctx.Random.NextDouble() *
				                     (Ctx.Settings.BladeMaxWidth - Ctx.Settings.BladeMinWidth));
				var bend = (float) (Ctx.Settings.BladeMinBend +
				                    Ctx.Random.NextDouble() * (Ctx.Settings.BladeMaxBend - Ctx.Settings.BladeMinBend));

				GrassData[i].x = randPos.x;
				GrassData[i].y = randPos.y;
				GrassData[i].z = width;
				GrassData[i].w = bend;
			}

			SharedGrassBuffer = new ComputeBuffer(GrassData.Length, 16, ComputeBufferType.Default);
			SharedGrassBuffer.SetData(GrassData);

			Ctx.GrassMaterial.SetBuffer("SharedGrassDataBuffer", SharedGrassBuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelPhysics, "SharedGrassDataBuffer", SharedGrassBuffer);
			Ctx.GrassSimulationComputeShader.SetBuffer(Ctx.KernelCulling, "SharedGrassDataBuffer", SharedGrassBuffer);
			
			return true;
		}
	}
}