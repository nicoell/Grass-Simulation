using System;
using UnityEngine;

namespace InfiniteMeadow.Core.Utils.GPULinker
{
	public class CommonLink<T> : IGPULink
	{
		private readonly Func<T> _getValue;
		private readonly Action<int, T> _link;
		private readonly int _propertyNameId;

		public CommonLink(string name, Func<T> getter, Action<int, T> link) :
			this(Shader.PropertyToID(name), getter, link) { }

		public CommonLink(int propertyNameId, Func<T> getter, Action<int, T> link) : this(propertyNameId, getter)
		{
			_link = link;
		}

		protected CommonLink(int propertyNameId, Func<T> getter)
		{
			_propertyNameId = propertyNameId;
			_getValue = getter;
		}

		public void Link() { _link(_propertyNameId, _getValue()); }
	}
}