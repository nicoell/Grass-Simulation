using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfiniteMeadow
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public partial class InfiniteMeadowManager : MonoBehaviour
	{
		private CoreManager _coreManager;

		[Header("General")]
		[Tooltip("Makes the object this manager is attached to not be destroyed automatically when loading a new scene.")]
		public bool EnableDontDestroyOnLoad;
		public InfiniteMeadowSettings InfiniteMeadowSettings;
		private static InfiniteMeadowManager Instance { get; set; }

		private void Awake()
		{
#if !UNITY_EDITOR
			if (EnableDontDestroyOnLoad) DontDestroyOnLoad(gameObject);
#endif
			if (Instance != null) return;
			Instance = this;
			_coreManager = new CoreManager();
		}

		private void Start()
		{

			_coreManager.Init();
			
		}

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
			
			_coreManager.Update();
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

		private void SceneManagerOnSceneLoaded(Scene arg0, LoadSceneMode loadSceneMode) { _coreManager.Init(); }

		public static InfiniteMeadowManager GetInfiniteMeadowManager() { return Instance; }
	}
}