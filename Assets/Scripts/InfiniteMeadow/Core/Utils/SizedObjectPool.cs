using System;
using System.Collections.Generic;

namespace InfiniteMeadow.Core.Utils {
	internal class SizedObjectPool<T> where T : class
	{
		private Func<T> _objectGenerator;
		private Stack<T> _objects;

		public SizedObjectPool(int size, Func<T> objectGenerator)
		{
			if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
			_objects = new Stack<T>();
			for (int i = 0; i < size; i++)
			{
				_objects.Push(objectGenerator());
			}
		}

		public T GetObject() { return _objects.Pop(); }
		public void PutObject(T obj) { _objects.Push(obj); }

	}
}