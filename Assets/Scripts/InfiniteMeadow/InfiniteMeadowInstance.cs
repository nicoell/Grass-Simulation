using System;
using InfiniteMeadow.Core;
using UnityEngine;

namespace InfiniteMeadow
{
	/// <inheritdoc />
	/// <summary>
	/// </summary>
	internal class InfiniteMeadowInstance : MonoBehaviour
	{
		[NonSerialized]
		private bool _isActive = false;
		public bool IsActive { get { return _isActive; } set { _isActive = value; } }

		private void Awake() { }

		// Use this for initialization
		private void Start() { }

		// Update is called once per frame
		private void Update()
		{
			if (!IsActive) IsActive = CoreManager.GetInstance().ApplyAsInstance(this);
			if (IsActive) IsActive = CoreManager.GetInstance().PersistAsInstance(this);
		}

		internal Bounds GetBounds()
		{
			return new Bounds();
		}

	}
}