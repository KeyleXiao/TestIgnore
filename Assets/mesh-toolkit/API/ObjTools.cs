using UnityEngine;
using System.IO;
using System.Text;

namespace MeshTK
{
	/// <summary>
	/// Export Tools - By EJM Software
	/// Export Mesh Objects to an OBJ file
	/// </summary>
	public class ObjTools
	{
		public static string ExportToString (Mesh mesh, Renderer renderer, bool uselhcoords = true, bool separateSubmeshes = true){
			Material[] mats = renderer.sharedMaterials;
			// Initiation
			StringBuilder sb = new StringBuilder();
			//Header
			sb.Append("o ").Append("Plane").Append("\n");
			foreach(Vector3 v in mesh.vertices) {
				sb.Append(string.Format("v {0:0.000000} {1:0.000000} {2:0.000000}\n",(uselhcoords?-v.x:v.x),v.y,v.z));
			}
			sb.Append("\n");
			foreach(Vector3 v in mesh.normals) {
				sb.Append(string.Format("vn {0:0.000000} {1:0.000000} {2:0.000000}\n",v.x,v.y,v.z));
			}
			sb.Append("\n");
			foreach(Vector3 v in mesh.uv) {
				sb.Append(string.Format("vt {0:0.000000} {1:0.000000}\n",v.x,v.y));
			}
			for (int material=0; material < mesh.subMeshCount; material ++) {
				sb.Append("\n");
				if (separateSubmeshes){
					sb.Append("g ").Append(mats[material].name).Append("\n");
				}
				sb.Append("usemtl ").Append(mats[material].name).Append("\n");
				sb.Append("usemap ").Append(mats[material].name).Append("\n");
				
				int[] triangles = mesh.GetTriangles(material);
				for (int i=0;i<triangles.Length;i+=3) {
					sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
					                        triangles[(uselhcoords?i+1:i)]+1, triangles[(uselhcoords?i:i+1)]+1, triangles[i+2]+1));
				}
			}
			return sb.ToString();
		}

		public static void ExportToFile(Mesh mesh, Renderer renderer, string filename, bool uselhcoords = true) {
			if (string.IsNullOrEmpty (filename))
				return;
			using (StreamWriter sw = new StreamWriter(filename)) 
			{
				sw.Write(ExportToString(mesh, renderer, uselhcoords));
			}
		}
	}
	public class ExportTools
	{
		public static string MeshToObjString (Mesh mesh, Renderer renderer, bool uselhcoords = true, bool separateSubmeshes = true){
			Debug.LogWarning ("WARNING: 'MeshTK.ExportTools.MeshToObjString()' is obsolete. Use MeshTK.ObjTools.ExportToString().");
			return ObjTools.ExportToString (mesh, renderer, uselhcoords, separateSubmeshes);
		}
		public static void MeshToFile(Mesh mesh, Renderer renderer, string filename, bool uselhcoords = true) {
			Debug.LogWarning ("WARNING: 'MeshTK.ExportTools.MeshToFile()' is obsolete. Use MeshTK.ObjTools.ExportToFile().");
			ObjTools.ExportToFile (mesh, renderer, filename, uselhcoords);
		}
	}
}