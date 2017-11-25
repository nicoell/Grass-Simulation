using UnityEngine;

namespace GrassSimulation.Core.Inputs
{
	public abstract class PositionInput : ScriptableObject
	{
		/// <summary>
		///   <para>Gets the xz position for grass in a patch which also acts as uv.</para>
		/// </summary>
		/// <returns>A Vector2 where x:x and y:z with each element in range 0..1</returns>
		/// <param name="id">The inded at which the position will be placed in the UvBuffer</param>
		public abstract Vector2 GetPosition(int id);
	}
}