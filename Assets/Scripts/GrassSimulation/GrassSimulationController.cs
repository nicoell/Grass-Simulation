using System;
using System.IO;
using System.Runtime.InteropServices;
using GrassSimulation.Core;
using GrassSimulation.Core.Inputs;
using GrassSimulation.Core.Utils;
using GrassSimulation.StandardInputs;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;

/* TODO:
 Dynamic Tessellation Level
Wind
Collision
Blade Texture
Blade Width Correction Minimal-width shape

Statistics
Blades drawn
*/

namespace GrassSimulation
{
	[ExecuteInEditMode]
	public class GrassSimulationController : MonoBehaviour
	{
		[EmbeddedScriptableObject(true)]
		public SimulationContext Context;
		
		//Debug Stuff
		private StreamWriter _writer;
		private string _logOutput = "";
		private KeyCode[] _testSettings = {
			KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3
		};
		private KeyCode[] _switchSimulationTexture = {
			KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7
		};
		private int _simulationTextureResolution = 32;
		private KeyCode _toggleBlossoms = KeyCode.B;
		private bool _debugColors = false;
		private KeyCode _toggleDebugColors = KeyCode.V;
		private KeyCode _toggleTerrain = KeyCode.T;
		private KeyCode _switchCamera = KeyCode.C;
		private KeyCode _switchGrassMapInput = KeyCode.G;
		private ClassTypeReference _textureGrassMapInputType;
		private GrassMapInput _textureGrassMapInput;
		private KeyCode _printDebugInfo = KeyCode.P;
		private Vector3 _cameraBackupPos;
		private Quaternion _cameraBackupRot;

		public Text AverageFps;
		private DateTime _debugInfoTime;
		private float _debubInfoTimespan = 3f;
		public Text DebugInfo;
		
		public float UpdateInterval = 10.0f;
		private double _lastInterval;
		private int _frames = 0;
		private float _fps;

		// Use this for initialization
		private void OnEnable()
		{
			InitLogWriter();
			UpdateDebugInfo("Enabled Log to File");
			PrepareSimulation();
		}

		public void PrepareSimulation()
		{
			_debugInfoTime = DateTime.Now;
			if (Context == null)
			{
				Context = ScriptableObject.CreateInstance<SimulationContext>();
			} else
			{
				Context.Destroy();
			}
			UpdateDebugInfo("Preparing Simulation");
			Context.Init();
			_lastInterval = Time.realtimeSinceStartup;
			_frames = 0;
		}

