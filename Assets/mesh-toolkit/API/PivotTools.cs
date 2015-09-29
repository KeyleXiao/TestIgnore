using UnityEngine;
using System.Collections;

namespace MeshTK
{
	/// <summary>
	/// Pivot Tools - Copyright EJM Software 2015
	/// Edit the pivot point of Mesh Objects
	/// </summary>
	public static class PivotTools
	{
		/// <summary>
		/// Centers the pivot of the mesh
		/// </summary>
		/// <returns>
		/// The offset the geometry was moved by to center the pivot.
		/// </returns>
		/// <param name='mesh'>
		/// Mesh
		/// </param>
		public static Vector3 Center (Mesh mesh)
		{
			mesh.RecalculateBounds ();
			Vector3 diff = mesh.bounds.center;
			PivotTools.Move (mesh, diff);
			return diff;
		}
		/// <summary>
		/// Moves the pivot of a mesh by given offset
		/// </summary>
		/// <param name='mesh'>
		/// Mesh
		/// </param>
		/// <param name='offset'>
		/// Vector3 to move pivot
		/// </param>
		public static void Move (Mesh mesh, Vector3 offset)
		{
			Vector3[] vertices = mesh.vertices;
			for (int i = 0; i < vertices.Length; i++) {
				vertices [i] = new Vector3 (vertices [i].x - offset.x, vertices [i].y - offset.y, vertices [i].z - offset.z);
			}
			mesh.vertices = vertices;
			mesh.RecalculateBounds ();
		}
		/// <summary>
		/// Sets the pivot to a given Vector3 offset from the center of the Mesh bounds.
		/// </summary>
		public static Vector3 Set (Mesh mesh, Vector3 offset)
		{
			mesh.RecalculateBounds ();
			Vector3 diff = mesh.bounds.center - offset;
			PivotTools.Move (mesh, diff);
			return diff;
		}
	}
}