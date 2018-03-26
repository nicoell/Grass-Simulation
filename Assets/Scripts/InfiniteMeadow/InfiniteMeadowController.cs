using System.Collections.Generic;
using InfiniteMeadow.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfiniteMeadow
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public sealed class InfiniteMeadowController : MonoBehaviour
	{
		private CoreManager _coreManager;
		private InfiniteMeadowSettings _infiniteMeadowSettings;

		[Header("General")]
		[Tooltip("Makes the object this manager is attached to not be destroyed automatically when loading a new scene.")]
		public bool EnableDontDestroyOnLoad;

		[Header("Dependencies")]
		public List<Camera> Cameras;
		public Shader MeadowShader = Shader.Find("TODO");
		public ComputeShader SimulationShader;
		
		
		public InfiniteMeadowSettings InfiniteMeadowSettings
		{
			get { return _infiniteMeadowSettings; }
			set
			{
				_infiniteMeadowSettings = value;
				_coreManager.Init();
			}
		}
		private static InfiniteMeadowController Instance { get; set; }

		private void Awake()
		{
			if (Instance != null)
			{
				Debug.LogError(
					"Multiple InfiniteMeadowManager detected. Deactivating InfiniteMeadowManager detected on GameObject " +
					gameObject);
				gameObject.GetComponent<InfiniteMeadowController>().enabled = false;
				return;
			}
			#if !UNITY_EDITOR
			if (EnableDontDestroyOnLoad) DontDestroyOnLoad(gameObject);
			#endif
			Instance = this;
			_coreManager = new CoreManager();
		}

		private void Start() { ValidateInit(); }

		private void Update()
		{
			#if UNITY_EDITOR
			// Force call Awake and Start after script recompilation.
			if (Application.isEditor && !Application.isPlaying && _coreManager == null)
			{
				Awake();
				Start();
			}
			#endif

			ValidateInit();
			_coreManager.Update();
		}

		
		
		private void ValidateInit()
		{
			if (_infiniteMeadowSettings.InitRequired) _coreManager.Init();

			_infiniteMeadowSettings.InitRequired = false;
		}

		private void OnEnable()
		{
			SceneManager.sceneUnloaded += SceneManagerOnSceneUnloaded;
			SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
		}

		private void OnDisable()
		{
			SceneManager.sceneUnloaded -= SceneManagerOnSceneUnloaded;
			SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
		}

		private void SceneManagerOnSceneUnloaded(Scene arg0) { _coreManager.Reset(); }

		private void SceneManagerOnSceneLoaded(Scene arg0, LoadSceneMode loadSceneMode) { Start(); }

		public static InfiniteMeadowController GetInstance() { return Instance; }
	}
}