		// Update is called once per frame
		private void Update()
		{
			++_frames;
			float timeNow = Time.realtimeSinceStartup;
			if (Context.IsReady)
			{
				if (timeNow > _lastInterval + UpdateInterval)
				{
					_fps = (float)(_frames / (timeNow - _lastInterval));
					_frames = 0;
					_lastInterval = timeNow;
					AverageFps.text = '\u00D8' + " FPS: " + _fps;
					Debug.Log('\u00D8' + " FPS: " + _fps);
				}
				Context.CollisionTextureRenderer.UpdateDepthTexture();
				Context.WindManager.Update();
				//Context.ProceduralWind.Update();
				//Context.WindFieldRenderer.Update();
				Context.PatchContainer.Draw();
				//Context.BillboardTexturePatchContainer.Draw();
			}

			for (int i = 0; i < _switchSimulationTexture.Length; i++)
			{
				if (Input.GetKeyDown(_switchSimulationTexture[i]))
				{
					_simulationTextureResolution = (int) Mathf.Pow(2, 3 + i);
					UpdateDebugInfo("Set SimulationTexture Resolution to "+_simulationTextureResolution);
				}
			}

			for (int i = 0; i < _testSettings.Length; i++)
				if (Input.GetKeyDown(_testSettings[i])) LoadTestSettings(i);

			if (Input.GetKeyDown(_printDebugInfo))
			{
				UpdateDebugInfo(Context.PrintDebugInfo(), 30f);
			}
			if (Input.GetKeyDown(_toggleBlossoms))
			{
				UpdateDebugInfo(Context.BladeContainer.Blades[0].HasBlossom ? "Deactivate Blossoms" : "Activate Blossoms");
				Context.BladeContainer.Blades[0].HasBlossom = !Context.BladeContainer.Blades[0].HasBlossom;
			}

			if (Input.GetKeyDown(_toggleDebugColors))
			{
				_debugColors = !_debugColors;
				Shader.SetGlobalInt("RenderDebugColor", _debugColors ? 1 : 0);
			}
			if (Input.GetKeyDown(_toggleTerrain))
			{
				FindObjectOfType<Terrain>().enabled = !FindObjectOfType<Terrain>().enabled;
			}
			if (Input.GetKeyDown(_switchCamera))
			{
				if (!Context.Camera.GetComponent<Animator>().enabled)
				{
					_cameraBackupPos = Context.Camera.transform.position;
					_cameraBackupRot = Context.Camera.transform.rotation;
				} else
				{
					Context.Camera.transform.position = _cameraBackupPos;
					Context.Camera.transform.rotation = _cameraBackupRot;
				}
				Context.Camera.GetComponent<Animator>().enabled = !Context.Camera.GetComponent<Animator>().enabled;
			}

			if (Input.GetKeyDown(_switchGrassMapInput))
			{
				if (Context.GrassMapInput.GetType() == typeof(RandomGrassMapInput))
				{
					UpdateDebugInfo("Switch to Texture GrassMap Input");
					Context.GrassMapInput = _textureGrassMapInput;
					Context.GrassMapInputType.Type = _textureGrassMapInputType;
				} else
				{
					UpdateDebugInfo("Switch to Uniform GrassMap Input");
					_textureGrassMapInput = Context.GrassMapInput;
					_textureGrassMapInputType = Context.GrassMapInputType;
					
					Context.GrassMapInput = Activator.CreateInstance(typeof(RandomGrassMapInput)) as RandomGrassMapInput;
					Context.GrassMapInputType.Type = typeof(RandomGrassMapInput);
				}
			}
		}
		
		private void InitLogWriter()
		{
			_writer = File.AppendText("log.txt");
			Application.logMessageReceived += LogToFile;
		}
		
		private void LogToFile(string logString, string stackTrace, LogType type)
		{
			var entry = string.Format("\n{0:T}\t{1}:\n{2}\n{3}", DateTime.Now, type, logString, stackTrace);
			_writer.Write(entry);
		}

