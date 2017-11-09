using UnityEngine;
using Random = System.Random;

namespace GrassSimulation.Grass
{
	public class SharedGrassData : RequiredContext, IInitializable, IDestroyable
	{
		public ComputeBuffer SharedGrassBuffer;
		
		public SharedGrassData(SimulationContext context) : base(context)
		{
		}

		public Vector4[] GrassData { get; private set; } //pos.xy, width, bend

		public unsafe bool Init()
		{
			var amountBlades = Context.Settings.GetAmountPrecomputedBlades();
			var patchSize = Context.Settings.PatchSize;
			var seed = Context.Settings.RandomSeed;

			GrassData = new Vector4[amountBlades];

			for (var i = 0; i < amountBlades; i++)
			{
				var randPos = new Vector2((float) Context.Random.NextDouble(), (float) Context.Random.NextDouble());
				//randPos *= patchSize; //TODO: Maybe don't do this here.
				var width = (float) (Context.Settings.BladeMinWidth +
				                     Context.Random.NextDouble() * (Context.Settings.BladeMaxWidth - Context.Settings.BladeMinWidth));
				var bend = (float) (Context.Settings.BladeMinBend +
				                    Context.Random.NextDouble() * (Context.Settings.BladeMaxBend - Context.Settings.BladeMinBend));

				GrassData[i].x = randPos.x;
				GrassData[i].y = randPos.y;
				GrassData[i].z = width;
				GrassData[i].w = bend;
			}
			
			SharedGrassBuffer = new ComputeBuffer(GrassData.Length, 16, ComputeBufferType.Default);
			SharedGrassBuffer.SetData(GrassData);
			
			Context.ForcesComputeShader.SetBuffer(Context.ForcesComputeShaderKernel, "SharedGrassData", SharedGrassBuffer);
			Context.VisibilityComputeShader.SetBuffer(Context.VisibilityComputeShaderKernel, "SharedGrassData", SharedGrassBuffer);
			return true;
		}

		public void Destroy()
		{
			SharedGrassBuffer.Release();
		}
	}
}