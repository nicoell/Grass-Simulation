using System.Collections.Generic;
using InfiniteMeadow.Utils;
using UnityEngine;

namespace InfiniteMeadow {
	
	public partial class InfiniteMeadowManager
	{
		private class CoreManager
		{
			private List<InfiniteMeadowInstance> _meadowInstances;
			private GlobalShaderLinker _globalShaderTarget;
			private MaterialLinker _materialLinker;
			private MaterialPropertyBlockLinker _propertyBlockLinker;
			private ComputeShaderLinker _computeShaderLinker;

			public float Test { get { return Random.Range(0f, 1f); } }

			public CoreManager()
			{
				_meadowInstances = new List<InfiniteMeadowInstance>();
				_globalShaderTarget = new GlobalShaderLinker();
			}

			public void Init()
			{
				_globalShaderTarget.LinkFloat(UpdateRate.PerFrame, "globaltest", () => Test);
				_globalShaderTarget.UpdateLinks(UpdateRate.Once);
			}

			public void Update()
			{
				_globalShaderTarget.UpdateLinks(UpdateRate.PerFrame);
			} 
				
				
			public void Reset()
			{
				_meadowInstances.Clear();
			}

		}
	}
}