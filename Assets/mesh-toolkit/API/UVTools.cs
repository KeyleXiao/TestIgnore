using UnityEngine;
using System.Collections;

namespace MeshTK
{
	/// <summary>
	/// UV Tools - Copyright EJM Software 2015
	/// Class for editing the UVs of a Mesh
	/// </summary>
	public class UVTools
	{
		/// <summary>
		/// Rotates the UVs of a mesh by a given rotation
		/// </summary>
		public static void Rotate(Mesh mesh, float rot){		
			Vector2[] uvs = mesh.uv;
			Quaternion eulerRot = Quaternion.Euler(0,0,rot);
			for (var i = 0 ; i < uvs.Length; i++){
				uvs[i] = eulerRot * (uvs[i] - new Vector2(0.5f,0.5f)) + new Vector3(0.5f, 0.5f);
			}
			mesh.uv = uvs;
		}
		/// <summary>
		/// Export the UVs
		/// </summary>
		/// <param name='mesh'>
		/// Mesh to export UVs
		/// </param>
		/// <param name='filepath'>
		/// Filepath to save exported map (png)
		/// </param>
		/// <param name='w'>
		/// Pixel width of exported image
		/// </param>
		/// <param name='h'>
		/// Pixel height of exported image
		/// </param>
		public static void Export(Mesh mesh, string filepath, int w = 1024, int h = 1024){
			Texture2D tex = new Texture2D(w, h);
			int[] tris = mesh.triangles;
			for (int i=0;i<tris.Length;i+=3){
				Vector2 pt1 = mesh.uv[tris[i]];
				Vector2 pt2 = mesh.uv[tris[i+1]];
				Vector2 pt3 = mesh.uv[tris[i+2]];
				TextureTools.DrawLine(tex, (int)(pt1.x*w), (int)(pt1.y*h), (int)(pt2.x*w), (int)(pt2.y*h), Color.red);
				TextureTools.DrawLine(tex, (int)(pt2.x*w), (int)(pt2.y*h), (int)(pt3.x*w), (int)(pt3.y*h), Color.red);
				TextureTools.DrawLine(tex, (int)(pt3.x*w), (int)(pt3.y*h), (int)(pt1.x*w), (int)(pt1.y*h), Color.red);
			}
			tex.Apply();
			TextureTools.SaveTexture(tex, filepath);
		}
	}
}