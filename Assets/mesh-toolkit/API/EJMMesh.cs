#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;

namespace MeshTK
{
public abstract class EJMMeshEditor
	{
		public EJMMesh ejmmesh;

		public Color DefaultColor = new Color(0f, 0.2f, 1.0f, 0.95f);
		public Color SelectionColor = new Color(1f, 0.25f, 0.25f, 0.9f);

		public abstract void DrawSceneGUI();
		public abstract void ReloadData();
	}

	public class EJMMesh
	{
	#region VARIABLES
		//PUBLIC
		public Renderer mRenderer;
		public Transform targettransform;
		public Matrix4x4 LocalToWorld;
		public Mesh SharedMesh;

		//Private Vars
		bool isSkinned;
		MeshFilter _mFilter;
		SkinnedMeshRenderer _smRenderer;
	#endregion

	#region CONSTRUCTORS
		public EJMMesh(MeshFilter component){
			isSkinned = false;
			_mFilter = component;
			mRenderer = component.GetComponent<Renderer> ();
			targettransform = component.transform;
			SetSharedMesh (_mFilter.sharedMesh);
			RefreshMatrix ();
		}
		public EJMMesh(SkinnedMeshRenderer component){
			isSkinned = true;
			_smRenderer = component;
			mRenderer = component.GetComponent<Renderer> ();
			targettransform = component.transform;
			SetSharedMesh (_smRenderer.sharedMesh);
			RefreshMatrix ();
		}
	#endregion

	#region PROPERTIES
	  public bool IsSkinned{
			get{
				return isSkinned;
			}
		}
		public int vertexCount{
			get{
				return SharedMesh.vertexCount;
			}
		}
		public Vector3[] vertices{
			get{
				return SharedMesh.vertices;
			}
			set{
				SharedMesh.vertices = value;
			}
		}
		public Vector2[] uv{
			get{
				return SharedMesh.uv;
			}
			set{
				SharedMesh.uv = value;
			}
		}
		public Vector3[] normals{
			get{
				return SharedMesh.normals;
			}
			set{
				SharedMesh.normals = value;
			}
		}
		public Vector4[] tangents{
			get{
				return SharedMesh.tangents;
			}
			set{
				SharedMesh.tangents = value;
			}
		}
		public Color[] colors{
			get{
				return SharedMesh.colors;
			}
			set{
				SharedMesh.colors = value;
			}
		}
		public int[] triangles{
			get{
				return SharedMesh.triangles;
			}
			set{
				SharedMesh.triangles = value;
			}
		}
	#endregion

	#region METHODS
	#if UNITY_EDITOR
		public enum StorageLocation {Application, Asset, Model, Scene}
		public StorageLocation GetStorageLocation(){
			if (AssetDatabase.Contains (SharedMesh)) {
				string assetpath = AssetDatabase.GetAssetPath(SharedMesh as UnityEngine.Object);
				if (assetpath=="") {
					return StorageLocation.Application;
				} else if (assetpath.Split ('.')[1]=="asset") {
					return StorageLocation.Asset;
				} else {
					return StorageLocation.Model;
				}
			} else {
				return StorageLocation.Scene;
			}
		}
		public void SaveAsset(){
			if (!System.IO.Directory.Exists("Assets/mesh-toolkit/meshes/")){
				System.IO.Directory.CreateDirectory("Assets/mesh-toolkit/meshes/");
			}
			AssetDatabase.CreateAsset (SharedMesh, "Assets/mesh-toolkit/meshes/" + SharedMesh.name + ".asset");
			AssetDatabase.SaveAssets ();
		}
	#endif
		public void RefreshMatrix() {
			if (isSkinned) {
				// Determine the root transform
				/*Transform root = null;
				if (_smRenderer.bones.Length > 0 && _smRenderer.bones[0] != null){
					root = _smRenderer.bones[0];
					for(int i = 1; i < _smRenderer.bones.Length; i++) {
						if (root.IsChildOf(_smRenderer.bones[i])) {
							root = _smRenderer.bones[i];
						}
					}
				}*/
				//Calculate offset using bounds
				Mesh m = new Mesh ();
				_smRenderer.BakeMesh (m);
				Vector3 offset = m.bounds.center - _smRenderer.sharedMesh.bounds.center;
				//Check offset by comparing vertices (More accurate)
				if (m.vertices.Length > 0 && _smRenderer.sharedMesh.vertices.Length > 0) {
					offset = m.vertices [0] - _smRenderer.sharedMesh.vertices [0];
				}
				// Determine the matrix
				LocalToWorld = Matrix4x4.TRS(_smRenderer.transform.position + offset, _smRenderer.transform.rotation, Vector3.one);
			} else {
				LocalToWorld = _mFilter.transform.localToWorldMatrix;
			}
		}
		static Vector3 ExtractScaleFromMatrix(Matrix4x4 matrix){
			Vector3 scale = new Vector3(
				matrix.GetColumn(0).magnitude,
				matrix.GetColumn(1).magnitude,
				matrix.GetColumn(2).magnitude
				);
			if (Vector3.Cross (matrix.GetColumn (0), matrix.GetColumn (1)).normalized != (Vector3)matrix.GetColumn (2).normalized) {
				scale.x *= -1;
			}
			return scale;
		}
		public void MakeUnique(){
			SetSharedMesh(ScriptableObject.Instantiate (SharedMesh as UnityEngine.Object) as Mesh);
		}
		public void SetSharedMesh(Mesh mesh){
			SharedMesh = mesh;
			if(isSkinned){
				_smRenderer.sharedMesh = SharedMesh;
			} else {
				_mFilter.sharedMesh = SharedMesh;
			}
		}
		public void UpdateCollider(){
			MeshCollider col = targettransform.GetComponent<MeshCollider>();
			if (col != null){
				col.sharedMesh = null;
				col.sharedMesh = SharedMesh;
				return;
			}
			BoxCollider BoxCol = targettransform.GetComponent<BoxCollider>();
			if (BoxCol != null){
				BoxCol.size = SharedMesh.bounds.size;
				BoxCol.center = SharedMesh.bounds.center;
				return;
			}
		}
		public void ImportData (Mesh dataMesh){
			SharedMesh.Clear ();
			SharedMesh.vertices = dataMesh.vertices;
			SharedMesh.uv = dataMesh.uv;
			SharedMesh.uv2 = dataMesh.uv2;
#if UNITY_5
			SharedMesh.uv3 = dataMesh.uv3;
			SharedMesh.uv4 = dataMesh.uv4;
#endif
			SharedMesh.colors = dataMesh.colors;
			SharedMesh.normals = dataMesh.normals;
			SharedMesh.tangents = dataMesh.tangents;
			SharedMesh.triangles = new int[dataMesh.triangles.Length];
			SharedMesh.subMeshCount = dataMesh.subMeshCount;
			for (int i=0; i<dataMesh.subMeshCount; i++) {
				SharedMesh.SetTriangles(dataMesh.GetTriangles(i), i);
			}
			SharedMesh.bindposes = dataMesh.bindposes;
			SharedMesh.boneWeights = dataMesh.boneWeights;
			SharedMesh.RecalculateBounds();
		}
	#endregion
	}
}
