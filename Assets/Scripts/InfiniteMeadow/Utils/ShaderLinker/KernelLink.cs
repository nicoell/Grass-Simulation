using System;
using UnityEngine;

namespace InfiniteMeadow.Utils {
	public class KernelLink<T> : IShaderLink
	{
		private readonly Func<T> _getValue;
		private readonly Action<int, int, T> _link;
		private readonly int _propertyNameId;
		private readonly int _kernel;

		public KernelLink(string name, Func<T> getter, Action<int, int, T> link, int kernel) : this(Shader.PropertyToID(name),
			getter, link, kernel) { }

		public KernelLink(int propertyNameId, Func<T> getter, Action<int, int, T> link, int kernel) : this(propertyNameId,
			getter, kernel)
		{
			_link = link;
		}

		protected KernelLink(int propertyNameId, Func<T> getter, int kernel)
		{
			_kernel = kernel;
			_propertyNameId = propertyNameId;
			_getValue = getter;
		}

		public void Link() { _link(_kernel, _propertyNameId, _getValue()); }
	}
}