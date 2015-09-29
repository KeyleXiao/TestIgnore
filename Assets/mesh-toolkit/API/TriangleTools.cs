using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MeshTK
{
	/// <summary>
	/// Triangle Tools - Copyright EJM Software 2015
	/// Edit the triangles of a mesh object
	/// </summary>
	public class TriangleTools
	{
		public static GameObject DetachToNewObject(ref EJMMesh ejmmesh, HashSet<int> indices){
			// Create new Gameobject
			GameObject obj = new GameObject ();
			MeshFilter filter = obj.AddComponent<MeshFilter> ();
			filter.sharedMesh = new Mesh ();
			obj.AddComponent<MeshRenderer> ();
			EJMMesh newejmmesh = new EJMMesh (filter);
			// Import data from ejmmesh
			newejmmesh.ImportData (ejmmesh.SharedMesh);
			//Remove the old triangles
			int submeshStartIndex = 0;
			for (int i=0; i<ejmmesh.SharedMesh.subMeshCount; i++) {
				List<int> tris = ejmmesh.SharedMesh.GetTriangles(i).ToList();
				int trisLength = tris.Count;
				List<int> removedtris = new List<int>();
				for (int j=submeshStartIndex+trisLength-3;j>=submeshStartIndex;j-=3){
					if (indices.Contains(j/3)){
						removedtris.AddRange(tris.GetRange(j-submeshStartIndex, 3));
						tris.RemoveRange(j-submeshStartIndex, 3);
					}
				}
				ejmmesh.SharedMesh.SetTriangles(tris.ToArray(), i);
				newejmmesh.SharedMesh.SetTriangles(removedtris.ToArray(), i);
				submeshStartIndex += trisLength;
			}
			return obj;
		}
		public static void Detach(Mesh mesh, int[] indices, bool newSubmesh = false){
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
			int ind = mesh.vertexCount;
			int[] tris = mesh.triangles;
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
			List<int> newtris = new List<int> ();
			// DETACH THE TRIANGLES
			foreach(int i in indices){
				for (int j=0;j<3;j++){
					int vertindex = tris[i*3+j];
					Vector3 vert = mesh.vertices[vertindex];
					int newindex = newverts.FindIndex(p=>Vector3.Distance (vert, p)<=0.001f);

					if(newindex == -1){
						newverts.Add (vert);
						newnormals.Add(mesh.normals[vertindex]);
						if (hasuv){newuvs.Add (mesh.uv[vertindex]);}
						if (hasuv2){newuv2s.Add (mesh.uv2[vertindex]);}
#if UNITY_5
						if (hasuv3){newuv3s.Add (mesh.uv3[vertindex]);}
						if (hasuv4){newuv4s.Add (mesh.uv4[vertindex]);}
#endif
						if (hascolors){newcolors.Add (mesh.colors[vertindex]);}
						if (hastangents){newtangents.Add (mesh.tangents[vertindex]);}
						if (hasboneweights){newboneweights.Add (mesh.boneWeights[vertindex]);}
						newindex = newverts.Count - 1;
					}
					tris[i*3+j] = ind + newindex;
					if (newSubmesh){newtris.Add (ind + newindex);}
				}
			}
			// BUILD UPDATED MESH DATA
			int newcount = mesh.vertexCount + newverts.Count;
			Vector3[] verts = new Vector3[newcount];
			mesh.vertices.CopyTo (verts, 0);
			newverts.ToArray().CopyTo(verts, ind);
			mesh.vertices = verts;
			Vector3[] normals = new Vector3[newcount];
			mesh.normals.CopyTo (normals, 0);
			newnormals.ToArray().CopyTo(normals, ind);
			mesh.normals = normals;
			if (hasuv) {
				Vector2[] uvs = new Vector2[newcount];
				mesh.uv.CopyTo (uvs, 0);
				newuvs.ToArray().CopyTo(uvs, ind);
				mesh.uv = uvs;
			}
			if (hasuv2) {
				Vector2[] uv2s = new Vector2[newcount];
				mesh.uv2.CopyTo (uv2s, 0);
				newuv2s.ToArray().CopyTo(uv2s, ind);
				mesh.uv2 = uv2s;
			}
#if UNITY_5
			if (hasuv3) {
				Vector2[] uv3s = new Vector2[newcount];
				mesh.uv3.CopyTo (uv3s, 0);
				newuv3s.ToArray().CopyTo(uv3s, ind);
				mesh.uv3 = uv3s;
			}
			if (hasuv4) {
				Vector2[] uv4s = new Vector2[newcount];
				mesh.uv4.CopyTo (uv4s, 0);
				newuv4s.ToArray().CopyTo(uv4s, ind);
				mesh.uv4 = uv4s;
			}
#endif
			if (hascolors) {
				Color[] colors = new Color[newcount];
				mesh.colors.CopyTo (colors, 0);
				newcolors.ToArray().CopyTo(colors, ind);
				mesh.colors = colors;
			}
			if (hastangents){
				Vector4[] tangents = new Vector4[newcount];
				mesh.tangents.CopyTo(tangents, 0);
				newtangents.ToArray().CopyTo(tangents, ind);
				mesh.tangents = tangents;
			}
			if (hasboneweights){
				BoneWeight[] boneweights = new BoneWeight[newcount];
				mesh.boneWeights.CopyTo(boneweights, 0);
				newboneweights.ToArray().CopyTo(boneweights, ind);
				mesh.boneWeights = boneweights;
			}
			// Pack tris back into their submeshes
			int pa = 0;
			for (int i=0; i<mesh.subMeshCount; i++) {
				int l = mesh.GetTriangles(i).Length;
				mesh.SetTriangles(tris.Skip (pa).Take(l).ToArray(), i);
				pa+=l;
			}
			// Create new submesh if necessary
			if (newSubmesh) {
				//Remove the original triangles
				Remove (mesh, indices);
				// Add the triangles to a new submesh instead
				mesh.subMeshCount += 1;
				mesh.SetTriangles(newtris.ToArray(), mesh.subMeshCount-1);
			}
		}
		/// <summary>
		/// Extrude the triangles of a specified Mesh
		/// </summary>
		/// <param name='mesh'>
		/// Specified Mesh
		/// </param>
		/// <param name='indices'>
		/// Indices of triangles to extrude (ie. Index 2 represents the triangle with the vertex indices 6,7,8)
		/// </param>
		/// <param name='dist'>
		/// Distance to extrude each triangle
		/// </param>
		public static void Extrude(Mesh mesh, int[] indices, float dist){
			// DETERMINE WHAT DATA WILL BE COPIED
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
			int[] sourcetris = mesh.triangles;
			Matrix4x4[] sourcebindposes = mesh.bindposes;
			// CREATE ARRAYS FOR NEW DATA
			int[] newtris = new int[sourcetris.Length + 18 * indices.Length];
			Vector3[] newverts = mesh.vertices;
			Vector2[] newuvs = mesh.uv;
			Vector2[] newuv2s = mesh.uv2;
#if UNITY_5
			Vector2[] newuv3s = mesh.uv3;
			Vector2[] newuv4s = mesh.uv4;
#endif
			Color[] newcolors = mesh.colors;
			Vector3[] normals = mesh.normals;
			Vector4[] newtangents = mesh.tangents;
			BoneWeight[] newboneweights = mesh.boneWeights;
			// EXTRUDE THE TRIANGLES
			int j = 0;
			for (int i=0; i<sourcetris.Length; i+=3) {
				if (Array.IndexOf (indices, i / 3) == -1) {
					newtris [j++] = sourcetris [i];
					newtris [j++] = sourcetris [i + 1];
					newtris [j++] = sourcetris [i + 2];
				} else {
					// calculate the direction of the extrusion
					Vector3 direction = Vector3.Cross(newverts[sourcetris[i+1]]-newverts[sourcetris[i]], newverts[sourcetris[i+2]]-newverts[sourcetris[i]]).normalized;
					// resize all the new data arrays
					System.Array.Resize (ref newverts, newverts.Length + 15);
					System.Array.Resize (ref normals, normals.Length + 15);
					System.Array.Resize (ref newuvs, newuvs.Length + 15);
					if (hasuv2){System.Array.Resize (ref newuv2s, newuv2s.Length + 15);}
#if UNITY_5
					if (hasuv3){System.Array.Resize (ref newuv3s, newuv3s.Length + 15);}
					if (hasuv4){System.Array.Resize (ref newuv4s, newuv4s.Length + 15);}
#endif
					if (hascolors){System.Array.Resize (ref newcolors, newcolors.Length + 15);}
					if (hastangents){System.Array.Resize (ref newtangents, newtangents.Length + 15);}
					if (hasboneweights){System.Array.Resize (ref newboneweights, newboneweights.Length + 15);}
					// create the new data
					for (int k=0;k<3;k++){
						// Calculate indices for the 4 new vertices
						int i0 = newverts.Length-15+k*4;
						int i1 = newverts.Length-14+k*4;
						int i2 = newverts.Length-13+k*4;
						int i3 = newverts.Length-12+k*4;
						// Create vertices and all vertex data
						newverts[i0] = newverts [sourcetris [i+k]];
						newverts[i1] = newverts [sourcetris [k==2?i:i+k+1]];
						newverts[i2] = newverts [sourcetris [k==2?i:i+k+1]] + dist * direction;
						newverts[i3] = newverts [sourcetris [i+k]] + dist * direction;
						Vector3 norm = Vector3.Cross(newverts[i1]-newverts[i0], newverts[i2]-newverts[i0]).normalized;
						normals [i0] = norm;
						normals [i1] = norm;
						normals [i2] = norm;
						normals [i3] = norm;
						newuvs [i0] = newuvs [sourcetris [i+k]];
						newuvs [i1] = newuvs [sourcetris [k==2?i:i+k+1]];
						newuvs [i2] = newuvs [sourcetris [k==2?i:i+k+1]];
						newuvs [i3] = newuvs [sourcetris [i+k]];
						if (hasuv2){
							newuv2s [i0] = newuv2s [sourcetris [i+k]];
							newuv2s [i1] = newuv2s [sourcetris [k==2?i:i+k+1]];
							newuv2s [i2] = newuv2s [sourcetris [k==2?i:i+k+1]];
							newuv2s [i3] = newuv2s [sourcetris [i+k]];
						}
#if UNITY_5
						if (hasuv3){
							newuv3s [i0] = newuv3s [sourcetris [i+k]];
							newuv3s [i1] = newuv3s [sourcetris [k==2?i:i+k+1]];
							newuv3s [i2] = newuv3s [sourcetris [k==2?i:i+k+1]];
							newuv3s [i3] = newuv3s [sourcetris [i+k]];
						}
						if (hasuv4){
							newuv4s [i0] = newuv4s [sourcetris [i+k]];
							newuv4s [i1] = newuv4s [sourcetris [k==2?i:i+k+1]];
							newuv4s [i2] = newuv4s [sourcetris [k==2?i:i+k+1]];
							newuv4s [i3] = newuv4s [sourcetris [i+k]];
						}
#endif
						if (hascolors){
							newcolors [i0] = newcolors [sourcetris [i+k]];
							newcolors [i1] = newcolors [sourcetris [k==2?i:i+k+1]];
							newcolors [i2] = newcolors [sourcetris [k==2?i:i+k+1]];
							newcolors [i3] = newcolors [sourcetris [i+k]];
						}
						if (hastangents){
							newtangents [i0] = newtangents [sourcetris [i+k]];
							newtangents [i1] = newtangents [sourcetris [k==2?i:i+k+1]];
							newtangents [i2] = newtangents [sourcetris [k==2?i:i+k+1]];
							newtangents [i3] = newtangents [sourcetris [i+k]];
						}
						if (hasboneweights){
							newboneweights [i0] = newboneweights [sourcetris [i+k]];
							newboneweights [i1] = newboneweights [sourcetris [k==2?i:i+k+1]];
							newboneweights [i2] = newboneweights [sourcetris [k==2?i:i+k+1]];
							newboneweights [i3] = newboneweights [sourcetris [i+k]];
						}
						// Create triangles
						newtris [j++] = i0;
						newtris [j++] = i1;
						newtris [j++] = i2;
						newtris [j++] = i0;
						newtris [j++] = i2;
						newtris [j++] = i3;
					}
					//Create top face
					for (int k=0;k<3;k++){
						newverts[newverts.Length-3+k] = newverts [sourcetris [i+k]] + dist * direction;
						normals[newverts.Length-3+k] = normals [sourcetris [i+k]];
						newuvs [newverts.Length-3+k] = newuvs [sourcetris [i+k]];
						if (hasuv2){newuv2s [newverts.Length-3+k] = newuv2s [sourcetris [i+k]];}
#if UNITY_5
						if (hasuv3){newuv3s [newverts.Length-3+k] = newuv3s [sourcetris [i+k]];}
						if (hasuv4){newuv4s [newverts.Length-3+k] = newuv4s [sourcetris [i+k]];}
#endif
						if (hascolors){newcolors [newverts.Length-3+k] = newcolors [sourcetris [i+k]];}
						if (hastangents){newtangents [newverts.Length-3+k] = newtangents [sourcetris [i+k]];}
						if (hasboneweights){newboneweights [newverts.Length-3+k] = newboneweights [sourcetris [i+k]];}
					}
					newtris [j++] = newverts.Length - 3;
					newtris [j++] = newverts.Length - 2;
					newtris [j++] = newverts.Length - 1;
				}
			}
			// PACK THE NEW DATA BACK INTO THE MESH
			mesh.vertices = newverts;
			mesh.normals = normals;
			if (hasuv){mesh.uv = newuvs;}
			if (hasuv2){mesh.uv2 = newuv2s;}
#if UNITY_5
			if (hasuv3){mesh.uv3 = newuv3s.ToArray();}
			if (hasuv4){mesh.uv4 = newuv4s.ToArray();}
#endif
			if (hascolors){mesh.colors = newcolors;}
			if (hastangents){mesh.tangents = newtangents;}
			if (hasboneweights){mesh.boneWeights = newboneweights;}
			mesh.bindposes = sourcebindposes;
			// PACK TRIANGLES BACK INTO THEIR SUBMESHES
			int originalindex = 0;
			int newind = 0;
			for (int i=0; i<mesh.subMeshCount; i++) {
				int originallength = mesh.GetTriangles(i).Length;
				int newlength = originallength + indices.Count (p=> p >= originalindex && p < originalindex + originallength) * 18; // original length + tris added (indices * 6 new tris * 3 ints per tri)
				mesh.SetTriangles(newtris.Skip (newind).Take(newlength).ToArray(), i);
				originalindex += originallength;
				newind += newlength;
			}
		}
		/// <summary>
		/// Flips triangles of given mesh at indices. If indices is null it flips all triangles
		/// </summary>
		public static void Flip(Mesh mesh, int[] indices = null){
			int[] tris = mesh.triangles;
			if (indices == null) {
				for (int i = 0; i < tris.Length; i += 3) {
					int intermediate = tris [i];
					tris [i] = tris [i + 2];
					tris [i + 2] = intermediate;
				}
			} else {
				foreach (int i in indices) {
					if(tris.Length >= i*3+3){
						int intermediate = tris [i * 3];
						tris [i * 3] = tris [i * 3 + 2];
						tris [i * 3 + 2] = intermediate;
					}
				}
			}
			// PACK TRIANGLES BACK INTO THEIR SUBMESHES
			int j = 0;
			for (int i=0; i<mesh.subMeshCount; i++) {
				int l = mesh.GetTriangles(i).Length;
				mesh.SetTriangles(tris.Skip (j).Take(l).ToArray(), i);
				j+=l;
			}
		}
		/// <summary>
		/// Remove the triangles at specific indices
		/// </summary>
		/// <param name='mesh'>
		/// Mesh
		/// </param>
		/// <param name='indices'>
		/// Indices of triangles that should be removed
		/// </param>
		public static void Remove(Mesh mesh, int[] indices){
			List<int> tris = mesh.triangles.ToList ();
			Array.Sort(indices, (a, b) => b.CompareTo(a));
			foreach (int i in indices){
				tris.RemoveRange (i*3,3);
			}
			// PACK TRIANGLES BACK INTO THEIR SUBMESHES
			int originalindex = 0;
			int newindex = 0;
			for (int i=0; i<mesh.subMeshCount; i++) {
				int originallength = mesh.GetTriangles(i).Length;
				int newlength = originallength - indices.Count (p=> p >= originalindex && p < originalindex + originallength) * 3;
				mesh.SetTriangles(tris.Skip (newindex).Take(newlength).ToArray(), i);
				originalindex += originallength;
				newindex += newlength;
			}
		}
		/// <summary>
		/// Subdivides the triangles of a mesh by their centers
		/// </summary>
		/// <param name='mesh'>
		/// Mesh
		/// </param>
		/// <param name='indices'>
		/// Indices of triangles to be subdivided
		/// </param>
		public static void SubdivideByCenter(Mesh mesh, int[] indices){
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
			int[] sourcetris = mesh.triangles;
			Matrix4x4[] sourcebindposes = mesh.bindposes;
			// CREATE ARRAYS FOR NEW DATA
			int[] newtris = new int[sourcetris.Length + 6 * indices.Length];
			Vector3[] newverts = mesh.vertices;
			Vector3[] newnormals = mesh.normals;
			Vector2[] newuvs = mesh.uv;
			Vector2[] newuv2s = mesh.uv2;
#if UNITY_5
			Vector2[] newuv3s = mesh.uv3;
			Vector2[] newuv4s = mesh.uv4;
#endif
			Color[] newcolors = mesh.colors;
			Vector4[] newtangents = mesh.tangents;
			BoneWeight[] newboneweights = mesh.boneWeights;
			// create the new vertices and triangles
			int j = 0;
			for (int i=0; i<sourcetris.Length; i+=3) {
				if (Array.IndexOf (indices, i / 3) == -1 && indices!=null) {
					newtris [j++] = sourcetris [i];
					newtris [j++] = sourcetris [i + 1];
					newtris [j++] = sourcetris [i + 2];
				} else {
					// resize all the data arrays
					System.Array.Resize (ref newverts, newverts.Length + 1);
					System.Array.Resize (ref newnormals, newnormals.Length + 1);
					System.Array.Resize (ref newuvs, newuvs.Length + 1);
					if(hasuv2){System.Array.Resize (ref newuv2s, newuv2s.Length + 1);}
#if UNITY_5
					if(hasuv3){System.Array.Resize (ref newuv3s, newuv3s.Length + 1);}
					if(hasuv4){System.Array.Resize (ref newuv4s, newuv4s.Length + 1);}
#endif
					if(hascolors){System.Array.Resize (ref newcolors, newcolors.Length + 1);}
					if(hastangents){System.Array.Resize (ref newtangents, newtangents.Length + 1);}
					if(hasboneweights){System.Array.Resize (ref newboneweights, newboneweights.Length + 1);}
					// create a new vertex in the center of the current triangle
					int sourceind0 = sourcetris [i];
					int sourceind1 = sourcetris [i + 1];
					int sourceind2 = sourcetris [i + 2];
					int newind = newverts.Length - 1;
					newverts [newind]   = (newverts   [sourceind0] + newverts   [sourceind1] + newverts   [sourceind2]) / 3;
					newnormals [newind] = (newnormals [sourceind0] + newnormals [sourceind1] + newnormals [sourceind2]) / 3;
					newuvs [newind]     = (newuvs     [sourceind0] + newuvs     [sourceind1] + newuvs     [sourceind2]) / 3;
					if(hasuv2){newuv2s [newind]     = (newuv2s [sourceind0]     + newuv2s [sourceind1]     + newuv2s [sourceind2])     / 3;}
#if UNITY_5
					if(hasuv3){newuv3s [newind]     = (newuv3s [sourceind0]     + newuv3s [sourceind1]     + newuv3s [sourceind2])     / 3;}
					if(hasuv4){newuv4s [newind]     = (newuv4s [sourceind0]     + newuv4s [sourceind1]     + newuv4s [sourceind2])     / 3;}
#endif
					if(hascolors){newcolors [newind]= (newcolors [sourceind0]   + newcolors [sourceind1]   + newcolors [sourceind2])   / 3;}
					if(hastangents){newtangents[newind] = (newtangents [sourceind0] + newtangents [sourceind1] + newtangents [sourceind2]) / 3;}
					if(hasboneweights){newboneweights[newind] = newboneweights[sourcetris[i]];}
					// use the new vertex to replace the old triangle with three new, smaller triangles
					newtris [j++] = sourceind0;
					newtris [j++] = sourceind1;
					newtris [j++] = newind;

					newtris [j++] = sourceind1;
					newtris [j++] = sourceind2;
					newtris [j++] = newind;

					newtris [j++] = sourceind2;
					newtris [j++] = sourceind0;
					newtris [j++] = newind;
				}
			}
			// PACK THE NEW DATA BACK INTO THE MESH
			mesh.vertices = newverts;
			mesh.normals = newnormals;
			if (hasuv){mesh.uv = newuvs;}
			if (hasuv2){mesh.uv2 = newuv2s;}
#if UNITY_5
			if (hasuv3){mesh.uv3 = newuv3s;}
			if (hasuv4){mesh.uv4 = newuv4s;}
#endif
			if (hascolors){mesh.colors = newcolors;}
			if (hastangents){mesh.tangents = newtangents;}
			if (hasboneweights){mesh.boneWeights = newboneweights;}
			mesh.bindposes = sourcebindposes;
			// PACK TRIANGLES BACK INTO THEIR SUBMESHES
			int originalindex = 0;
			int newindex = 0;
			for (int i=0; i<mesh.subMeshCount; i++) {
				int originallength = mesh.GetTriangles(i).Length;
				int newlength = originallength + indices.Count (p=> p >= originalindex && p < originalindex + originallength) * 6; // original length + tris added (indices * 2 new tris * 3 ints per tri)
				mesh.SetTriangles(newtris.Skip (newindex).Take(newlength).ToArray(), i);
				originalindex += originallength;
				newindex += newlength;
			}
		}
		/// <summary>
		/// Subdivides the triangles of a mesh by their edges
		/// </summary>
		/// <param name='mesh'>
		/// Mesh.
		/// </param>
		/// <param name='indices'>
		/// Indices of triangles to be subdivided
		/// </param>
		/// <param name='weld'>
		/// Should the subdivided triangles use existing vertices when available or create new ones on their edges?
		/// </param>
		public static void SubdivideByEdge(Mesh mesh, int[] indices, bool weld = true, float threshold=0.001f){
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
			int[] sourcetris = mesh.triangles;
			Matrix4x4[] sourcebindposes = mesh.bindposes;
			//Create arrays for new data
			int[] newtris = new int[sourcetris.Length + 9 * indices.Length];
			Vector3[] newverts = mesh.vertices;
			Vector3[] newnormals = mesh.normals;
			Vector2[] newuvs = mesh.uv;
			Vector2[] newuv2s = mesh.uv2;
#if UNITY_5
			Vector2[] newuv3s = mesh.uv3;
			Vector2[] newuv4s = mesh.uv4;
#endif
			Color[] newcolors = mesh.colors;
			Vector4[] newtangents = mesh.tangents;
			BoneWeight[] newboneweights = mesh.boneWeights;
			//Generate new newverts and triangles
			int j = 0;
			for (int i=0; i<sourcetris.Length; i+=3) {
				if (Array.IndexOf (indices, i / 3) == -1) {
					newtris [j++] = sourcetris [i];
					newtris [j++] = sourcetris [i + 1];
					newtris [j++] = sourcetris [i + 2];
				} else {
					// Get the indices from the original triangle
					int sourceind0 = sourcetris [i];
					int sourceind1 = sourcetris [i + 1];
					int sourceind2 = sourcetris [i + 2];
					// create 3 new vertices, one on the midpoint of each edge
					Vector3 vert1 = (newverts [sourcetris [i]] + newverts [sourcetris [i + 1]]) / 2;
					Vector3 vert2 = (newverts [sourcetris [i + 1]] + newverts [sourcetris [i + 2]]) / 2;
					Vector3 vert3 = (newverts [sourcetris [i + 2]] + newverts [sourcetris [i]]) / 2;
					// check if any of the 3 new vertices already exist in the array of vertices
					int p1index = System.Array.FindIndex(newverts, p => Vector3.Distance(p, vert1)<=threshold);
					int p2index = System.Array.FindIndex(newverts, p => Vector3.Distance(p, vert2)<=threshold);
					int p3index = System.Array.FindIndex(newverts, p => Vector3.Distance(p, vert3)<=threshold);
					// Resize the arrays
					int newlength = newverts.Length + (!weld ? 3 : (p1index==-1?1:0) + (p2index==-1?1:0) + (p3index==-1?1:0));
					System.Array.Resize (ref newverts, newlength);
					System.Array.Resize (ref newnormals, newlength);
					System.Array.Resize (ref newuvs, newlength);
					if(hasuv2){System.Array.Resize (ref newuv2s, newlength);}
#if UNITY_5
					if(hasuv3){System.Array.Resize (ref newuv3s, newlength);}
					if(hasuv4){System.Array.Resize (ref newuv4s, newlength);}
#endif
					if(hascolors){System.Array.Resize (ref newcolors, newlength);}
					if(hastangents){System.Array.Resize (ref newtangents, newlength);}
					if(hasboneweights){System.Array.Resize (ref newboneweights, newlength);}
					// if the first new vertex does not exist, or we are not welding the new verts to other verts, add the new vertex to the mesh
					if (!weld || p1index==-1){
						p1index = newverts.Length - 1 - (p2index==-1?1:0) - (p3index==-1?1:0);
						newverts[p1index] = vert1;
						newnormals[p1index] = (newnormals [sourceind0] + newnormals [sourceind1]) / 2;
						newuvs[p1index] = (newuvs [sourceind0] + newuvs [sourceind1]) / 2;
						if(hasuv2){newuv2s[p1index] = (newuv2s [sourceind0] + newuv2s [sourceind1]) / 2;}
#if UNITY_5
						if(hasuv3){newuv3s[p1index] = (newuv3s [sourceind0] + newuv3s [sourceind1]) / 2;}
						if(hasuv4){newuv4s[p1index] = (newuv4s [sourceind0] + newuv4s [sourceind1]) / 2;}
#endif
						if(hascolors){newcolors[p1index] = (newcolors [sourceind0] + newcolors [sourceind1]) / 2;}
						if(hastangents){newtangents[p1index] = (newtangents [sourceind0] + newtangents [sourceind1]) / 2;}
						if(hasboneweights){newboneweights[p1index] = newboneweights[sourceind0];}
					}
					// if the second new vertex does not exist, or we are not welding the new verts to other verts, add the new vertex to the mesh
					if (!weld || p2index==-1){
						p2index = newverts.Length - 1 - (p3index==-1?1:0);
						newverts[p2index] = vert2;
						newnormals[p2index] = (newnormals [sourceind1] + newnormals [sourceind2]) / 2;
						newuvs[p2index] = (newuvs [sourceind1] + newuvs [sourceind2]) / 2;
						if(hasuv2){newuv2s[p2index] = (newuv2s [sourceind1] + newuv2s [sourceind2]) / 2;}
#if UNITY_5
						if(hasuv3){newuv3s[p2index] = (newuv3s [sourceind1] + newuv3s [sourceind2]) / 2;}
						if(hasuv4){newuv4s[p2index] = (newuv4s [sourceind1] + newuv4s [sourceind2]) / 2;}
#endif
						if(hascolors){newcolors[p2index] = (newcolors [sourceind1] + newcolors [sourceind2]) / 2;}
						if(hastangents){newtangents[p2index] = (newtangents [sourceind1] + newtangents [sourceind2]) / 2;}
						if(hasboneweights){newboneweights[p2index] = newboneweights[sourceind1];}
					}
					// if the second new vertex does not exist, or we are not welding the new verts to other verts, add the new vertex to the mesh
					if (!weld || p3index==-1){
						p3index = newverts.Length - 1;
						newverts[p3index] = vert3;
						newnormals[p3index] = (newnormals [sourceind2] + newnormals [sourceind0]) / 2;
						newuvs[p3index] = (newuvs [sourceind2] + newuvs [sourceind0]) / 2;
						if(hasuv2){newuv2s[p3index] = (newuv2s [sourceind2] + newuv2s [sourceind0]) / 2;}
#if UNITY_5
						if(hasuv3){newuv3s[p3index] = (newuv3s [sourceind2] + newuv3s [sourceind0]) / 2;}
						if(hasuv4){newuv4s[p3index] = (newuv4s [sourceind2] + newuv4s [sourceind0]) / 2;}
#endif
						if(hascolors){newcolors[p3index] = (newcolors [sourceind2] + newcolors [sourceind0]) / 2;}
						if(hastangents){newtangents[p3index] = (newtangents [sourceind2] + newtangents [sourceind0]) / 2;}
						if(hasboneweights){newboneweights[p3index] = newboneweights[sourceind2];}
					}
					Debug.Log("1: " + p1index.ToString() + ", 2: " + p2index.ToString() + ", 3: " + p3index.ToString());
					// create 4 new triangles using the new vertices to replace the old triangle
					newtris [j++] = sourceind0;
					newtris [j++] = p1index;
					newtris [j++] = p3index;

					newtris [j++] = sourceind1;
					newtris [j++] = p2index;
					newtris [j++] = p1index;

					newtris [j++] = sourceind2;
					newtris [j++] = p3index;
					newtris [j++] = p2index;

					newtris [j++] = p1index;
					newtris [j++] = p2index;
					newtris [j++] = p3index;
				}
			}
			// PACK THE NEW DATA BACK INTO THE MESH
			mesh.vertices = newverts;
			mesh.normals = newnormals;
			if (hasuv){mesh.uv = newuvs;}
			if (hasuv2){mesh.uv2 = newuv2s;}
#if UNITY_5
			if (hasuv3){mesh.uv3 = newuv3s;}
			if (hasuv4){mesh.uv4 = newuv4s;}
#endif
			if (hascolors){mesh.colors = newcolors;}
			if (hastangents){mesh.tangents = newtangents;}
			if (hasboneweights){mesh.boneWeights = newboneweights;}
			mesh.bindposes = sourcebindposes;
			// PACK TRIANGLES BACK INTO THEIR SUBMESHES
			int originalindex = 0;
			int newindex = 0;
			for (int i=0; i<mesh.subMeshCount; i++) {
				int originallength = mesh.GetTriangles(i).Length;
				int newlength = originallength + indices.Count (p=> p >= originalindex && p < originalindex + originallength) * 3 * 3; // original length + tris added (indices * 3 new tris * 3 ints per tri)
				mesh.SetTriangles(newtris.Skip (newindex).Take(newlength).ToArray(), i);
				originalindex += originallength;
				newindex += newlength;
			}
		}
	}
}
