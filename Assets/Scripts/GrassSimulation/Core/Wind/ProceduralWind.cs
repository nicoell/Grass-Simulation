using UnityEngine;

namespace GrassSimulation.Core.Wind
{
	public class ProceduralWind : ContextRequirement
	{
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
		public Vector4 WindData;

		public ProceduralWind(SimulationContext ctx) : base(ctx)
		{
			_frequencyDir = Ctx.Settings.WindFrequencyDirMin + (float) Ctx.Random.NextDouble() *
			                (Ctx.Settings.WindFrequencyDirMax - Ctx.Settings.WindFrequencyDirMin);
			_directionChangeSpeed = Ctx.Settings.WindFrequencyDirMin + (float) Ctx.Random.NextDouble() *
			                        (_frequencyDir - Ctx.Settings.WindFrequencyDirMin);
			_frequencyMag = Ctx.Settings.WindFrequencyMagMin + (float) Ctx.Random.NextDouble() *
			                (Ctx.Settings.WindFrequencyMagMax - Ctx.Settings.WindFrequencyMagMin);
			_magnitudeChangeSpeed = Ctx.Settings.WindFrequencyMagMin + (float) Ctx.Random.NextDouble() *
			                        (_frequencyMag - Ctx.Settings.WindFrequencyMagMin);
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
			
			WindData = new Vector4();
		}

		public void Update()
		{
			_shiftPeriodDir += Time.deltaTime;
			_shiftPeriodMag += Time.deltaTime;
			_wave += Time.deltaTime;

			if (_shiftPeriodDir >= _frequencyDir)
			{
				_shiftPeriodDir = 0;
				_frequencyDir = Ctx.Settings.WindFrequencyDirMin + (float) Ctx.Random.NextDouble() *
				                (Ctx.Settings.WindFrequencyDirMax - Ctx.Settings.WindFrequencyDirMin);
				_directionChangeSpeed = Ctx.Settings.WindFrequencyDirMin + (float) Ctx.Random.NextDouble() *
				                        (_frequencyDir - Ctx.Settings.WindFrequencyDirMin);
				float phi = (float) Ctx.Random.NextDouble() * Mathf.PI * 2f;
				_oldDirection = _newDirection;
				_newDirection = new Vector3(Mathf.Sin(phi), (float) Ctx.Random.NextDouble() - 0.5f, Mathf.Cos(phi)).normalized;
			}

			if (_shiftPeriodMag >= _frequencyMag)
			{
				_shiftPeriodMag = 0;
				_frequencyMag = Ctx.Settings.WindFrequencyMagMin + (float) Ctx.Random.NextDouble() *
				                (Ctx.Settings.WindFrequencyMagMax - Ctx.Settings.WindFrequencyMagMin);
				_magnitudeChangeSpeed = Ctx.Settings.WindFrequencyMagMin + (float) Ctx.Random.NextDouble() *
				                (_frequencyMag - Ctx.Settings.WindFrequencyMagMin);
				_oldMagnitude = _newMagnitude;
				_newMagnitude = (float) Ctx.Random.NextDouble() * Ctx.Settings.WindMagnitudeMax;
			}

			_direction = Vector3.Slerp(_oldDirection, _newDirection, Mathf.Clamp01(_shiftPeriodDir / _directionChangeSpeed));
			_magnitude = Mathf.SmoothStep(_oldMagnitude, _newMagnitude, _shiftPeriodMag / _magnitudeChangeSpeed);
			_wind = _direction * _magnitude;
			WindData = new Vector4(_wind.x, _wind.y, _wind.z, _wave);
			
			Ctx.GrassSimulationComputeShader.SetVector("WindData", WindData);
		}
	}
}