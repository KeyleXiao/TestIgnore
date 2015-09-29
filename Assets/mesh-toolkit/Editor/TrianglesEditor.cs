using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace MeshTK
{
	public class TrianglesEditor : EJMMeshEditor
	{
		#region VARIABLES
		private Vector3[] meshvertices;
		private int[] meshtriangles;
		private HashSet<int> selectedVertices = new HashSet<int> ();
		private HashSet<int> selectedTriangles = new HashSet<int> ();
		private int selectiontype = PlayerPrefs.GetInt ("meshtk-selection", 0);
		private Rect windowrect = new Rect (10, 100, 150, 220);
		private Material selectionmaterial = new Material("Shader \"Lines/Colored Blended\" {" +
		                                                  "SubShader { Pass { " +
		                                                  "    Offset -1, -1 " +
		                                                  "    Blend SrcAlpha OneMinusSrcAlpha " +
		                                                  "    ZWrite Off Cull Off Fog { Mode Off } " +
		                                                  "    BindChannels {" +
		                                                  "      Bind \"vertex\", vertex Bind \"color\", color }" +
		                                                  "} } }");
		private Quaternion rotationHandle = Quaternion.identity;
		private Vector3 scaleHandle = Vector3.one;
		private Vector3 worldselectioncenter;
		private Vector3 localselectioncenter;
		//Settings
		bool showExtrusionSettings = false;
		bool showMaterialSettings = false;
		bool showDetachSettings = false;
		bool showDeleteSettings = false;
		bool showSubdivideSettings = false;

		float extrusiondistance = 1.0f;
		enum SubdivisionType {EDGE, CENTER};
		SubdivisionType subdividetype = SubdivisionType.EDGE;
		bool autoWeld = true;
		bool removeUnusedVerts = true;
		int detachMethod = 1;
		Material detachMaterial;
		#endregion
		
		#region INHERITED METHODS
		public TrianglesEditor(EJMMesh target){
			ejmmesh = target;
			ReloadData ();
		}
		public override void DrawSceneGUI()
		{
			if (!(Tools.current == Tool.View || (Event.current.isMouse && Event.current.button > 0) || Event.current.type == EventType.ScrollWheel)) {
				if (Event.current.type == EventType.Layout) {
					HandleUtility.AddDefaultControl (GUIUtility.GetControlID (FocusType.Passive));
				} else {
					TryShortcuts();
				}
				DrawTriangles ();
				if (!TryTranslation ()) {
					if (selectiontype==0){
						TrySingleSelect ();
					} else if (selectiontype==1){
						TryBoxSelect ();
					} else {
						TryPaintSelect ();
					}
				}
				Handles.BeginGUI ();
				windowrect = GUI.Window (0, windowrect, GUIWindow, "Triangle Tools");
				Handles.EndGUI ();
			}
		}
		/// <summary>
		/// Reloads the data if it has changed, but only updates the selection if the length of tris changed.
		/// </summary>
		public void ReloadIfChanged(){
			if (ejmmesh.SharedMesh.vertices != meshvertices || ejmmesh.SharedMesh.triangles != meshtriangles) {
				meshvertices = ejmmesh.SharedMesh.vertices;
				meshtriangles = ejmmesh.SharedMesh.triangles;
				UpdateSelectedVertices ();
			}
			if (ejmmesh.SharedMesh.triangles.Length != meshtriangles.Length) {
				selectedVertices.Clear ();
				selectedTriangles.Clear ();
			}
		}
		/// <summary>
		/// Forced the data to reload
		/// </summary>
		public override void ReloadData()
		{
			if (ejmmesh.SharedMesh.vertices != meshvertices || ejmmesh.SharedMesh.triangles != meshtriangles) {
				meshvertices = ejmmesh.SharedMesh.vertices;
				meshtriangles = ejmmesh.SharedMesh.triangles;
				selectedVertices.Clear ();
				selectedTriangles.Clear ();
			}
		}
		public void UpdateMesh()
		{
			ejmmesh.SharedMesh.vertices = meshvertices;
			ejmmesh.SharedMesh.RecalculateBounds ();
		}
		#endregion
		
		#region GUI
		private void GUIWindow (int windowID)
		{
			GUI.DragWindow (new Rect (0, 0, 10000, 20));
			GUI.color = Color.white;
			Rect tooltipBox = GUILayoutUtility.GetRect (100f, 200f, 18f, 18f);
			GUILayout.Space (tooltipBox.height);
			EditorGUI.BeginChangeCheck ();
			selectiontype = EditorGUILayout.Popup (selectiontype, new string[3]{"Single Select (Shift-1)", "Box Select (Shift-2)", "Paint Select (Shift-3)"});
			if (EditorGUI.EndChangeCheck ()) {
				PlayerPrefs.SetInt ("meshtk-selection", selectiontype);
			}
			if (GUILayout.Button (new GUIContent("Select All", "Select All (Shift-a)"), EditorStyles.miniButton)){
				for(int i=0;i<meshtriangles.Length/3;i++){
					selectedTriangles.Add (i);
				}
				UpdateSelectedVertices();
				UpdateSelectionCenter();
			}
			if (GUILayout.Button ("Flip Selection", EditorStyles.miniButton)) {
				TriangleTools.Flip (ejmmesh.SharedMesh, selectedTriangles.ToArray());
				ReloadIfChanged();
			}
			//EXTRUDE
			GUILayout.BeginHorizontal();
			if (GUILayout.Button (new GUIContent("Extrude", "Extrude Triangles (Shift-e)"), EditorStyles.miniButtonLeft)) {
				TriangleTools.Extrude(ejmmesh.SharedMesh, selectedTriangles.ToArray (), extrusiondistance);
				ReloadData();
			}
			showExtrusionSettings = GUILayout.Toggle (showExtrusionSettings, "+", EditorStyles.miniButtonRight, GUILayout.Width(30f));
			GUILayout.EndHorizontal();
			if (showExtrusionSettings){
				extrusiondistance = EditorGUILayout.FloatField(extrusiondistance);
			}
			//SUBDIVIDE
			GUILayout.BeginHorizontal();
			if (GUILayout.Button (new GUIContent("Subdivide", "Subdivide Triangles (Shift-s)"), EditorStyles.miniButtonLeft)) {
				if (subdividetype==SubdivisionType.EDGE){
					TriangleTools.SubdivideByEdge(ejmmesh.SharedMesh, selectedTriangles.ToArray(), autoWeld);
				} else {
					TriangleTools.SubdivideByCenter(ejmmesh.SharedMesh, selectedTriangles.ToArray ());
				}
				ReloadData();
			}
			showSubdivideSettings = GUILayout.Toggle (showSubdivideSettings, "+", EditorStyles.miniButtonRight, GUILayout.Width(30f));
			GUILayout.EndHorizontal();
			if (showSubdivideSettings){
				subdividetype = (SubdivisionType)EditorGUILayout.EnumPopup (subdividetype);
				autoWeld = GUILayout.Toggle (autoWeld, "Auto-Weld");
			}
			//SET MATERIAL
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Set Material", EditorStyles.miniButtonLeft)) {
				Material[] allMaterials = new Material[ejmmesh.SharedMesh.subMeshCount];
				System.Array.Copy(ejmmesh.mRenderer.sharedMaterials, allMaterials, Mathf.Min(ejmmesh.mRenderer.sharedMaterials.Length, ejmmesh.SharedMesh.subMeshCount));
				int start=0;
				int end;
				for (int i=0;i<ejmmesh.SharedMesh.subMeshCount;i++){
					end = start+ejmmesh.SharedMesh.GetTriangles(i).Length;
					if (System.Array.FindIndex (selectedVertices.ToArray(), p => start<=p&&p<end) !=-1){
						allMaterials[i] = detachMaterial;
					}
					start = end;
				}
				ejmmesh.mRenderer.sharedMaterials = allMaterials;
				ReloadIfChanged();
			}
			showMaterialSettings = GUILayout.Toggle (showMaterialSettings, "+", EditorStyles.miniButtonRight, GUILayout.Width(30f));
			GUILayout.EndHorizontal ();
			if (showMaterialSettings){
				try {
					detachMaterial = EditorGUILayout.ObjectField(detachMaterial, typeof(Material), true) as Material;
				} catch (ExitGUIException) {}
				catch (UnityException) {}
			}
			//DETACH
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Detach Selection", EditorStyles.miniButtonLeft)) {
				if (detachMethod==0){
					MeshTK.TriangleTools.Detach(ejmmesh.SharedMesh, selectedTriangles.ToArray(), false);
				} else if (detachMethod==1){
					TriangleTools.Detach (ejmmesh.SharedMesh, selectedTriangles.ToArray(), true);
					Material[] allMaterials = new Material[ejmmesh.SharedMesh.subMeshCount];
					System.Array.Copy(ejmmesh.mRenderer.sharedMaterials, allMaterials, Mathf.Min(ejmmesh.mRenderer.sharedMaterials.Length, ejmmesh.SharedMesh.subMeshCount));
					allMaterials[ejmmesh.SharedMesh.subMeshCount-1] = detachMaterial;
					ejmmesh.mRenderer.sharedMaterials = allMaterials;
				} else {
					GameObject obj = TriangleTools.DetachToNewObject(ref ejmmesh, selectedTriangles);
					obj.GetComponent<Renderer>().sharedMaterials = ejmmesh.mRenderer.sharedMaterials;
					obj.transform.position = ejmmesh.targettransform.position;
					obj.transform.rotation = ejmmesh.targettransform.rotation;
					obj.transform.localScale = ejmmesh.targettransform.lossyScale;
				}
				ReloadIfChanged();
			}
			showDetachSettings = GUILayout.Toggle (showDetachSettings, "+", EditorStyles.miniButtonRight, GUILayout.Width(30f));
			GUILayout.EndHorizontal ();
			if (showDetachSettings){
				detachMethod = EditorGUILayout.Popup(detachMethod, new string[3]{"Detach Verts", "Detach Submesh", "Detach Object"});
				if (detachMethod>0){
					try {
						detachMaterial = EditorGUILayout.ObjectField(detachMaterial, typeof(Material), true) as Material;
					} catch (ExitGUIException) {}
					catch (UnityException) {}
				}
			}
			//DELETE
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button (new GUIContent("Delete Selected", "Delete Selected (Shift-d)"), EditorStyles.miniButtonLeft)) {
				TriangleTools.Remove (ejmmesh.SharedMesh, selectedTriangles.ToArray ());
				if(removeUnusedVerts){
					VertexTools.RemoveUnused(ejmmesh.SharedMesh);
				}
				ReloadData();
			}
			showDeleteSettings = GUILayout.Toggle (showDeleteSettings, "+", EditorStyles.miniButtonRight, GUILayout.Width(30f));
			GUILayout.EndHorizontal ();
			if (showDeleteSettings) {
				removeUnusedVerts = GUILayout.Toggle (removeUnusedVerts, "Remove Verts");
			}
			EditorGUI.HelpBox (tooltipBox, string.IsNullOrEmpty (GUI.tooltip) ? "Current tool: " + Tools.current.ToString () : GUI.tooltip, MessageType.None);
		}
		private void TryShortcuts(){
			if (Event.current.type == EventType.KeyDown && Event.current.shift){
				switch (Event.current.keyCode){
				case KeyCode.Alpha1:
					selectiontype = 0;
					break;
				case KeyCode.Alpha2:
					selectiontype = 1;
					break;
				case KeyCode.Alpha3:
					selectiontype = 2;
					break;
				case KeyCode.A:
					for(int i=0;i<meshtriangles.Length/3;i++){
						selectedTriangles.Add (i);
					}
					UpdateSelectedVertices();
					UpdateSelectionCenter();
					break;
				case KeyCode.D:
					TriangleTools.Remove (ejmmesh.SharedMesh, selectedTriangles.ToArray ());
					if(removeUnusedVerts){
						VertexTools.RemoveUnused(ejmmesh.SharedMesh);
					}
					ReloadData();
					break;
				case KeyCode.E:
					TriangleTools.Extrude(ejmmesh.SharedMesh, selectedTriangles.ToArray (), extrusiondistance);
					ReloadData();
					break;
				case KeyCode.S:
					if (subdividetype==SubdivisionType.EDGE){
						TriangleTools.SubdivideByEdge(ejmmesh.SharedMesh, selectedTriangles.ToArray(), autoWeld);
					} else {
						TriangleTools.SubdivideByCenter(ejmmesh.SharedMesh, selectedTriangles.ToArray ());
					}
					ReloadData();
					break;
				default:
					return;
				}
				PlayerPrefs.SetInt ("meshtk-selection", selectiontype);
			}
		}
		private void DrawTriangles(){
			selectionmaterial.SetPass (0);
			GL.Begin(GL.TRIANGLES);
			GL.Color(SelectionColor);
			
			for (int i = 0; i < meshtriangles.Length; i+=3) {
				if (selectedTriangles.Contains (i/3)){
					GL.Vertex (ejmmesh.LocalToWorld.MultiplyPoint3x4(meshvertices[meshtriangles[i]]));
					GL.Vertex (ejmmesh.LocalToWorld.MultiplyPoint3x4(meshvertices[meshtriangles[i+1]]));
					GL.Vertex (ejmmesh.LocalToWorld.MultiplyPoint3x4(meshvertices[meshtriangles[i+2]]));
				}
			}
			
			GL.End();
		}
		#endregion
		
		#region TRANSLATION
		private bool TryTranslation()
		{
			//<summary>
			//Attempts to translate the selected vertices. Returns true if anything was translated.
			//If not, it returns false
			//</summary>
			if (selectedVertices.Count < 1) { return false;}
			if (Tools.current == Tool.Move) {
				return TryMove();
			} else if (Tools.current == Tool.Rotate) {
				return TryRotate ();
			} else if (Tools.current == Tool.Scale) {
				return TryScale ();
			}
			return false;
		}
		private bool TryMove()
		{
			Vector3 newPosition = Handles.PositionHandle (worldselectioncenter, Quaternion.identity);
			if (newPosition != worldselectioncenter) {
				Vector3 offset = ejmmesh.LocalToWorld.MultiplyVector (newPosition - worldselectioncenter);
				foreach (int index in selectedVertices) {
					meshvertices [index] += offset;
				}
				worldselectioncenter = newPosition;
				localselectioncenter += offset;
				UpdateMesh ();
				return true;
			}
			return false;
		}
		private bool TryRotate()
		{
			Quaternion newRotation = Handles.RotationHandle (rotationHandle, worldselectioncenter);
			if (newRotation != rotationHandle) {
				foreach (int index in selectedVertices) {
					meshvertices [index] = Quaternion.Euler (newRotation.eulerAngles - rotationHandle.eulerAngles) * (meshvertices[index] - localselectioncenter) + localselectioncenter;
				}
				rotationHandle = newRotation;
				UpdateMesh ();
				return true;
			}
			return false;
		}
		
		private bool TryScale()
		{
			Vector3 newScale = Handles.ScaleHandle (scaleHandle, worldselectioncenter, Quaternion.identity, HandleUtility.GetHandleSize (worldselectioncenter));
			if (newScale != scaleHandle) {
				foreach (int index in selectedVertices) {
					meshvertices [index] += Vector3.Scale(newScale - scaleHandle, meshvertices [index] - localselectioncenter);
				}
				scaleHandle = newScale;
				UpdateMesh ();
				return true;
			}
			return false;
		}
		#endregion
		
		#region SELECTION
		private void TrySingleSelect()
		{
			if (Event.current.type == EventType.MouseDown && !Event.current.shift) {
				selectedVertices.Clear ();
				selectedTriangles.Clear ();
			} else if (Event.current.type==EventType.MouseUp){
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				ray.origin = ejmmesh.LocalToWorld.inverse.MultiplyPoint3x4(ray.origin);
				ray.direction = ejmmesh.LocalToWorld.inverse.MultiplyVector(ray.direction);//targettransform.InverseTransformDirection(ray.direction);
				float closestdist = Mathf.Infinity;
				int closestindex = -1;
				for (int i = 0; i < meshtriangles.Length; i+=3) {
					float intersectdist = EditorTools.RayTriangleIntersection(ray, meshvertices[meshtriangles[i]], meshvertices[meshtriangles[i+1]], meshvertices[meshtriangles[i+2]]);
					if (intersectdist!=-1 && intersectdist < closestdist)
					{
						closestindex = i/3;
						closestdist = intersectdist;
					}
				}
				if (closestindex!=-1){
					if (selectedTriangles.Contains (closestindex)) {
						selectedTriangles.Remove (closestindex);
					} else {
						selectedTriangles.Add (closestindex);
					}
				}
				UpdateSelectedVertices();
				UpdateSelectionCenter();
			}
		}
		private void TryBoxSelect(){
			if (Event.current.type == EventType.MouseDown && !Event.current.shift) {
				selectedVertices.Clear ();
				selectedTriangles.Clear ();
			}
			Rect? temprect = EditorTools.TryBoxSelect ();
			if (temprect != null) {
				selectedVertices.Clear ();
				Rect selectedRect = EditorTools.GUIToScreenRect(temprect.GetValueOrDefault ());
				for (int i = 0; i < meshvertices.Length; i++) {
					if (selectedRect.Contains (Camera.current.WorldToScreenPoint (ejmmesh.LocalToWorld.MultiplyPoint3x4(meshvertices [i])))) {
						selectedVertices.Add (i);
					}
				}
				for (int i=0; i<meshtriangles.Length; i+=3) {
					if (selectedVertices.Contains (meshtriangles [i]) && selectedVertices.Contains (meshtriangles [i + 1]) && selectedVertices.Contains (meshtriangles [i + 2])) {
						if (!selectedTriangles.Contains (i / 3)) {
							selectedTriangles.Add (i / 3);
						} else {
							selectedTriangles.Remove (i / 3);
						}
					}
				}
				UpdateSelectedVertices();
				UpdateSelectionCenter();
			}
		}
		private void TryPaintSelect()
		{
			if (Event.current.type == EventType.MouseDown && !Event.current.shift) {
				selectedVertices.Clear ();
				selectedTriangles.Clear ();
			} else if (Event.current.type==EventType.MouseDrag){
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				ray.origin = ejmmesh.LocalToWorld.inverse.MultiplyPoint3x4(ray.origin);
				ray.direction = ejmmesh.LocalToWorld.inverse.MultiplyVector(ray.direction);//targettransform.InverseTransformDirection(ray.direction);
				float closestdist = Mathf.Infinity;
				int closestindex = -1;
				for (int i = 0; i < meshtriangles.Length; i+=3) {
					float intersectdist = EditorTools.RayTriangleIntersection(ray, meshvertices[meshtriangles[i]], meshvertices[meshtriangles[i+1]], meshvertices[meshtriangles[i+2]]);
					if (intersectdist!=-1 && intersectdist < closestdist)
					{
						closestindex = i/3;
						closestdist = intersectdist;
					}
				}
				if (closestindex!=-1){
					if (!selectedTriangles.Contains (closestindex)) {
						selectedTriangles.Add (closestindex);
					}
				}
			} else if (Event.current.type==EventType.MouseUp){
				UpdateSelectedVertices();
				UpdateSelectionCenter();
			}
		}
		/// <summary>
		/// Sets the selected vertices to the indices of the vertices in the selected triangles
		/// </summary>
		private void UpdateSelectedVertices(){
			selectedVertices.Clear ();
			for (int i=0;i<meshtriangles.Length;i+=3){
				if (selectedTriangles.Contains(i/3)){
					if (!selectedVertices.Contains (meshtriangles[i])) {
						selectedVertices.Add (meshtriangles[i]);
					}
					if (!selectedVertices.Contains (meshtriangles[i+1])) {
						selectedVertices.Add (meshtriangles[i+1]);
					}
					if (!selectedVertices.Contains (meshtriangles[i+2])) {
						selectedVertices.Add (meshtriangles[i+2]);
					}
				}
			}
		}
		public void UpdateSelectionCenter()
		{
			Vector3 average = Vector3.zero;
			foreach (int index in selectedVertices) {
				average += meshvertices [index];
			}
			localselectioncenter = average / selectedVertices.Count;
			worldselectioncenter = ejmmesh.LocalToWorld.MultiplyPoint3x4(localselectioncenter);
		}
		#endregion
	}
}