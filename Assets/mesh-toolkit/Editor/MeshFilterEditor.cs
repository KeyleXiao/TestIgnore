using UnityEngine;
using UnityEditor;

namespace MeshTK
{
	[CustomEditor(typeof(MeshFilter))]
	public class MeshFilterEditor : Editor
	{
		//Editors
		EJMMesh ejmmesh;
		EJMMeshEditor CurrentMeshEditor;
		//target
		MeshFilter targetMeshFilter;

		// === EDITOR STATE (Static so they will remain until Unity is closed) ===
		static bool menuExpanded = false;
		static int EditingMode = -1;
		static Mesh previousMesh;
		static Material[] previousMaterials;
		//===== GUI Variables =====
		//GUIContent
		GUIContent OpenGUI;
		GUIContent OpenUniqueGUI;
		GUIContent CloseGUI;
		GUIContent ObjectGUI;
		GUIContent VertexGUI;
		GUIContent TriangleGUI;
		GUIContent NormalGUI;
		GUIContent UVsGUI;
		GUIContent PrimitivesGUI;
		GUIContent RevertGUI;
		GUIContent[] ToolbarIcons;
		//Rects
		static int ToolbarIconCount = 6;
		static Rect ClosedWindowRect = new Rect (10, 20, 89, 62);
		static Rect OpenedWindowRect = new Rect (10, 20, 346, 62);
		static Rect DragRect = new Rect (0, 0, 1000, 20);
		static Vector2 buttonsize = new Vector2 (37f, 37f);
		static Rect CloseRect = new Rect (5, 20, buttonsize.x, buttonsize.y);
		static Rect ToolbarRect = new Rect(10+buttonsize.x, 20, ToolbarIconCount*(buttonsize.x+5), buttonsize.y);
		static Rect UndoRect = new Rect(15 + buttonsize.x + ToolbarIconCount*(buttonsize.x+5), 20, buttonsize.x, buttonsize.y);
		
		void OnEnable ()
		{
			//Get target MeshFilter
			targetMeshFilter = (target as MeshFilter);
			if (menuExpanded) {
				ejmmesh = new EJMMesh(targetMeshFilter);
				LoadCurrentEditor();
			}
			//LOAD ICONS
			OpenGUI = new GUIContent(Resources.Load("Icons/wrench_icon") as Texture2D, "Edit");
			if (OpenGUI.image == null){OpenGUI.text = "EDIT";}
			OpenUniqueGUI = new GUIContent(Resources.Load("Icons/wrench_plus_icon") as Texture2D, "Edit as Unique");
			if (OpenUniqueGUI.image == null){OpenUniqueGUI.text = "EDIT";}
			CloseGUI = new GUIContent(Resources.Load("Icons/exit_icon") as Texture2D, "Save and Exit");
			if (CloseGUI.image == null){CloseGUI.text = "DONE";}
			ObjectGUI = new GUIContent (Resources.Load("Icons/object_icon") as Texture2D, "Object Tools");
			if (ObjectGUI.image == null){ObjectGUI.text = "OBJ";}
			VertexGUI = new GUIContent (Resources.Load("Icons/vertex_icon") as Texture2D, "Vertex Tools");
			if (VertexGUI.image == null){VertexGUI.text = "VERT";}
			TriangleGUI = new GUIContent (Resources.Load("Icons/triangle_icon") as Texture2D, "Triangle Tools");
			if (TriangleGUI.image == null){TriangleGUI.text = "TRIS";}
			NormalGUI = new GUIContent(Resources.Load("Icons/normal_icon") as Texture2D, "Normal Tools");
			if (NormalGUI.image == null){NormalGUI.text = "NORM";}
			UVsGUI = new GUIContent(Resources.Load("Icons/uv_icon") as Texture2D, "UV Tools");
			if (UVsGUI.image == null){UVsGUI.text = "UVs";}
			PrimitivesGUI = new GUIContent(Resources.Load("Icons/primitives_icon") as Texture2D, "Primitives");
			if (PrimitivesGUI.image == null){PrimitivesGUI.text = "PRIM";}
			RevertGUI = new GUIContent(Resources.Load("Icons/undo_icon") as Texture2D, "Revert Mesh");
			if (RevertGUI.image == null){RevertGUI.text = "UNDO";}
			ToolbarIcons = new GUIContent[6]{ObjectGUI, VertexGUI, TriangleGUI, NormalGUI, UVsGUI, PrimitivesGUI};
		}

