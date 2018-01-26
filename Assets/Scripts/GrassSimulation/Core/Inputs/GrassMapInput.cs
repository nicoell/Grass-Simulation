using UnityEngine;

namespace GrassSimulation.Core.Inputs
{
	public abstract class GrassMapInput : ScriptableObject
	{
		/// <summary>
		///     <para>Gets the type of grass at given position.</para>
		/// </summary>
		/// <returns>The index corresponding to the array position in the GrassContainer represtenting the grass type.</returns> 
		public abstract int GetGrassType(float x, float y, float z);

		
		/// <summary>
		///     <para>Gets the density of grass at given position.</para>
		/// </summary>
		/// <returns>The float density value between 0 and 1.</returns> 
		public abstract float GetDensity(float x, float y, float z);
		
		/// <summary>
		///     <para>Gets the height modifier of grass at given position.</para>
		/// </summary>
		/// <returns>Height modifier value between 0 and 1.</returns> 
		public abstract float GetHeightModifier(float x, float y, float z);
	}
}