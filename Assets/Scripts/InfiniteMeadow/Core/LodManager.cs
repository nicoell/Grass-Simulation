using UnityEngine;

namespace InfiniteMeadow.Core {
	internal class LodManager : Manager<LodManager>
	{
		
		public LodManager() { Instance = this; }

		public Bounds GetBounds()
		{
			return new Bounds();
		}
	}
}