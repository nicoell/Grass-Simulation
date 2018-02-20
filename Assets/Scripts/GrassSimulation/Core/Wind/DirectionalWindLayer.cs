using UnityEngine;

namespace GrassSimulation.Core.Wind {
	public class DirectionalWindLayer : WindLayer
	{
		private const int _windType = 0;
		public override int WindType { get { return _windType; } }
		
		[Header("Wind Settings")]
		[Range(0, 32)]
		public float WindFrequencyDirMin = 5f;
		[Range(0, 32)]
		public float WindFrequencyDirMax = 8f;
		[Range(0, 32)]
		public float WindFrequencyMagMin = 1f;
		[Range(0, 32)]
		public float WindFrequencyMagMax = 2f;
		[Range(0, 1024)]
		public float WindMagnitudeMax = 8f;
		[Range(0, 128)]
		public float WindXZPosFactor = 0.75f;
		[Range(0, 128)]
		public float WindXYPosFactor = 0.5f;
		[Range(0, 128)]
		public float WindYZPosFactor = 0.25f;
		
		private float _shiftPeriodDir;
		private float _shiftPeriodMag;
		private float _wave;
		private Vector3 _direction;
		private Vector3 _newDirection;
		private Vector3 _oldDirection;
		private float _directionChangeSpeed;
		private Vector3 _wind;
		private float _magnitude;
		private float _newMagnitude;
		private float _oldMagnitude;
		private float _magnitudeChangeSpeed;
		private float _frequencyDir;
		private float _frequencyMag;
		private Vector4 _windData;

		private void OnEnable() { base.Start(); }

		private new void Start()
		{
			base.Start();
			
			_frequencyDir = WindFrequencyDirMin + (float) Ctx.Random.NextDouble() *
			                (WindFrequencyDirMax - WindFrequencyDirMin);
			_directionChangeSpeed = WindFrequencyDirMin + (float) Ctx.Random.NextDouble() *
			                        (_frequencyDir - WindFrequencyDirMin);
			_frequencyMag = WindFrequencyMagMin + (float) Ctx.Random.NextDouble() *
			                (WindFrequencyMagMax - WindFrequencyMagMin);
			_magnitudeChangeSpeed = WindFrequencyMagMin + (float) Ctx.Random.NextDouble() *
			                        (_frequencyMag - WindFrequencyMagMin);
			_shiftPeriodDir = 0;
			_shiftPeriodMag = 0;
			float phi = (float) Ctx.Random.NextDouble() * Mathf.PI * 2f;
			_direction = Vector3.zero;
			_oldDirection = new Vector3(Mathf.Sin(phi), (float) Ctx.Random.NextDouble() - 0.5f, Mathf.Cos(phi)).normalized;
			_newDirection = _oldDirection;
			_magnitude = 0;
			_oldMagnitude = 0.5f;
			_newMagnitude = 0.5f;
			_wave = 0;
			_wind = Vector3.zero;
			
			_windData = new Vector4();
		}

		public override WindLayerData GetWindData()
		{
			_shiftPeriodDir += Time.deltaTime;
			_shiftPeriodMag += Time.deltaTime;
			_wave += Time.deltaTime;

			if (_shiftPeriodDir >= _frequencyDir)
			{
				_shiftPeriodDir = 0;
				_frequencyDir = WindFrequencyDirMin + (float) Ctx.Random.NextDouble() *
				                (WindFrequencyDirMax - WindFrequencyDirMin);
				_directionChangeSpeed = WindFrequencyDirMin + (float) Ctx.Random.NextDouble() *
				                        (_frequencyDir - WindFrequencyDirMin);
				float phi = (float) Ctx.Random.NextDouble() * Mathf.PI * 2f;
				_oldDirection = _newDirection;
				_newDirection = new Vector3(Mathf.Sin(phi), (float) Ctx.Random.NextDouble() - 0.5f, Mathf.Cos(phi)).normalized;
			}

			if (_shiftPeriodMag >= _frequencyMag)
			{
				_shiftPeriodMag = 0;
				_frequencyMag = WindFrequencyMagMin + (float) Ctx.Random.NextDouble() *
				                (WindFrequencyMagMax - WindFrequencyMagMin);
				_magnitudeChangeSpeed = WindFrequencyMagMin + (float) Ctx.Random.NextDouble() *
				                        (_frequencyMag - WindFrequencyMagMin);
				_oldMagnitude = _newMagnitude;
				_newMagnitude = (float) Ctx.Random.NextDouble() * WindMagnitudeMax;
			}

			_direction = Vector3.Slerp(_oldDirection, _newDirection, Mathf.Clamp01(_shiftPeriodDir / _directionChangeSpeed));
			_magnitude = Mathf.SmoothStep(_oldMagnitude, _newMagnitude, _shiftPeriodMag / _magnitudeChangeSpeed);
			_wind = _direction * _magnitude;
			_windData = new Vector4(_wind.x, _wind.y, _wind.z, _wave);			
			var data = new WindLayerData
			{
				WindType = IsActive ? WindType : -1,
				WindData = _windData,
				WindData2 =  new Vector4(WindXZPosFactor, WindXYPosFactor, WindYZPosFactor, 0)
			};
			return data;
		}
	}
}