using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MeshTK
{
	/// <summary>
	/// Vertex Tools - By EJM Software
	/// Edit the vertices of Mesh Objects
	/// </summary>
	public class VertexTools
	{
		/// <summary>
		/// Merges all double vertices in mesh
		/// </summary>
		/// <param name='mesh'>
		/// Mesh to merge vertices
		/// </param>
		/// <param name='threshold'>
		/// Threshold for merging vertices
		/// </param>
		public static void MergeDoubles(Mesh mesh, float threshold = 0.001f){
			// CHECK WHAT DATA WILL BE COPIED
			bool hasuv = (mesh.uv.Length > 0);
			bool hasuv2 = (mesh.uv2.Length > 0);
#if UNITY_5
			bool hasuv3 = (mesh.uv3.Length > 0);
			bool hasuv4 = (mesh.uv4.Length > 0);
#endif
			bool hascolors = (mesh.colors.Length > 0);
			bool hastangents = (mesh.tangents.Length > 0);
			bool hasboneweights = (mesh.boneWeights.Length > 0);
			// CACHE THE ORIGINAL MESH DATA IN AN ARRAY
			Vector3[] sourceverts = mesh.vertices;
			Matrix4x4[] sourcebindposes = mesh.bindposes;
			int[] sourcesubmeshes = new int[mesh.subMeshCount];
			for (int i=0; i<mesh.subMeshCount; i++) {
				sourcesubmeshes[i] = mesh.GetTriangles(i).Length;
			}
			// CREATE LISTS FOR NEW DATA
			List<Vector3> newverts = new List<Vector3> ();
			List<Vector2> newuvs = new List<Vector2> ();
			List<Vector2> newuv2s = new List<Vector2> ();
#if UNITY_5
			List<Vector2> newuv3s = new List<Vector2> ();
			List<Vector2> newuv4s = new List<Vector2> ();
#endif
			List<Color> newcolors = new List<Color> ();
			List<Vector3> newnormals = new List<Vector3> ();
			List<Vector4> newtangents = new List<Vector4> ();
			List<BoneWeight> newboneweights = new List<BoneWeight> ();
			// MERGE DUPLICATE VERTICES
			for (int i=0;i<sourceverts.Length;i++) {
				Vector3 vert = sourceverts[i];
				if(!newverts.Exists(p => Vector3.Distance (vert, p)<=threshold)){
					newverts.Add (vert);
					if (hasuv){newuvs.Add (mesh.uv[i]);}
					if (hasuv2){newuv2s.Add (mesh.uv2[i]);}
#if UNITY_5
					if (hasuv3){newuv3s.Add (mesh.uv3[i]);}
					if (hasuv4){newuv4s.Add (mesh.uv4[i]);}
#endif
					newnormals.Add (mesh.normals[i]);
					if (hascolors){newcolors.Add (mesh.colors[i]);}
					if (hastangents){newtangents.Add (mesh.tangents[i]);}
					if (hasboneweights){newboneweights.Add (mesh.boneWeights[i]);}
				}
			}
			// Rebuild triangles using new vertices
			int[] tris = mesh.triangles;
			for (int i = 0; i < tris.Length; ++i) {
				for (int j = 0; j < newverts.Count; ++j) {
					if (Vector3.Distance (newverts [j], sourceverts [tris [i]]) <= threshold) {
						tris [i] = j;
						break;
					}
				}
			}
			// PACK THE NEW DATA BACK INTO THE MESH
			mesh.Clear();
			mesh.vertices = newverts.ToArray();
			mesh.normals = newnormals.ToArray();
			if (hasuv){mesh.uv = newuvs.ToArray();}
			if (hasuv2){mesh.uv2 = newuv2s.ToArray();}
#if UNITY_5
			if (hasuv3){mesh.uv3 = newuv3s.ToArray();}
			if (hasuv4){mesh.uv4 = newuv4s.ToArray();}
#endif
			if (hastangents){mesh.tangents = newtangents.ToArray();}
			if (hasboneweights){mesh.boneWeights = newboneweights.ToArray();}
			mesh.bindposes = sourcebindposes;
			// RECONSTRUCT TRIANGLE SUBMESHES
			mesh.subMeshCount = sourcesubmeshes.Length;
			int pa = 0;
			for (int i=0; i<sourcesubmeshes.Length; i++) {
				mesh.SetTriangles(tris.Skip (pa).Take(sourcesubmeshes[i]).ToArray(), i);
				pa += sourcesubmeshes[i];
			}
		}
		/// <summary>
		/// Flips the vertices of the Mesh over the given axis
		/// </summary>
		public static void MirrorAxis (Mesh mesh, string axis)
		{
			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;
			for (int i = 0; i < vertices.Length; i++) {
				if (axis == "x") {
					vertices [i].x = -vertices [i].x;
					normals [i].x = -normals [i].x;
				} else if (axis == "y") {
					vertices [i].y = -vertices [i].y;
					normals [i].y = -normals [i].y;
				} else if (axis == "z") {
					vertices [i].z = -vertices [i].z;
					normals [i].z = -normals [i].z;
				}
			}
			mesh.vertices = vertices;
			mesh.normals = normals;
			TriangleTools.Flip (mesh);
			mesh.RecalculateBounds ();
		}
		/// <summary>
		/// Remove the vertices at given indices of a given mesh
		/// </summary>
		public static void Remove(Mesh mesh, int[] indices){
			if (indices != null && indices.Length > 0) {
				// CHECK WHAT DATA WILL BE COPIED
				bool hasuv = (mesh.uv.Length > 0);
				bool hasuv2 = (mesh.uv2.Length > 0);
#if UNITY_5
				bool hasuv3 = (mesh.uv3.Length > 0);
				bool hasuv4 = (mesh.uv4.Length > 0);
#endif
				bool hascolors = (mesh.colors.Length > 0);
				bool hastangents = (mesh.tangents.Length > 0);
				bool hasboneweights = (mesh.boneWeights.Length > 0);
				// CACHE THE ORIGINAL MESH DATA
				int[] sourcesubmeshes = new int[mesh.subMeshCount];
				for (int i=0; i<mesh.subMeshCount; i++) {
					sourcesubmeshes[i] = mesh.GetTriangles(i).Length;
				}
				// ----------------------------------------------------------
				// REMOVE THE DEPENDANT TRIANGLES
				int[] tris = mesh.triangles;
				List<int> triindices = new List<int> ();
				for (int i=0; i<tris.Length; i+=3) {
					if(System.Array.Exists(indices, p => p==tris[i] || p==tris[i+1] || p==tris[i+2])){
						triindices.Add (i / 3);
					}
				}
				TriangleTools.Remove (mesh, triindices.ToArray ());
				// REMOVE THE VERTICES BY INDEX
				int newCount = mesh.vertexCount - indices.Length;
				Vector3[] newverts = new Vector3[newCount];
				Vector2[] newuvs = new Vector2[newCount];
				Vector2[] newuv2s = new Vector2[newCount];
#if UNITY_5
				Vector2[] newuv3s = new Vector2[newCount];
				Vector2[] newuv4s = new Vector2[newCount];
#endif
				Color[] newcolors = new Color[newCount];
				Vector3[] newnormals = new Vector3[newCount];
				Vector4[] newtangents = new Vector4[newCount];
				BoneWeight[] newboneweights = new BoneWeight[newCount];
				tris = mesh.triangles;
				int j = 0;
				int previousdeletions = 0;
				for (int i=0; i<mesh.vertexCount; i+=1) {
					if (!System.Array.Exists (indices, p => p==i)) {
						newverts [j] = mesh.vertices [i];
						if (hasuv){newuvs [j] = mesh.uv [i];}
						if (hasuv2){newuv2s [j] = mesh.uv2 [i];}
#if UNITY_5
						if (hasuv3){newuv3s [j] = mesh.uv3 [i];}
						if (hasuv4){newuv4s [j] = mesh.uv4 [i];}
#endif
						if (hascolors) {newcolors[j] = mesh.colors[i];}
						if (hastangents){newtangents[j] = mesh.tangents[i];}
						if (hasboneweights){newboneweights[j] = mesh.boneWeights[i];}
						newnormals [j] = mesh.normals [i];
						j += 1;
						if (previousdeletions > 0) {
							while (System.Array.Exists(tris, p => p==i)) {
								tris [System.Array.FindIndex (tris, p => p == i)] -= previousdeletions;
							}
						}
					} else {
						previousdeletions += 1;
					}
				}
				// ---------------------------------------------------------------
				// PACK THE NEW DATA BACK INTO THE MESH
				mesh.Clear();
				Matrix4x4[] bindposes = mesh.bindposes; // Bind poses should remain the same
				mesh.vertices = newverts;
				mesh.normals = newnormals;
				//if (hasuv){mesh.uv = newuvs;}
				if (hasuv2){mesh.uv2 = newuv2s;}
#if UNITY_5
				if (hasuv3){mesh.uv3 = newuv3s;}
				if (hasuv4){mesh.uv4 = newuv4s;}
#endif
				if (hastangents){mesh.tangents = newtangents;}
				if (hasboneweights){mesh.boneWeights = newboneweights;}
				mesh.bindposes = bindposes;
				// RECONSTRUCT TRIANGLE SUBMESHES
				mesh.subMeshCount = sourcesubmeshes.Length;
				int pa = 0;
				for (int i=0; i<sourcesubmeshes.Length; i++) {
					mesh.SetTriangles(tris.Skip (pa).Take(sourcesubmeshes[i]).ToArray(), i);
					pa += sourcesubmeshes[i];
				}
			}
		}
		/// <summary>
		/// Removes the vertices that are not in any triangles.
		/// </summary>
		/// <param name='mesh'>
		/// Mesh to remove unused vertices from
		/// </param>
		public static void RemoveUnused (Mesh mesh){
			List<int> indices = new List<int>();
			for (int i=0;i<mesh.vertexCount;i++){
				if (!Array.Exists(mesh.triangles, p => p==i)){
					indices.Add (i);
				}
			}
			VertexTools.Remove(mesh, indices.ToArray());
		}
		/// <summary>
		/// Creates a unique vertex for every index in the original Mesh
		/// </summary>
		public static void SplitShared(Mesh mesh){
			// CHECK WHAT DATA WILL BE COPIED
			bool hasuv = (mesh.uv.Length > 0);
			bool hasuv2 = (mesh.uv2.Length > 0);
#if UNITY_5
			bool hasuv3 = (mesh.uv3.Length > 0);
			bool hasuv4 = (mesh.uv4.Length > 0);
#endif
			bool hascolors = (mesh.colors.Length > 0);
			bool hastangents = (mesh.tangents.Length > 0);
			bool hasboneweights = (mesh.boneWeights.Length > 0);
			// ----------------------------------------------------------
			// CACHE THE ORIGINAL MESH DATA IN AN ARRAY
			int[] sourceindices = mesh.triangles;
			Matrix4x4[] sourcebindposes = mesh.bindposes;
			int[] sourcesubmeshes = new int[mesh.subMeshCount];
			for (int i=0; i<mesh.subMeshCount; i++) {
				sourcesubmeshes[i] = mesh.GetTriangles(i).Length;
			}
			// CREATE ARRAYS FOR NEW DATA
			int newCount = mesh.triangles.Length;
			int[] newtris = new int[newCount];
			Vector3[] newverts = new Vector3[newCount];
			Vector2[] newuvs = new Vector2[newCount];
			Vector2[] newuv2s = new Vector2[newCount];
#if UNITY_5
			Vector2[] newuv3s = new Vector2[newCount];
			Vector2[] newuv4s = new Vector2[newCount];
#endif
			Vector3[] newnormals = new Vector3[newCount];
			Vector4[] newtangents = new Vector4[newCount];
			Color[] newcolors = new Color[newCount];
			BoneWeight[] newboneweights = new BoneWeight[newCount];
			// SPLIT THE SHARED VERTICES
			for(int i = 0; i < sourceindices.Length; i++)
			{
				newtris[i] = i;
				int vertexindex = sourceindices[i];
				newverts[i] = mesh.vertices[vertexindex];
				if (hasuv){newuvs[i] = mesh.uv[vertexindex];}
				if (hasuv2){newuv2s[i] = mesh.uv2[vertexindex];}
#if UNITY_5
				if (hasuv3){newuv3s[i] = mesh.uv3[vertexindex];}
				if (hasuv4){newuv4s[i] = mesh.uv4[vertexindex];}
#endif
				newnormals[i] = mesh.normals[vertexindex];
				if (hascolors){newcolors[i] = mesh.colors[vertexindex];}
				if (hastangents){newtangents[i] = mesh.tangents[vertexindex];}
				if (hasboneweights){newboneweights[i] = mesh.boneWeights[vertexindex];}
			}
			// ---------------------------------------------------------------
			// PACK THE NEW DATA BACK INTO THE MESH
			mesh.Clear ();
			mesh.vertices = newverts;
			mesh.normals = newnormals;
			if (hasuv){mesh.uv = newuvs;}
			if (hasuv2){mesh.uv2 = newuv2s;}
#if UNITY_5
			if (hasuv3){mesh.uv3 = newuv3s;}
			if (hasuv4){mesh.uv4 = newuv4s;}
#endif
			if (hastangents){mesh.tangents = newtangents;}
			if (hasboneweights){mesh.boneWeights = newboneweights;}
			mesh.bindposes = sourcebindposes;
			// RECONSTRUCT TRIANGLE SUBMESHES
			mesh.subMeshCount = sourcesubmeshes.Length;
			int pa = 0;
			for (int i=0; i<sourcesubmeshes.Length; i++) {
				mesh.SetTriangles(newtris.Skip (pa).Take(sourcesubmeshes[i]).ToArray(), i);
				pa += sourcesubmeshes[i];
			}
		}
		/// <summary>
		/// Weld the vertices at specified indices of a given mesh
		/// </summary>
		public static void Weld(Mesh mesh, int[] indices){
			// return early if there are not enough vertices
			if (indices.Length <= 1)
				return;
			// CHECK WHAT DATA WILL BE COPIED
			//bool hasuv = (mesh.uv.Length > 0);
			bool hasuv2 = (mesh.uv2.Length > 0);
#if UNITY_5
			bool hasuv3 = (mesh.uv3.Length > 0);
			bool hasuv4 = (mesh.uv4.Length > 0);
#endif
			bool hascolors = (mesh.colors.Length > 0);
			//bool hastangents = (mesh.tangents.Length > 0);
			//bool hasboneweights = (mesh.boneWeights.Length > 0);
			// CREATE ARRAYS FOR NEW DATA
			Vector3[] newverts = mesh.vertices;
			Vector3[] newnormals = mesh.normals;
			Vector2[] newuvs = mesh.uv;
			Vector2[] newuv2s = mesh.uv2;
#if UNITY_5
			Vector2[] newuv3s = mesh.uv3;
			Vector2[] newuv4s = mesh.uv4;
#endif
			Color[] newcolors = mesh.colors;
			//Vector4[] newtangents = mesh.tangents;
			//BoneWeight[] newboneweights = mesh.boneWeights;
			// CALCULATE THE WELDED VERTEX DATA
			Vector3 averagePos = Vector3.zero;
			Vector2 averageUV = Vector2.zero;
			Vector2 averageUV2 = Vector2.zero;
#if UNITY_5
			Vector2 averageUV3 = Vector2.zero;
			Vector2 averageUV4 = Vector2.zero;
#endif
			Vector3 averageNormal = Vector3.zero;
			Color averageColor = Color.black;
			foreach (int index in indices){
				averagePos += newverts[index];
				averageNormal += newnormals[index];
				averageUV += newuvs[index];
				if(hasuv2){averageUV2 += newuv2s[index];}
#if UNITY_5
				if(hasuv3){averageUV3 += newuv3s[index];}
				if(hasuv4){averageUV4 += newuv4s[index];}
#endif
				if(hascolors){averageColor += newcolors[index];}
			}
			averagePos = averagePos/indices.Length;
			averageNormal = averageNormal/indices.Length;
			averageUV = averageUV/indices.Length;
			if(hasuv2){averageUV2 = averageUV2/indices.Length;}
#if UNITY_5
			if(hasuv3){averageUV3 = averageUV3/indices.Length;}
			if(hasuv4){averageUV4 = averageUV4/indices.Length;}
#endif
			if(hascolors){averageColor = averageColor/indices.Length;}
			// PACK THE NEW DATA BACK INTO THE MESH
			foreach (int index in indices){
				newverts[index] = averagePos;
				newnormals[index] = averageNormal;
				newuvs[index] = averageUV;
				if(hasuv2){newuv2s[index] = averageUV2;}
#if UNITY_5
				if(hasuv3){newuv3s[index] = averageUV3;}
				if(hasuv4){newuv4s[index] = averageUV4;}
#endif
				if(hascolors){newcolors[index] = averageColor;}
			}
			mesh.vertices = newverts;
			mesh.normals = newnormals;
			mesh.uv = newuvs;
			if(hasuv2){mesh.uv2 = newuv2s;}
#if UNITY_5
			if(hasuv3){mesh.uv3 = newuv3s;}
			if(hasuv4){mesh.uv4 = newuv4s;}
#endif
			if(hascolors){mesh.colors = newcolors;}
			mesh.RecalculateBounds();
		}
	}
}
