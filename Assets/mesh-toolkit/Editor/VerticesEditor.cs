using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace MeshTK
{
	public class VerticesEditor : EJMMeshEditor
	{
		#region VARIABLES
		private Vector3[] meshvertices;
		private Rect windowrect = new Rect (10, 100, 150, 200);
		private Quaternion rotationHandle = Quaternion.identity;
		private Vector3 scaleHandle = Vector3.one;
		//Selection data
		HashSet<int> selectedVertices = new HashSet<int> ();
		int selectiontype = PlayerPrefs.GetInt ("meshtk-selection", 0);
		Color selectedCol = Color.white;
		Vector3 worldselectioncenter;
		Vector3 localselectioncenter;
		//Settings
		bool showColorSettings = false;
		//bool selectDoubles = true;
		#endregion

		#region INHERITED METHODS
		public VerticesEditor(EJMMesh target){
			ejmmesh = target;
			ReloadData ();
		}

		public override void DrawSceneGUI()
		{
			if (!(Tools.current == Tool.View || (Event.current.isMouse && Event.current.button > 0) || Event.current.type == EventType.ScrollWheel)) {
				if (Event.current.type == EventType.Layout) {
					HandleUtility.AddDefaultControl (GUIUtility.GetControlID (FocusType.Passive));
					Handles.BeginGUI ();
					windowrect = GUI.Window (0, windowrect, GUIWindow, "Vertex Tools");
					Handles.EndGUI ();
				} else {
					TryShortcuts();
				}
				if (Event.current.type == EventType.Repaint){
					DrawVertices ();
				}
				if (!TryTranslation ()) {
					if (selectiontype==0){
						TrySingleSelect ();
					} else if (selectiontype==1){
						TryBoxSelect ();
					} else {
						TryPaintSelect ();
					}
				}
			}
		}

		//override the reload method
		public override void ReloadData()
		{
			Vector3[] newverts = ejmmesh.SharedMesh.vertices;
			if (newverts != meshvertices) {
				meshvertices = newverts;
				selectedVertices.Clear ();
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
			GUI.DragWindow (new Rect (0, 0, 1000, 20));
			GUI.color = Color.white;
			Rect tooltipBox = GUILayoutUtility.GetRect (100f, 200f, 18f, 18f);
			GUILayout.Space (tooltipBox.height);
			EditorGUI.BeginChangeCheck ();
			selectiontype = EditorGUILayout.Popup (selectiontype, new string[3]{"Single Select (Shift-1)", "Box Select (Shift-2)", "Paint Select (Shift-3)"});
			if (EditorGUI.EndChangeCheck ()) {
				PlayerPrefs.SetInt ("meshtk-selection", selectiontype);
			}
			if (GUILayout.Button (new GUIContent("Select All", "Select All (Shift-a)"), EditorStyles.miniButton)){
				selectedVertices.Clear ();
				foreach (int item in Enumerable.Range(0,meshvertices.Length)){
	            	selectedVertices.Add(item);
	       		}
				UpdateSelectionCenter();
			}
			//Color
			GUILayout.BeginHorizontal();
			if (GUILayout.Button ("Set Color", EditorStyles.miniButtonLeft)) {
				Color[] cols = ejmmesh.SharedMesh.colors;
				if (cols.Length<meshvertices.Length){
					cols = new Color[meshvertices.Length];
				}
				foreach(int i in selectedVertices){
					cols[i] = selectedCol;
				}
				ejmmesh.SharedMesh.colors = cols;
			}
			showColorSettings = GUILayout.Toggle (showColorSettings, "+", EditorStyles.miniButtonRight, GUILayout.Width(30f));
			GUILayout.EndHorizontal();
			if (showColorSettings){
				selectedCol = EditorTools.ColorPicker(selectedCol);
			}
			//Weld
			if (GUILayout.Button ("Weld", EditorStyles.miniButton)) {
				VertexTools.Weld (ejmmesh.SharedMesh, selectedVertices.ToArray ());
				ReloadData();
			}
			//Delete
			if (GUILayout.Button (new GUIContent("Delete Selected", "Delete Selected (Shift-d)"), EditorStyles.miniButton)) {
				VertexTools.Remove (ejmmesh.SharedMesh, selectedVertices.ToArray ());
				ReloadData();
			}
			EditorGUI.HelpBox (tooltipBox, string.IsNullOrEmpty (GUI.tooltip) ? "Current tool: " + Tools.current.ToString () : GUI.tooltip, MessageType.None);
		}

		private void TryShortcuts(){
			if (Event.current.type == EventType.KeyUp && Event.current.shift){
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
					selectedVertices.Clear ();
					foreach (int item in Enumerable.Range(0,meshvertices.Length)){
						selectedVertices.Add(item);
					}
					UpdateSelectionCenter();
					break;
				case KeyCode.D:
					VertexTools.Remove (ejmmesh.SharedMesh, selectedVertices.ToArray ());
					ReloadData();
					break;
				}
			}
		}

		private void DrawVertices()
		{
			Handles.matrix = ejmmesh.LocalToWorld;
			for (int i = 0; i<meshvertices.Length; i++){
				if (selectedVertices.Contains(i))
					Handles.color = SelectionColor;
				else
					Handles.color = DefaultColor;
				Handles.DotCap (0, meshvertices [i], Quaternion.identity, HandleUtility.GetHandleSize (meshvertices [i]) * 0.04f);
			}
			/*
			Handles.color = SelectionColor;
			foreach (int i in selectedVertices){
				Handles.DotCap (0, meshvertices [i], Quaternion.identity, HandleUtility.GetHandleSize (meshvertices [i]) * 0.04f);
			}
			*/
			Handles.matrix = Matrix4x4.identity;
		}
		#endregion

		#region SELECTION
		private void TrySingleSelect ()
		{
			//Clear vertices if shift not held down
			if (Event.current.type == EventType.MouseDown && !Event.current.shift) {
				selectedVertices.Clear ();
			} else if (Event.current.type == EventType.MouseUp) {
				float closestdistance = 100f;
				float dist = 0f;
				Vector2 screenpt;
				//int closestindex = -1;
				List<int> indices = new List<int>();
				for (int i=0; i<meshvertices.Length; i++) {
					screenpt = Camera.current.WorldToScreenPoint (ejmmesh.LocalToWorld.MultiplyPoint3x4(meshvertices [i]));
					dist = Vector2.Distance (screenpt, EditorTools.GUIToScreenPoint (Event.current.mousePosition));
					if (dist < closestdistance) {
						closestdistance = dist;
						//closestindex = i;
						indices.Clear();
						indices.Add(i);
					}
					else if (dist == closestdistance){
						indices.Add (i);
					}
				}
				foreach (int i in indices){
					if (!selectedVertices.Contains (i)) {
						selectedVertices.Add (i);
					} else {
						selectedVertices.Remove (i);
					}
				}
				UpdateSelectionCenter();
			}
		}
		/// <summary>
		/// Tries to box select vertices in the scene view.
		/// </summary>
		private void TryBoxSelect()
		{
			if (Event.current.type == EventType.MouseDown && !Event.current.shift) {
				selectedVertices.Clear ();
			}
			Rect? temprect = EditorTools.TryBoxSelect ();
			if (temprect != null) {
				Rect selectedRect = EditorTools.GUIToScreenRect(temprect.GetValueOrDefault ());
				for (int i = 0; i < meshvertices.Length; i++) {
					if (selectedRect.Contains (Camera.current.WorldToScreenPoint (ejmmesh.LocalToWorld.MultiplyPoint3x4 (meshvertices [i])))) {
						if (selectedVertices.Contains (i)) {
							selectedVertices.Remove (i);
						} else {
							selectedVertices.Add (i);
						}
					}
				}
				UpdateSelectionCenter();
			}
		}
		private void TryPaintSelect ()
		{
			if (Event.current.type == EventType.MouseDown && !Event.current.shift) {
				selectedVertices.Clear ();
			} else if (Event.current.type == EventType.MouseDrag) {
				float closestdistance = 100f;
				float dist = 0f;
				Vector2 screenpt;
				//int closestindex = -1;
				List<int> indices = new List<int>();
				for (int i=0; i<meshvertices.Length; i++) {
					screenpt = Camera.current.WorldToScreenPoint (ejmmesh.LocalToWorld.MultiplyPoint3x4(meshvertices [i]));
					dist = Vector2.Distance (screenpt, EditorTools.GUIToScreenPoint (Event.current.mousePosition));
					if (dist < closestdistance) {
						closestdistance = dist;
						//closestindex = i;
						indices.Clear();
						indices.Add(i);
					}
					else if (dist == closestdistance){
						indices.Add (i);
					}
				}
				foreach (int i in indices){
					if (!selectedVertices.Contains (i)) {
						selectedVertices.Add (i);
					}
				}
			} else if (Event.current.type == EventType.MouseUp) {
				UpdateSelectionCenter();
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
				return TryVertexRotate ();
			} else if (Tools.current == Tool.Scale) {
				return TryVerticesScale ();
			}
			return false;
		}
		private bool TryMove()
		{
			Vector3 newPosition = Handles.PositionHandle (worldselectioncenter, Quaternion.identity);
			if (newPosition != worldselectioncenter) {
				Vector3 offset = ejmmesh.LocalToWorld.MultiplyVector(newPosition - worldselectioncenter);
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
		private bool TryVertexRotate()
		{
			Quaternion newRotation = Handles.RotationHandle (rotationHandle, worldselectioncenter);
			if (newRotation != rotationHandle) {
				Quaternion offsetRotation = Quaternion.Euler (newRotation.eulerAngles - rotationHandle.eulerAngles);
				foreach (int index in selectedVertices) {
					meshvertices [index] = offsetRotation * (meshvertices[index] - localselectioncenter) + localselectioncenter;
				}
				rotationHandle = newRotation;
				UpdateMesh ();
				return true;
			}
			return false;
		}

		private bool TryVerticesScale()
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
	}
}
