using UnityEngine;

namespace GrassSimulation.Core.Inputs
{
	public abstract class GrassMapInput : ScriptableObject
	{
		/// <summary>
		///     <para>Gets the type of grass at given position.</para>
		/// </summary>
		/// <returns>The index corresponding to the array position in the GrassContainer represtenting the grass type.</returns> 
		public abstract byte GetGrassType(float x, float y, float z);
	}
}