using UnityEngine;

namespace GrassSimulation.Core.Inputs
{
	public abstract class GrassMapInput : ScriptableObject
	{
		/// <summary>
		///     <para>Gets the color and type of grass at given position.</para>
		/// </summary>
		/// <returns>Color32 where rgb is the color and alpha is the TypeId</returns> 
		public abstract Color32 GetColorAndType(float x, float y, float z);
	}
}