using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

namespace MeshTK
{
	/// <summary>
	/// Primitives Tools - Copyright EJM Software 2015
	/// Generate primitive mesh objects procedurally
	/// </summary>
	public class PrimitivesTools
	{
		public static Mesh CreateCircle(float radius, int sides){
			List<Vector3> verts = new List<Vector3> ();
			List<Vector2> uvs = new List<Vector2> ();
			List<int> tris = new List<int> ();
			// Generate mesh data
			verts.Add (new Vector3 (0f, 0f, 0f));
			uvs.Add (new Vector2(0.5f, 0.5f));
			verts.Add (new Vector3 (radius, 0f, 0f));
			uvs.Add (new Vector2(1f, 0.5f));
			for (int i=1; i<=sides; i++) {
				float radians = i/(float)sides * 2f * Mathf.PI;
				verts.Add (new Vector3(Mathf.Cos (radians) * radius, 0f, Mathf.Sin (radians) * radius));
				uvs.Add(new Vector2(0.5f+Mathf.Cos (radians)/2, 0.5f+Mathf.Sin(radians)/2));
				tris.AddRange(new int[3]{0, (i==sides?1:i+1), i});
			}
			// Build and return the mesh 
			Mesh m = new Mesh ();
			m.vertices = verts.ToArray ();
			m.uv = uvs.ToArray();
			m.triangles = tris.ToArray ();
			m.RecalculateBounds ();
			m.RecalculateNormals ();
			return m;
		}
		public static Mesh CreatePlane(float width, float height){
			List<Vector3> verts = new List<Vector3> ();
			List<Vector2> uvs = new List<Vector2> ();
			List<int> tris = new List<int> ();
			// Generate mesh data
			verts.Add (new Vector3(-width/2f, 0f, -height/2f));
			verts.Add (new Vector3(-width/2f, 0f, height/2f));
			verts.Add (new Vector3(width/2f, 0f, height/2f));
			verts.Add (new Vector3(width/2f, 0f, -height/2f));
			uvs.AddRange(new Vector2[4]{new Vector2(0f,0f), new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(1f,0f)});
			tris.AddRange (new int[6]{0, 1, 2, 0, 2, 3});
			// Build and return the mesh
			Mesh m = new Mesh ();
			m.vertices = verts.ToArray ();
			m.uv = uvs.ToArray();
			m.triangles = tris.ToArray ();
			m.RecalculateBounds ();
			m.RecalculateNormals ();
			return m;
		}

		public static Mesh CreateFromImage (Texture2D outlineTex, float maxDeviance, float meshWidth, float meshHeight, Vector3 pivotOffset){
			if (outlineTex == null) {
				return new Mesh();
			}
			float pixelTransparencyThreshold = 200f;
#if UNITY_EDITOR
			// Make sure the texture is readable
			string path = AssetDatabase.GetAssetPath(outlineTex);
			TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
			textureImporter.isReadable = true;
			AssetDatabase.ImportAsset(path);
#endif
			// Load pixels from the texture
			Color[] pixels = outlineTex.GetPixels(); // get the pixels to build the mesh from
			// Process the pixels
			ImageProcessor mcs = new ImageProcessor(pixels, outlineTex.width, outlineTex.height, pixelTransparencyThreshold/255.0f);
			mcs.SimplifyEdges (maxDeviance);
			// Create the mesh
			ArrayList allVertexLoops = mcs.GetAllPoints();
			
			ArrayList completeVertices = new ArrayList();
			ArrayList completeIndices = new ArrayList();
			ArrayList completeUVs = new ArrayList();
			int verticesOffset = 0;
			int indicesOffset = 0;
			int loopCount = 0;
			foreach (Vector2[] vertices2D in allVertexLoops) {
				// Build triangles from the loops of vertices
				int[] indices = new Triangulator(vertices2D).Triangulate();
				Vector2[] uvs = new Vector2[vertices2D.Length];
				Vector3[] vertices = new Vector3[vertices2D.Length];

				for (int i=0; i<vertices2D.Length; i++) {
					// Calculate normalized X and Y points
					float vertX = 1.0f - (vertices2D[i].x/outlineTex.width) - 0.5f/outlineTex.width;
					float vertY = vertices2D[i].y/outlineTex.height + 0.5f/outlineTex.height;
					// Center and scale the X and Y points
					vertX = (vertX * meshWidth) - (meshWidth / 2.0f);
					vertY = (vertY * meshHeight) - (meshHeight / 2.0f);

					vertices[i] = new Vector3(-vertX, 0f, vertY) - pivotOffset;
					uvs[i] = mcs.GetUVAtIndex(loopCount, i);
				}
				//Add data to complete arrays
				completeVertices.AddRange(vertices);
				completeUVs.AddRange(uvs);
				for (int i = 0; i < indices.Length; i++) {
					completeIndices.Add(indices[i] + verticesOffset);
				}
				
				verticesOffset += vertices.Length;
				indicesOffset += indices.Length;
				loopCount++;
			}
			// Create the mesh to return
			Mesh m = new Mesh ();
			m.vertices = (Vector3[]) completeVertices.ToArray(typeof(Vector3));
			m.triangles = (int[]) completeIndices.ToArray(typeof(int));
			m.uv = (Vector2[]) completeUVs.ToArray(typeof(Vector2));
			m.RecalculateNormals();
			m.RecalculateBounds();
			return m;
		}
	}
}