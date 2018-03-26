using System;
using UnityEngine;

namespace InfiniteMeadow {
	
	public class InfiniteMeadowSettings : ScriptableObject
	{
		[NonSerialized]
		public bool InitRequired = true;
		
		public float TestProp { get; set; }
	}
}