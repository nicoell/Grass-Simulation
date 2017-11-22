using UnityEngine;

namespace GrassSimulation.DataProvider
{
	public abstract class DimensionsProvider : MonoBehaviour
	{
		/// <summary>
		///     <para>Gets the width e.g. of a terrain used for grid and hierarchy creation.</para>
		/// </summary>
		public abstract float GetWidth();

		/// <summary>
		///     <para>Gets the depth e.g. of a terrain used for grid and hierarchy creation.</para>
		/// </summary>
		public abstract float GetDepth();

		/// <summary>
		///     <para>Gets the total height by which all height values get scaled.</para>
		/// </summary>
		public abstract float GetHeight();
		
		/// <summary>
		///     <para>Gets the axis aligned bounding box covering the total area of grass simulation.</para>
		/// </summary>
		public abstract Bounds GetBounds();
	}
}