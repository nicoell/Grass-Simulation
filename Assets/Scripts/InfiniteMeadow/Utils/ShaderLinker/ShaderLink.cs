using System;
using UnityEngine;

namespace InfiniteMeadow.Utils
{
	public class ShaderLink<T> : IShaderLink
	{
		private readonly Func<T> _getValue;
		private readonly Action<int, T> _link;
		private readonly int _propertyNameId;

		public ShaderLink(string name, Func<T> getter, Action<int, T> link) :
			this(Shader.PropertyToID(name), getter, link) { }

		public ShaderLink(int propertyNameId, Func<T> getter, Action<int, T> link) : this(propertyNameId, getter)
		{
			_link = link;
		}

		protected ShaderLink(int propertyNameId, Func<T> getter)
		{
			_propertyNameId = propertyNameId;
			_getValue = getter;
		}

		public void Link() { _link(_propertyNameId, _getValue()); }
	}
}