using UnityEngine;
using Random = System.Random;

namespace GrassSimulation.Grass
{
	public class SharedGrassData : RequiredContext, IInitializable
	{
		public SharedGrassData(SimulationContext context) : base(context)
		{
		}

		public Vector4[] GrassData { get; private set; } //pos.xy, width, bend

		public bool Init()
		{
			var amountBlades = Context.Settings.GetAmountPrecomputedBlades();
			var patchSize = Context.Settings.PatchSize;
			var random = new Random(Context.Settings.RandomSeed);
			var seed = Context.Settings.RandomSeed;

			GrassData = new Vector4[amountBlades];

			for (var i = 0; i < amountBlades; i++)
			{
				var randPos = new Vector2((float) random.NextDouble(), (float) random.NextDouble());
				//randPos *= patchSize; //TODO: Maybe don't do this here.
				var width = (float) (Context.Settings.BladeMinWidth +
				                     random.NextDouble() * (Context.Settings.BladeMaxWidth - Context.Settings.BladeMinWidth));
				var bend = (float) (Context.Settings.BladeMinBend +
				                    random.NextDouble() * (Context.Settings.BladeMaxBend - Context.Settings.BladeMinBend));

				GrassData[i].x = randPos.x;
				GrassData[i].y = randPos.y;
				GrassData[i].z = width;
				GrassData[i].w = bend;
			}
			
			return true;
		}
	}
}