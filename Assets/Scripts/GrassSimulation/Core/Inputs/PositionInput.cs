using UnityEngine;

namespace GrassSimulation.Core.Inputs
{
	public abstract class PositionInput : ScriptableObject
	{
		/// <summary>
		///   <para>Gets the xz position for grass in a patch which also acts as uv.</para>
		/// </summary>
		/// <returns>A Vector2 where x:x and y:z with each element in range 0..1</returns>
		/// <param name="id">The index at which the position will be placed in the UvBuffer</param>
		public abstract Vector2 GetPosition(int id);

		/// <summary>
		///   <para>Gets the number of positions that are consecutively very similar, e.g. in clumped grass.</para>
		/// </summary>
		/// <returns>A unsigned int number of the repetition count. If no positions are similar, this should be 1.</returns>
		/// <remarks>This is needed to avoid clumping billboard grass.</remarks>
		public abstract uint GetRepetitionCount();
	}
}