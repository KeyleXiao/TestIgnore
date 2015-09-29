using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MeshTK
{
	public class UVsEditor : EJMMeshEditor
	{
		#region Variables
		private Vector2[] meshuvs;
		Vector3[] meshvertices;
		private List<int> selectedUVs = new List<int> ();
		private Rect windowrect = new Rect (10, 100, 200, 300);
		static int currentview = 0;

		int currentUVSet = 0;

		bool useSceneSelection = true;
		int tileCount = 1;
		Texture2D selectionTexture;
		float zoomfactor = 1;
		Vector2 scrollpos;
		Rect areaVisible;
		Rect areaRect;
		float uvtopixelratio;
		float offsetpix = 0f;
		Vector2 handleposition = Vector2.zero;
		bool dragged = false;

		int vertSize = 6;
		#endregion

		#region INHERITED METHODS
		public UVsEditor(EJMMesh target){
			ejmmesh = target;
			ReloadData ();
		}
		public override void DrawSceneGUI()
		{
			if (Event.current.type == EventType.Layout) {
				HandleUtility.AddDefaultControl (GUIUtility.GetControlID (FocusType.Passive));
			}
			if (meshuvs.Length>0 && useSceneSelection) {
				TrySingleSceneSelection();
			}
			Handles.BeginGUI ();
			windowrect = GUI.Window (0, windowrect, this.GUIWindow, "UV Tools");
			Handles.EndGUI ();
		}
		public override void ReloadData()
		{
			meshvertices = ejmmesh.SharedMesh.vertices;
			if (currentUVSet==0){
				meshuvs = ejmmesh.SharedMesh.uv;
			} else if (currentUVSet==1){
				meshuvs = ejmmesh.SharedMesh.uv2;
			}
#if UNITY_5
			else if (currentUVSet==2){
				meshuvs = ejmmesh.SharedMesh.uv3;
			} else if (currentUVSet==3){
				meshuvs = ejmmesh.SharedMesh.uv4;
			}
#endif
			if (ejmmesh.mRenderer && ejmmesh.mRenderer.sharedMaterial){
				selectionTexture = ejmmesh.mRenderer.sharedMaterial.mainTexture as Texture2D;
			}
		}
		public void UpdateMesh()
		{
			if (currentUVSet==0){
				ejmmesh.SharedMesh.uv = meshuvs;
			} else if (currentUVSet==1){
				ejmmesh.SharedMesh.uv2 = meshuvs;
			}
#if UNITY_5
			else if (currentUVSet==2){
				ejmmesh.SharedMesh.uv3 = meshuvs;
			} else if (currentUVSet==3){
				ejmmesh.SharedMesh.uv4 = meshuvs;
			}
#endif
		}
		#endregion

		#region GUI
		private void GUIWindow (int windowID)
		{
			GUI.DragWindow (new Rect (0, 0, 10000, 20));
			GUI.color = Color.white;

			GUI.BeginGroup (GUILayoutUtility.GetRect(windowrect.width, 18f), EditorStyles.toolbar);
			currentview = GUI.Toolbar (new Rect (5f, 0f, 140f, 18f), currentview, new string[3]{"Settings", "Edit", "Tools"}, EditorStyles.toolbarButton);
			EditorGUI.BeginChangeCheck ();
#if UNITY_5
			currentUVSet = EditorGUI.Popup (new Rect (145f, 0f, 40f, 18f), currentUVSet, new string[4]{"UV", "UV2", "UV3", "UV4"}, EditorStyles.toolbarPopup);
#else
			currentUVSet = EditorGUI.Popup (new Rect (145f, 0f, 40f, 18f), currentUVSet, new string[2]{"UV", "UV2"}, EditorStyles.toolbarPopup);
#endif
			if (EditorGUI.EndChangeCheck ()) {
				ReloadData();
			}
			GUI.EndGroup ();

			if (currentview==2){
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Unwrap", EditorStyles.miniButtonLeft)) {
					Vector2[] triuvs = Unwrapping.GeneratePerTriangleUV(ejmmesh.SharedMesh);
					int[] tris = ejmmesh.SharedMesh.triangles;
					for (int i=0;i<meshuvs.Length;i++){
						meshuvs[i] = triuvs[Array.FindIndex(tris, p => p==i)];
					}
					UpdateMesh ();
				}
				if (GUILayout.Button ("Export", EditorStyles.miniButtonRight)) {
					int w = 1024;
					int h = 1024;
					Texture2D tex = new Texture2D(w, h);
					int[] tris = ejmmesh.SharedMesh.triangles;
					for (int i=0;i<tris.Length;i+=3){
						Vector2 pt1 = meshuvs[tris[i]];
						Vector2 pt2 = meshuvs[tris[i+1]];
						Vector2 pt3 = meshuvs[tris[i+2]];
						TextureTools.DrawLine(tex, (int)(pt1.x*w), (int)(pt1.y*h), (int)(pt2.x*w), (int)(pt2.y*h), Color.red);
						TextureTools.DrawLine(tex, (int)(pt2.x*w), (int)(pt2.y*h), (int)(pt3.x*w), (int)(pt3.y*h), Color.red);
						TextureTools.DrawLine(tex, (int)(pt3.x*w), (int)(pt3.y*h), (int)(pt1.x*w), (int)(pt1.y*h), Color.red);
					}
					tex.Apply();
					string path = EditorUtility.SaveFilePanelInProject ("Save as", "uv_map", "png", "");
					TextureTools.SaveTexture(tex, path);
				}
				GUILayout.EndHorizontal ();
			}

			else if (currentview==1){
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Flip X", EditorStyles.miniButtonLeft)) {
					for (int i=0;i<meshuvs.Length;i++){
						meshuvs[i].x = 1f-meshuvs[i].x;
					}
					UpdateMesh();
				}
				if (GUILayout.Button ("Flip Y", EditorStyles.miniButtonRight)) {
					for (int i=0;i<meshuvs.Length;i++){
						meshuvs[i].y = 1f-meshuvs[i].y;
					}
					UpdateMesh();
				}
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal();
				if(GUILayout.Button("Rotate 45 deg", EditorStyles.miniButtonLeft)){
					UVTools.Rotate (ejmmesh.SharedMesh, 45f);
					ReloadData();
				}
				if(GUILayout.Button("Rotate 90 deg", EditorStyles.miniButtonRight)){
					UVTools.Rotate (ejmmesh.SharedMesh, 90f);
					ReloadData();
				}
				GUILayout.EndHorizontal();
			}

			else if (currentview==0){
				GUILayout.BeginHorizontal();
				useSceneSelection = EditorGUILayout.Toggle(useSceneSelection, GUILayout.Width (20f));
				GUILayout.Label("Select in Scene");
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Zoom");
				zoomfactor = Mathf.Clamp(EditorGUILayout.FloatField(zoomfactor), 0.5f, 4f);
				GUILayout.Label ("Tiles");
				tileCount = Mathf.Clamp(EditorGUILayout.IntField(tileCount), 1, 5);
				GUILayout.EndHorizontal ();
			}
			RenderUVEditor();
			if (selectedUVs.Count == 0 || !TryTranslate ()) {
				if (!useSceneSelection){
					TrySingleSelect ();
				}
			}
		}
		void RenderUVEditor() {
			uvtopixelratio = zoomfactor * 180f;
			areaRect = GUILayoutUtility.GetRect (180f, 180f);
			areaVisible = new Rect(0, 0, uvtopixelratio*tileCount, uvtopixelratio*tileCount);
			GUI.Box (areaRect, "");
			scrollpos = GUI.BeginScrollView(areaRect, scrollpos, areaVisible);
			//Draw the Background
			if (selectionTexture != null){
				for (int x = 0; x < tileCount; x++) {
					for (int y = 0; y < tileCount; y++) {
						GUI.DrawTexture(new Rect(uvtopixelratio*x,uvtopixelratio*y,uvtopixelratio,uvtopixelratio), selectionTexture);
					}
				}
			}
			//Draw the uvs
			offsetpix = ((tileCount-1) / 2) * uvtopixelratio;
			for (int i = 0; i < meshuvs.Length; i++) {
				if (selectedUVs.Contains(i)) GUI.color = SelectionColor; else GUI.color = DefaultColor;
				GUI.DrawTexture(new Rect(offsetpix + uvtopixelratio*meshuvs[i].x-vertSize/2, offsetpix + uvtopixelratio*(1-meshuvs[i].y)-vertSize/2, vertSize, vertSize), EditorGUIUtility.whiteTexture);
			}
			GUI.EndScrollView();
		}
		#endregion

		#region TRANSLATION
		bool TryTranslate() {
			Handles.color = Color.red;
			Handles.RectangleCap (0, new Vector2(areaRect.x, areaRect.y)+handleposition-scrollpos, Quaternion.identity, 10f);

			if (Event.current.type == EventType.MouseDrag) {
				dragged = true;
				if (HandleUtility.DistanceToRectangle (new Vector2(areaRect.x, areaRect.y)+handleposition-scrollpos, Quaternion.identity, 10f) == 0) {
					handleposition += Event.current.delta;
					Vector2 d = Event.current.delta * (1/uvtopixelratio);
					d.y=-d.y;
					for (int i = 0; i < selectedUVs.Count; i++) {
						meshuvs[selectedUVs[i]] += d;
					}
					UpdateMesh ();
					return true;
				}
			}
			return false;
		}
		#endregion

		#region SELECTION
		bool TrySingleSceneSelection(){
			// End Early if the click was in the window
			if (windowrect.Contains(Event.current.mousePosition)) {
				return false;
			}
			if (Event.current.type == EventType.MouseDown && !Event.current.shift) {
				selectedUVs.Clear ();
			} else if (Event.current.type == EventType.MouseUp) {
				float closestdistance = 100f;
				float dist = 0f;
				Vector2 screenpt;
				List<int> indices = new List<int>();
				for (int i=0; i<meshvertices.Length; i++) {
					screenpt = Camera.current.WorldToScreenPoint (ejmmesh.LocalToWorld.MultiplyPoint3x4(meshvertices [i]));
					dist = Vector2.Distance (screenpt, EditorTools.GUIToScreenPoint (Event.current.mousePosition));
					if (dist < closestdistance) {
						closestdistance = dist;
						indices.Clear();
						indices.Add(i);
					}
					else if (dist == closestdistance){
						indices.Add (i);
					}
				}
				foreach (int i in indices){
					if (!selectedUVs.Contains (i)) {
						selectedUVs.Add (i);
					} else {
						selectedUVs.Remove (i);
					}
				}
				UpdateSelectionCenter();
				return true;
			}
		return false;
		}

		bool TrySingleSelect()
		{
			if (Event.current.type == EventType.MouseDown && !Event.current.shift && HandleUtility.DistanceToRectangle (new Vector2(areaRect.x, areaRect.y)+handleposition-scrollpos, Quaternion.identity, 10f) > 0) {
				selectedUVs.Clear();
			}
			else if (Event.current.type == EventType.MouseUp) {
				if (dragged){
					dragged = false;
				} else {
					Vector2 uvhandlesoffset = new Vector2 (offsetpix, offsetpix);
					float closestdistance = 50f;
					float dist = 0f;
					int closestindex = -1;
					for (int i=0; i<meshuvs.Length; i++) {
						dist = Vector2.Distance (uvtopixelratio*new Vector2(meshuvs[i].x, 1-meshuvs[i].y)+uvhandlesoffset, Event.current.mousePosition-new Vector2(areaRect.x, areaRect.y)+scrollpos);
						if (dist < closestdistance) {
							closestdistance = dist;
							closestindex = i;
						}
					}
					if (closestindex != -1) {
						if (!selectedUVs.Contains (closestindex)) {
							selectedUVs.Add (closestindex);
						} else {
							selectedUVs.Remove (closestindex);
						}
						UpdateSelectionCenter();
						return true;
					}
				}
			}
			return false;
		}
		void UpdateSelectionCenter()
		{
			Vector2 max = Vector2.zero;
			Vector2 min = new Vector2(10000f, 10000f);
			foreach (int i in selectedUVs){
				if (meshuvs[i].y<min.y){
					min.y = meshuvs[i].y;
				}
				if (meshuvs[i].y>max.y){
					max.y = meshuvs[i].y;
				}
				if (meshuvs[i].x<min.x){
					min.x = meshuvs[i].x;
				}
				if (meshuvs[i].x>max.x){
					max.x = meshuvs[i].x;
				}
			}
			handleposition = new Vector2(uvtopixelratio*(min.x+max.x)/2+offsetpix, uvtopixelratio*(1-(min.y+max.y)/2)+offsetpix);
		}
		#endregion
	}
}
