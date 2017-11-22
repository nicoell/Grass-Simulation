using UnityEngine;

namespace GrassSimulation.Utils {
	public static class Bounds
	{
		public class BoundsVertices
		{
			public Vector3[] BoundVertices;
			
			public BoundsVertices(UnityEngine.Bounds bounds)
			{
				BoundVertices = new Vector3[8];
				BoundVertices[0] = bounds.min;
				BoundVertices[1] = bounds.max;
				BoundVertices[2] = new Vector3(BoundVertices[0].x, BoundVertices[0].y, BoundVertices[1].z);
				BoundVertices[3] = new Vector3(BoundVertices[0].x, BoundVertices[1].y, BoundVertices[0].z);
				BoundVertices[4] = new Vector3(BoundVertices[1].x, BoundVertices[0].y, BoundVertices[0].z);
				BoundVertices[5] = new Vector3(BoundVertices[0].x, BoundVertices[1].y, BoundVertices[1].z);
				BoundVertices[6] = new Vector3(BoundVertices[1].x, BoundVertices[0].y, BoundVertices[1].z);
				BoundVertices[7] = new Vector3(BoundVertices[1].x, BoundVertices[1].y, BoundVertices[0].z);
			}
			
			/// <summary>
			///   <para>Does a bounding box lies fully or partially inside a sphere?</para>
			/// </summary>
			/// <param name="vertices">Vertices representation of a bounding box.</param>
			/// <param name="center">Center of the sphere.</param>
			/// <param name="radius">Radius of the sphere.</param>
			public static bool IntersectsSphere(BoundsVertices vertices, Vector3 center, float radius)
			{
				foreach (var vertex in vertices.BoundVertices)
				{
					if (Vector3.Distance(vertex, center) <= radius) return true;
				}
				return false;
			}
		}
	}
}