		void OnDisable() {EditorTools.HideDefaultHandles = false;}
		
		public override void OnInspectorGUI ()
		{
			DrawDefaultInspector();
			GUILayout.Label ("Using Mesh TK Editor - By EJM Software", "HelpBox");
		}

		public void OnSceneGUI ()
		{
			if (Camera.current == null) {
				return;
			} else if (targetMeshFilter.sharedMesh==null){
				Mesh newmesh = new Mesh();
				newmesh.name = "Empty Mesh";
				targetMeshFilter.mesh = newmesh;
			}
			if (EditingMode > -1){
#if (UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5)
				if (ejmmesh.targettransform.hasChanged){
					ejmmesh.RefreshMatrix ();
				}
#endif
				CurrentMeshEditor.DrawSceneGUI();
				HandleUtility.Repaint();
			}
			Handles.BeginGUI ();
			if (menuExpanded) {
				OpenedWindowRect = GUI.Window (GUIUtility.GetControlID(FocusType.Passive), OpenedWindowRect, OpenedToolbarWindow, "Mesh Toolkit");
			} else {
				ClosedWindowRect = GUI.Window (GUIUtility.GetControlID(FocusType.Passive), ClosedWindowRect, ClosedToolbarWindow, "Mesh Toolkit");
			}
			Handles.EndGUI();
		}
		void ClosedToolbarWindow(int windowID){
			GUI.DragWindow (DragRect);
			if (GUI.Button (new Rect (5, 20, buttonsize.x, buttonsize.y), OpenGUI)) {
				ejmmesh = new EJMMesh(targetMeshFilter);
				//Set previous Mesh
				previousMesh = (Mesh)Instantiate((Object)ejmmesh.SharedMesh);
				previousMaterials = new Material[ejmmesh.mRenderer.sharedMaterials.Length];
				System.Array.Copy(ejmmesh.mRenderer.sharedMaterials, previousMaterials, ejmmesh.mRenderer.sharedMaterials.Length);
				//Hide the default Handles
				EditorTools.HideDefaultHandles = true;
				//Expand the menu
				menuExpanded = true;
			} else if (GUI.Button (new Rect (10+buttonsize.x, 20, buttonsize.x, buttonsize.y), OpenUniqueGUI)){
				ejmmesh = new EJMMesh(targetMeshFilter);
				ejmmesh.MakeUnique();
				//Set previous Mesh
				previousMesh = (Mesh)Instantiate((Object)ejmmesh.SharedMesh);
				previousMaterials = new Material[ejmmesh.mRenderer.sharedMaterials.Length];
				System.Array.Copy(ejmmesh.mRenderer.sharedMaterials, previousMaterials, ejmmesh.mRenderer.sharedMaterials.Length);
				//Hide the default Handles
				EditorTools.HideDefaultHandles = true;
				//Expand the menu
				menuExpanded = true;
			}
		}
		void OpenedToolbarWindow(int windowID){
			GUI.DragWindow (DragRect);
			if (GUI.Button (CloseRect, CloseGUI)) {
				menuExpanded = false;
				EditingMode = -1;
				EditorTools.HideDefaultHandles = false;
			}
			//toolbar
			EditorGUI.BeginChangeCheck();
			EditingMode = GUI.SelectionGrid(ToolbarRect, EditingMode, ToolbarIcons, ToolbarIcons.Length);
			if (GUI.Button (UndoRect, RevertGUI)){
				ejmmesh.ImportData(previousMesh);
				ejmmesh.mRenderer.sharedMaterials = previousMaterials;
			}
			if (EditorGUI.EndChangeCheck()){
				LoadCurrentEditor();
				ejmmesh.UpdateCollider();
			}
		}
		void LoadCurrentEditor() {
			if (EditingMode == 0){CurrentMeshEditor = new ObjectEditor(ejmmesh);}
			else if (EditingMode == 1){CurrentMeshEditor = new VerticesEditor(ejmmesh);}
			else if (EditingMode == 2){CurrentMeshEditor = new TrianglesEditor(ejmmesh);}
			else if (EditingMode == 3){CurrentMeshEditor = new NormalsEditor(ejmmesh);}
			else if (EditingMode == 4){CurrentMeshEditor = new UVsEditor (ejmmesh);}
			else if (EditingMode == 5){CurrentMeshEditor = new PrimitivesEditor(ejmmesh);}
		}
	}
}