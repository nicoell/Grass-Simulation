using UnityEngine;

namespace GrassSimulation.Core.Wind {
	public class Whirlwind : WindLayer
	{
		private const int _windType = 1;
		public override int WindType { get { return _windType; } }
		[Header("Wind Settings")]
		[Range(0, 32)]
		public float WindFrequencyMagMin = 1f;
		[Range(0, 32)]
		public float WindFrequencyMagMax = 2f;
		[Range(0, 1024)]
		public float WindMagnitudeMax = 8f;
		[Range(1, 64)]
		public float WindRadius = 1f;
		
		private float _shiftPeriodMag;
		private float _wave;

		private float _magnitude;
		private float _newMagnitude;
		private float _oldMagnitude;
		private float _magnitudeChangeSpeed;
		private float _frequencyMag;
		
		private new void Start()
		{
			base.Start();
			_frequencyMag = WindFrequencyMagMin + (float) Ctx.Random.NextDouble() *
			                (WindFrequencyMagMax - WindFrequencyMagMin);
			_magnitudeChangeSpeed = WindFrequencyMagMin + (float) Ctx.Random.NextDouble() *
			                        (_frequencyMag - WindFrequencyMagMin);
			_shiftPeriodMag = 0;
			_magnitude = 0;
			_oldMagnitude = 0.5f;
			_newMagnitude = 0.5f;
			_wave = 0;
		}
		
		public override WindLayerData GetWindData()
		{
			_shiftPeriodMag += Time.deltaTime;
			_wave += Time.deltaTime;


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

			_magnitude = Mathf.SmoothStep(_oldMagnitude, _newMagnitude, _shiftPeriodMag / _magnitudeChangeSpeed);

			return new WindLayerData
			{
				WindType = WindType,
				WindData = new Vector4(transform.position.x, transform.position.y, transform.position.z, WindRadius),
				WindData2 = new Vector4(_magnitude, 0, 0, 0)
			};
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, WindRadius);
		}
	}
}