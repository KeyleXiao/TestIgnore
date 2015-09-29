using UnityEngine;
using UnityEditor;

namespace MeshTK
{
	public class PrimitivesEditor : EJMMeshEditor {
		Texture2D triangulationTexture;
		GUILayoutOption[] textureDisplaySize = { GUILayout.ExpandWidth(true), GUILayout.Height(80) };
		//Settings
		int currentPrimitive = 0;
		Vector3 primitiveScale = Vector3.one;
		int circleSides = 10;
		float circleRadius = 1f;
		float maxDeviance = 0.448f;
		//Rects
		Rect WindowRect = new Rect (10, 100, 150, 200);
		readonly Rect DragRect = new Rect (0, 0, 1000, 20);
		#region PUBLIC METHODS
		public PrimitivesEditor(EJMMesh target){
			ejmmesh = target;
			ReloadData ();
		}
		public override void DrawSceneGUI()
		{
			if (Event.current.type == EventType.Layout) {
				HandleUtility.AddDefaultControl (GUIUtility.GetControlID (FocusType.Passive));
			}
			Handles.BeginGUI ();
			WindowRect = GUI.Window (0, WindowRect, this.GUIWindow, "Primitive Tools");
			Handles.EndGUI ();
		}
		public override void ReloadData(){
			return;
		}
		#endregion
		private void GUIWindow (int windowID)
		{
			GUI.DragWindow (DragRect);
			GUI.color = Color.white;
			currentPrimitive = EditorGUILayout.Popup (currentPrimitive, new string[3]{"Rect", "Circle", "From Image"});
			// CREATE PRIMITIVE RECT
			if (currentPrimitive==0){
				primitiveScale = EditorGUILayout.Vector3Field ("Scale", primitiveScale);
				if (GUILayout.Button("Build", EditorStyles.miniButton)){
					ejmmesh.ImportData(PrimitivesTools.CreatePlane(primitiveScale.x, primitiveScale.z));
				}
			}
			// CREATE PRIMITIVE CIRCLE
			if (currentPrimitive == 1) {
				GUILayout.BeginHorizontal();
				GUILayout.Label ("Sides", GUILayout.Width(80f));
				circleSides = EditorGUILayout.IntField(circleSides);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label ("Radius", GUILayout.Width(80f));
				circleRadius = EditorGUILayout.FloatField(circleRadius);
				GUILayout.EndHorizontal();
				if (GUILayout.Button("Build", EditorStyles.miniButton)){
					ejmmesh.ImportData(PrimitivesTools.CreateCircle(circleRadius, circleSides));
				}
			}
			// CREATE PRIMITIVE FROM IMAGE
			if (currentPrimitive == 2) {
				primitiveScale = EditorGUILayout.Vector3Field ("Scale", primitiveScale);
				GUILayout.BeginHorizontal();
				GUILayout.Label ("Simplification", GUILayout.Width(80f));
				maxDeviance = GUILayout.HorizontalSlider(maxDeviance, 0.4f, 0.448f);
				GUILayout.EndHorizontal();
				try {
					triangulationTexture = EditorGUILayout.ObjectField (triangulationTexture, typeof(Texture2D), false, textureDisplaySize) as Texture2D;
				} catch (ExitGUIException) {}
				catch (UnityException) {}
				if (GUILayout.Button("Build", EditorStyles.miniButton)){
					ejmmesh.ImportData(PrimitivesTools.CreateFromImage(triangulationTexture, maxDeviance, primitiveScale.x, primitiveScale.z, Vector3.zero));
				}
			}
		}
	}
}