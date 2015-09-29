using UnityEditor;
using UnityEngine;
using System;

namespace MeshTK
{
	public class ObjectEditor : EJMMeshEditor
	{
		#region VARIABLES
		private Vector2 scrollposition;
		// Settings
		bool showexportsettings = false;
		bool uselhcoordinates = true;
		// Information
		EJMMesh.StorageLocation StorageLoc;
		//Rects
		Rect WindowRect = new Rect (10, 100, 180, 200);
		readonly Rect DragRect = new Rect (0, 0, 1000, 20);
		#endregion

		#region PUBLIC METHODS
		public ObjectEditor(EJMMesh target){
			ejmmesh = target;
			ReloadData ();
		}
		public override void DrawSceneGUI()
		{
			if (Event.current.type == EventType.Layout) {
				HandleUtility.AddDefaultControl (GUIUtility.GetControlID (FocusType.Passive));
			}
			Handles.BeginGUI ();
			WindowRect = GUI.Window (0, WindowRect, this.GUIWindow, "Object Tools");
			Handles.EndGUI ();
		}
		public override void ReloadData(){
			StorageLoc = ejmmesh.GetStorageLocation ();
			return;
		}
		#endregion

		#region PRIVATE METHODS
		private void GUIWindow (int windowID)
		{
			GUI.DragWindow (DragRect);
			GUI.color = Color.white;
			scrollposition = GUILayout.BeginScrollView (scrollposition);
			ejmmesh.SharedMesh.name = GUILayout.TextField (ejmmesh.SharedMesh.name, GUILayout.MaxWidth(145f));
			GUILayout.Label ("Verts: " + ejmmesh.SharedMesh.vertexCount + ", Tris: " + ejmmesh.SharedMesh.triangles.Length / 3);
			switch(StorageLoc){
			case EJMMesh.StorageLocation.Application:
				EditorGUILayout.HelpBox ("This Mesh is a builtin Unity primitive. Changes will not be saved.", MessageType.Warning);
				if (GUILayout.Button("Save in scene", EditorStyles.miniButton)){
					ejmmesh.MakeUnique();
				}
				break;
			case EJMMesh.StorageLocation.Asset:
				EditorGUILayout.HelpBox ("This Mesh is saved in a project asset file.", MessageType.Info);
				if (GUILayout.Button("Show in Project", EditorStyles.miniButton)){
					EditorGUIUtility.PingObject(ejmmesh.SharedMesh as UnityEngine.Object);
				}
				if (GUILayout.Button("Save in scene", EditorStyles.miniButton)){
					ejmmesh.MakeUnique();
				}
				break;
			case EJMMesh.StorageLocation.Model:
				EditorGUILayout.HelpBox ("This Mesh is saved in a model file. Changes will not be saved.", MessageType.Warning);
				if (GUILayout.Button("Save in scene", EditorStyles.miniButton)){
					ejmmesh.MakeUnique();
				}
				break;
			case EJMMesh.StorageLocation.Scene:
				EditorGUILayout.HelpBox ("This Mesh is saved in the current scene.", MessageType.Info);
				if (GUILayout.Button ("Save in Project", EditorStyles.miniButton)) {
					ejmmesh.SaveAsset();
				}
				break;
			}
			// Export to OBJ
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Export", EditorStyles.miniButtonLeft);
			if (GUILayout.Button ("OBJ", EditorStyles.miniButtonMid)) {
				string path = EditorUtility.SaveFilePanelInProject(
					"Export to OBJ",
					ejmmesh.SharedMesh.name + ".obj",
					"obj",
					"");
				ObjTools.ExportToFile(ejmmesh.SharedMesh, ejmmesh.mRenderer, path, uselhcoordinates);
				AssetDatabase.Refresh();
			}
			if (GUILayout.Button ("FBX", EditorStyles.miniButtonMid)) {
				string path = EditorUtility.SaveFilePanelInProject(
					"Export to FBX",
					ejmmesh.SharedMesh.name + ".fbx",
					"fbx",
					"");
				if (ejmmesh.IsSkinned){
					FbxTools.ExportToFile(ejmmesh.mRenderer as SkinnedMeshRenderer, path, uselhcoordinates);
				} else {
					FbxTools.ExportToFile(ejmmesh.SharedMesh, ejmmesh.mRenderer, path, uselhcoordinates);
				}
				AssetDatabase.ImportAsset (path);
			}
			showexportsettings = GUILayout.Toggle (showexportsettings, "+", EditorStyles.miniButtonRight, GUILayout.Width(30f));
			GUILayout.EndHorizontal();
			if (showexportsettings){
				uselhcoordinates = GUILayout.Toggle (uselhcoordinates, "Use LH Coords (Unity)");
				uselhcoordinates = !GUILayout.Toggle (!uselhcoordinates, "Use RH Coords");
			}
			GUILayout.Space (15);
			//MIRROR
			if (GUILayout.Button ("Mirror X Axis", EditorStyles.miniButton)) {
				VertexTools.MirrorAxis(ejmmesh.SharedMesh, "x");
			}
			if (GUILayout.Button ("Mirror Y Axis", EditorStyles.miniButton)) {
				VertexTools.MirrorAxis(ejmmesh.SharedMesh, "y");
			}
			if (GUILayout.Button ("Mirror Z Axis", EditorStyles.miniButton)) {
				VertexTools.MirrorAxis(ejmmesh.SharedMesh, "z");
			}

			//********EDIT PIVOT*********
			GUILayout.Space (15);
			GUILayout.Label ("Pivot", "Helpbox");
			GUILayout.BeginHorizontal ();
			Vector3 center = ejmmesh.SharedMesh.bounds.center;
			EditorGUI.BeginChangeCheck ();
			center.x = EditorGUILayout.FloatField (center.x, GUILayout.Width (45f));
			center.y = EditorGUILayout.FloatField (center.y, GUILayout.Width (45f));
			center.z = EditorGUILayout.FloatField (center.z, GUILayout.Width (45f));
			if (EditorGUI.EndChangeCheck ()) {
				PivotTools.Set(ejmmesh.SharedMesh, center);
				ejmmesh.UpdateCollider();
			}
			GUILayout.EndHorizontal ();
			if (GUILayout.Button ("Center Pivot", EditorStyles.miniButton)) {
				ejmmesh.targettransform.position += PivotTools.Center(ejmmesh.SharedMesh);
			}
			if (GUILayout.Button ("Apply Transform", EditorStyles.miniButton)){
				ApplyTransform (true, true, true);
			}

			//********OPTIMIZE MESH********
			GUILayout.Space (15);
			GUILayout.Label ("Optimize Mesh", "Helpbox");
			if (GUILayout.Button ("Combine Children", EditorStyles.miniButton)){
				CombineTools.CombineChildren(ejmmesh.targettransform.gameObject);
			}
			if (GUILayout.Button ("Remove Unused Verts", EditorStyles.miniButton)) {
				VertexTools.RemoveUnused(ejmmesh.SharedMesh);
			}
			if (GUILayout.Button ("Split Shared Verts", EditorStyles.miniButton)){
				VertexTools.SplitShared(ejmmesh.SharedMesh);
			}
			if (GUILayout.Button ("Merge Double Verts", EditorStyles.miniButton)) {
				VertexTools.MergeDoubles(ejmmesh.SharedMesh);
			}
			if (GUILayout.Button ("Generate Triangle Strips", EditorStyles.miniButton)) {
				ejmmesh.SharedMesh.Optimize ();
			}
			GUILayout.EndScrollView ();
			if (GUI.changed && GUIUtility.hotControl == 0) {
				ejmmesh.UpdateCollider();
			}
		}

		private void ApplyTransform (bool applyPos, bool applyRot, bool applySca)
		{
			//Get Mesh Data
			Mesh mesh = ejmmesh.SharedMesh;
			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;
			//Get each vertex's world coorinates
			for (int i = 0; i < vertices.Length; i++) {
				vertices [i] = ejmmesh.targettransform.TransformPoint (vertices [i]);
			}
			//Reset the object's transform
			if (applyPos) {
				//Reset position
				ejmmesh.targettransform.localPosition = Vector3.zero;
			}
			if (applyRot) {
				//Fix Normals
				for (int i=0;i<normals.Length;i++){
					normals[i] = ejmmesh.targettransform.localRotation * normals[i];
				}
				//Reset Rotation
				ejmmesh.targettransform.localRotation = Quaternion.identity;
			}
			if (applySca) {
				//Reset scale
				ejmmesh.targettransform.localScale = Vector3.one;
			}
			//Get each vertex's local coordinates from its global coordinate
			for (int i = 0; i < vertices.Length; i++) {
				vertices [i] = ejmmesh.targettransform.InverseTransformPoint (vertices [i]);
			}
			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.RecalculateBounds ();
		}
		#endregion
	}
}
