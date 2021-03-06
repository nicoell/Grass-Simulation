﻿using UnityEngine;

namespace GrassSimulation.Core.Inputs
{
	public abstract class HeightInput : ScriptableObject
	{
		/// <summary>
		///     <para>Gets the height value at point x,y.</para>
		/// </summary>
		/// <returns>A float height value in range 0..Max Height, can be greater than 1</returns>
		/// <param name="x">x coordinate in range 0..1</param>
		/// <param name="y">y coordnate in range 0..1</param>
		public abstract float GetHeight(float x, float y);

		/// <summary>
		///     <para>Gets the smallest steps at which the height will get sampled.</para>
		///     <returns>A vector with the sampling rate for x and y.</returns>
		/// </summary>
		public abstract Vector2 GetSamplingRate();
	}
}