using System.Collections.Generic;
using UnityEngine;

namespace InfiniteMeadow.Core
{
	internal sealed class CoreManager : Manager<CoreManager>
	{
		private readonly LodManager _lodManager;
		private readonly List<InfiniteMeadowInstance> _meadowInstances;
		private readonly Camera[] _renderCameras;
		private CollisionManager _collisionManager;
		private PatchManager _patchManager;
		private ShaderManager _shaderManager;
		private TextureManager _textureManager;
		private WindManager _windManager;

		public CoreManager()
		{
			Instance = this;
			_meadowInstances = new List<InfiniteMeadowInstance>();
			_renderCameras = new Camera[] { };

			_patchManager = new PatchManager();
			_lodManager = new LodManager();
			_shaderManager = new ShaderManager();
			_textureManager = new TextureManager();
			_windManager = new WindManager();
			_collisionManager = new CollisionManager();
		}

		public void Init()
		{
			var controller = InfiniteMeadowController.GetInstance();
			controller.Cameras.CopyTo(_renderCameras);
		}

		public void Update() { }

		public void Reset()
		{
			foreach (var meadowInstance in _meadowInstances) meadowInstance.IsActive = false;
			_meadowInstances.Clear();
		}

		public bool ApplyAsInstance(InfiniteMeadowInstance instance)
		{
			if (!IsInRange(instance.GetBounds())) return false;
			_meadowInstances.Add(instance);
			return true;
		}

		public bool PersistAsInstance(InfiniteMeadowInstance instance)
		{
			if (IsInRange(instance.GetBounds())) return true;
			_meadowInstances.Remove(instance);
			return false;
		}

		private bool IsInRange(Bounds bounds) { return _lodManager.GetBounds().Intersects(bounds); }
	}
}