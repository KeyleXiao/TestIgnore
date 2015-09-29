using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace MeshTK
{
	/// <summary>
	/// Editor Tools - Copyright EJM Software 2015
	/// Tools to extend the Unity Editor
	/// Note: Organized Alphabetically
	/// </summary>
	public class EditorTools
	{
		private static Texture2D _staticRectTexture;
		private static GUIStyle _staticRectStyle;
		private static Rect _BoxSelectRect;
		private static bool _IsBoxSelecting;
		private static Color BoxSelectColor = new Color(135f / 255f, 203f / 255f, 255f / 255f, 200f / 255f);

		public static Color ColorPicker(Color color)
		{
			//Create a blank texture.
			Texture2D tex = new Texture2D(40,40);

			GUILayout.BeginHorizontal();
			#region Slider block
			GUILayout.BeginVertical("Box");

			//Sliders for rgb variables betwen 0.0 and 1.0
			GUILayout.BeginHorizontal();
			GUILayout.Label("R",GUILayout.Width(12));
			color.r = GUILayout.HorizontalSlider(color.r,0f,1f);
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("G",GUILayout.Width(12));
			color.g = GUILayout.HorizontalSlider(color.g,0f,1f);
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("B",GUILayout.Width(12));
			color.b = GUILayout.HorizontalSlider(color.b,0f,1f);
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			#endregion
			#region Preview block
			GUILayout.BeginVertical("Box", new GUILayoutOption[]{GUILayout.Width(44),GUILayout.Height(44)});
			//Apply color to following label
			GUI.color = color;
			GUILayout.Label(tex);
			//Revert color to white to avoid messing up any following controls.
			GUI.color = Color.white;
			GUILayout.EndVertical();
			#endregion
			GUILayout.EndHorizontal();

			//Finally return the modified value.
			return color;
		}

		public static void DrawGUIRect(Rect position, Color color)
		{
			//<summary>
			//This method draws a Rect to the screen using GUI Space.
			//Note that this function should be called from the OnGUI() function.
			//</summary>
			if( _staticRectTexture == null ){_staticRectTexture = new Texture2D( 1, 1 );}
			if( _staticRectStyle == null ){_staticRectStyle = new GUIStyle();}
			
			_staticRectTexture.SetPixel( 0, 0, color );
			_staticRectTexture.Apply();
			
			_staticRectStyle.normal.background = _staticRectTexture;
			GUI.Box( position, GUIContent.none, _staticRectStyle );
		}

		public static Vector2 GUIToScreenPoint(Vector2 guipoint)
		{
			return new Vector2 (guipoint.x, Camera.current.pixelHeight-guipoint.y);
		}

		public static Rect GUIToScreenRect(Rect guirect)
		{
			return new Rect (guirect.x, Camera.current.pixelHeight-(guirect.y+guirect.height), guirect.width, guirect.height);
		}
		
		public static bool HideDefaultHandles
		{
			//<summary>
			//This bool hides the default handles when set to true
			//</summary>
			get
			{
				Type type = typeof(Tools);
				FieldInfo field = type.GetField("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
				return ((bool)field.GetValue(null));
			}
			set
			{
				Type type = typeof(Tools);
				FieldInfo field = type.GetField("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
				field.SetValue(null, value);
			}
		}
		
		public static Vector3 InverseScale (Vector3 point, Vector3 scale)
		{
			return new Vector3 (point.x/scale.x, point.y/scale.y, point.z/scale.z);
		}
		
		public static float RayTriangleIntersection(Ray ray, Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
		{
			//<summary>
			//Calculates and returns the distance from a ray to a triangle.
			//If the ray does not intersect the triangle it returns -1.
			//</summary>
			
			//Compute vectors along two edges of the triangle.
			Vector3 edge1 = vertex2 - vertex1;
			Vector3 edge2 = vertex3 - vertex1;
			//Cross product of ray direction and edge2 - first part of determinant.
			Vector3 directioncrossedge2 = Vector3.Cross (ray.direction, edge2);
			//Compute the determinant.
			float determinant = Vector3.Dot (edge1, directioncrossedge2);
			//If the ray is parallel to the triangle plane, there is no collision.
			if (determinant > -1e-6f && determinant < 1e-6f){return -1;}
			//Calculate the inverse determinant
			float inversedeterminant = 1.0f / determinant;
			//Calculate the U parameter of the intersection point.
			Vector3 distanceVector = ray.origin - vertex1;
			float triangleU = Vector3.Dot (distanceVector, directioncrossedge2) * inversedeterminant;
			//Make sure it is inside the triangle.
			if (triangleU < 0f || triangleU > 1f){return -1;}
			//Calculate the V parameter of the intersection point.
			Vector3 distancecrossedge1 = Vector3.Cross (distanceVector, edge1);
			float triangleV = Vector3.Dot (ray.direction, distancecrossedge1) * inversedeterminant;
			//Make sure it is inside the triangle.
			if (triangleV < 0f || triangleU + triangleV > 1f){return -1;}
			//Compute the distance along the ray to the triangle.
			float raydistance = Vector3.Dot (edge2, distancecrossedge1) * inversedeterminant;
			//Check if the triangle is behind the ray origin
			if (raydistance < 0f) {return -1;}
			//Return the distance from the ray origin to its intersection with the triangle
			return raydistance;
		}

		public static Rect? TryBoxSelect()
		{
			//<summary>
			//This method trys to box select and returns the selected Rect if something
			//is selected. Otherwise it returns null.
			//</summary>
			if (Event.current.type == EventType.MouseDown) {
				_BoxSelectRect = new Rect (Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0);
				_IsBoxSelecting = true;
			} else if (Event.current.type == EventType.MouseUp) {
				_IsBoxSelecting = false;
				if (_BoxSelectRect.width<0){
					_BoxSelectRect.x += _BoxSelectRect.width;
					_BoxSelectRect.width *= -1;
				}
				if (_BoxSelectRect.height<0){
					_BoxSelectRect.y += _BoxSelectRect.height;
					_BoxSelectRect.height *= -1;
				}
				return _BoxSelectRect;
			} else if (_IsBoxSelecting) {
				_BoxSelectRect.width = Event.current.mousePosition.x - _BoxSelectRect.x;
				_BoxSelectRect.height = Event.current.mousePosition.y - _BoxSelectRect.y;
				Handles.BeginGUI();
				DrawGUIRect(_BoxSelectRect, BoxSelectColor);
				Handles.EndGUI();
			}
			return null;
		}
	}
}