		private void LoadTestSettings(int i)
		{
			switch (i)
			{
				case 0:
					UpdateDebugInfo("Loading Test Settings " + i + " (High) ");
					Debug.Log("Loading Test Settings " + i + " (High) ");
					Context.Settings.GrassDataResolution = _simulationTextureResolution;
					Context.Settings.CollisionDepthResolution = 1024;
					Context.Settings.BillboardTextureResolution = 128;
					Context.Settings.LodTessellationMin = 4;
					Context.Settings.LodTessellationMax = 40;
					Context.Settings.LodDistanceTessellationMin = 4;
					Context.Settings.LodDistanceTessellationMax = 32;
					Context.Settings.LodInstancesGeometry = 512;
					Context.Settings.LodInstancesBillboardCrossed = 256;
					Context.Settings.LodInstancesBillboardScreen = 128;
					Context.Settings.LodGeometryTransitionSegments = 32;
					Context.Settings.LodBillboardCrossedTransitionSegments = 32;
					Context.Settings.LodBillboardScreenTransitionSegments = 16;
					Context.Settings.LodDistanceGeometryStart = 6;
					Context.Settings.LodDistanceGeometryEnd = 64;
					Context.Settings.LodDistanceBillboardCrossedStart = 12;
					Context.Settings.LodDistanceBillboardCrossedPeak = 56;
					Context.Settings.LodDistanceBillboardCrossedEnd = 144;
					Context.Settings.LodDistanceBillboardScreenStart = 64;
					Context.Settings.LodDistanceBillboardScreenPeak = 128;
					Context.Settings.LodDistanceBillboardScreenEnd = 512;
					break;
				case 1:
					UpdateDebugInfo("Loading Test Settings " + i + " (Normal) ");
					Debug.Log("Loading Test Settings " + i + " (Normal) ");
					Context.Settings.GrassDataResolution = _simulationTextureResolution;
					Context.Settings.CollisionDepthResolution = 512;
					Context.Settings.BillboardTextureResolution = 64;
					Context.Settings.LodTessellationMin = 3;
					Context.Settings.LodTessellationMax = 32;
					Context.Settings.LodDistanceTessellationMin = 4;
					Context.Settings.LodDistanceTessellationMax = 24;
					Context.Settings.LodInstancesGeometry = 384;
					Context.Settings.LodInstancesBillboardCrossed = 160;
					Context.Settings.LodInstancesBillboardScreen = 96;
					Context.Settings.LodGeometryTransitionSegments = 32;
					Context.Settings.LodBillboardCrossedTransitionSegments = 32;
					Context.Settings.LodBillboardScreenTransitionSegments = 16;
					Context.Settings.LodDistanceGeometryStart = 4;
					Context.Settings.LodDistanceGeometryEnd = 48;
					Context.Settings.LodDistanceBillboardCrossedStart = 12;
					Context.Settings.LodDistanceBillboardCrossedPeak = 40;
					Context.Settings.LodDistanceBillboardCrossedEnd = 104;
					Context.Settings.LodDistanceBillboardScreenStart = 48;
					Context.Settings.LodDistanceBillboardScreenPeak = 96;
					Context.Settings.LodDistanceBillboardScreenEnd = 384;
					break;
				case 2:
					UpdateDebugInfo("Loading Test Settings " + i + " (Low) ");
					Debug.Log("Loading Test Settings " + i + " (Low) ");
					Context.Settings.GrassDataResolution = _simulationTextureResolution;
					Context.Settings.CollisionDepthResolution = 256;
					Context.Settings.BillboardTextureResolution = 64;
					Context.Settings.LodTessellationMin = 2;
					Context.Settings.LodTessellationMax = 16;
					Context.Settings.LodDistanceTessellationMin = 0;
					Context.Settings.LodDistanceTessellationMax = 16;
					Context.Settings.LodInstancesGeometry = 192;
					Context.Settings.LodInstancesBillboardCrossed = 96;
					Context.Settings.LodInstancesBillboardScreen = 48;
					Context.Settings.LodGeometryTransitionSegments = 32;
					Context.Settings.LodBillboardCrossedTransitionSegments = 32;
					Context.Settings.LodBillboardScreenTransitionSegments = 16;
					Context.Settings.LodDistanceGeometryStart = 0;
					Context.Settings.LodDistanceGeometryEnd = 40;
					Context.Settings.LodDistanceBillboardCrossedStart = 4;
					Context.Settings.LodDistanceBillboardCrossedPeak = 32;
					Context.Settings.LodDistanceBillboardCrossedEnd = 64;
					Context.Settings.LodDistanceBillboardScreenStart = 40;
					Context.Settings.LodDistanceBillboardScreenPeak = 56;
					Context.Settings.LodDistanceBillboardScreenEnd = 256;
					break;
				default: 
					return;
			}
			PrepareSimulation();
		}

		private void UpdateDebugInfo(string text, float timespan = 3f)
		{
			_debubInfoTimespan = timespan;
			_debugInfoTime = DateTime.Now;
			DebugInfo.text = text;
		}
		
		private void OnDrawGizmos()
		{
			if (Context.IsReady)
			{
				Context.PatchContainer.DrawGizmo();
				Context.BillboardTexturePatchContainer.DrawGizmo();
			}
		}

		private void OnDisable()
		{
			if (Context.IsReady) Context.Destroy();
			_writer.Close();
			Application.logMessageReceived -= LogToFile;
		}

		private void OnGUI()
		{
			DebugInfo.color = new Color(DebugInfo.color.r, DebugInfo.color.g, DebugInfo.color.b, Mathf.SmoothStep(1, 0, (float)(DateTime.Now - _debugInfoTime).TotalSeconds / _debubInfoTimespan));
			if (Context.IsReady)  Context.OnGUI();
		}
	}
}