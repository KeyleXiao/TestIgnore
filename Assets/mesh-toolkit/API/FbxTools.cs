using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; //Use UnityEditor to find the path of the textures if in the editor
#endif
using System.Collections.Generic;
using System.IO;
using System.Linq; // For Enumerable object

namespace MeshTK
{
	/// <summary>
	/// Copyright EJM Software 2015
	/// FbxTools contains methods to export a unity Mesh to an FBX file
	/// This tool is currently in its alpha stage, especially exporting skinning information
	/// </summary>
	public class FbxTools : MonoBehaviour
	{
		/// <summary>
		/// represents a node in an fbx file
		/// </summary>
		class FbxNode {
			public int indentation;
			public string key;
			public string val;
			public string content;
			/// <summary>
			/// initialize an node without bracketed content
			/// </summary>
			public FbxNode(int i, string k, string v){
				indentation = i;
				key = k;
				val = v;
			}
			/// <summary>
			/// initialize an node with bracketed content
			/// </summary>
			public FbxNode(int i, string k, string v, string c){
				indentation = i;
				key = k;
				val = v;
				content = c;
			}
			public override string ToString(){
				string text = new string('\t', indentation) + key + ": " + val;
				if (!string.IsNullOrEmpty(content)){
					text += " {\n" + content + new string('\t', indentation) + "}";
				}
				text += "\n";
				return text;
			}
		}
		/// <summary>
		/// represents a node that has other child nodes
		/// </summary>
		class FbxContainerNode : FbxNode
		{
			public FbxNode[] subnodes;
			public FbxContainerNode(int i, string k, string v, FbxNode[] n) : base (i, k, v, "") {
				subnodes = n;
			}
			public override string ToString(){
				if (subnodes != null) {
					content = subnodes.Aggregate("", (s, i) => s + i.ToString());
				}
				return base.ToString();
			}
		}
		// =================================================== MAIN FUNCTION ================================================
		public static void ExportToFile(SkinnedMeshRenderer renderer, string filename, bool uselhcoords = true) {
			if (string.IsNullOrEmpty (filename))
				return;
			using (StreamWriter sw = new StreamWriter(filename)) {
				Mesh m = renderer.sharedMesh;
				Renderer r = (Renderer)renderer;
				sw.Write(ExportToString(m, ref r, uselhcoords));
			}
		}
		public static void ExportToFile(Mesh mesh, Renderer renderer, string filename, bool uselhcoords = true) {
			if (string.IsNullOrEmpty (filename))
				return;
			using (StreamWriter sw = new StreamWriter(filename)) {
				sw.Write(ExportToString(mesh, ref renderer, uselhcoords));
			}
		}
		public static string ExportToString (Mesh mesh, ref Renderer renderer, bool uselhcoords = true){
#if UNITY_EDITOR
			EditorUtility.DisplayProgressBar("EJM Software FBX Exporter", "Writing header", 0f);
#endif
			string documenttext = "; FBX 6.1.0 project file\n; Copyright (C) 1997-2008 Autodesk Inc. and/or its licensors.\n; All rights reserved.\n; ----------------------------------------------------\n\n";
			documenttext += FBXHeaderExtensionNode ();
			documenttext += "\n\n; Object definitions\n;------------------------------------------------------------------\n\n";
			documenttext += DefinitionsNode (ref mesh, ref renderer).ToString();
			documenttext += "\n\n; Object properties\n;------------------------------------------------------------------\n\n";
			string skinmodelnodes = "", defnodes = "", posenode = "";
			if (renderer.GetType() == typeof(SkinnedMeshRenderer)){
#if UNITY_EDITOR
				EditorUtility.DisplayProgressBar("EJM Software FBX Exporter", "Parsing skinning data", 0.33f);
#endif
				SkinnedMeshRenderer skmr = renderer as SkinnedMeshRenderer;
				skinmodelnodes = SkinnedModelNodes(ref skmr);
				defnodes = DeformationNodes(ref mesh, ref skmr);
				posenode = PoseNode(ref mesh);
			}
#if UNITY_EDITOR
			EditorUtility.DisplayProgressBar("EJM Software FBX Exporter", "Writing mesh data", 0.66f);
#endif
			documenttext += ObjectsNode (ModelNodes (ref mesh, uselhcoords) + skinmodelnodes, MaterialNodes (ref renderer), TextureNodes(ref renderer), defnodes, posenode, GlobalSettingsNode ().ToString());
			documenttext += "\n\n; Object connections\n;------------------------------------------------------------------\n\n";
			documenttext += ConnectionsNode (ref mesh, ref renderer).ToString();
			documenttext += "\n\n;Version 5 settings\n;------------------------------------------------------------------\n\n";
			documenttext += Version5Node ();
#if UNITY_EDITOR
						EditorUtility.ClearProgressBar();
#endif
			return documenttext;
		}
		// =================================================== FIRST LEVEL NODES ================================================
		static string FBXHeaderExtensionNode () { // Also includes Creation Time and Creator Nodes
			System.DateTime date = System.DateTime.Now;
			return "FBXHeaderExtension:  {\n\tFBXHeaderVersion: 1003\n\tFBXVersion: 6100\n\tCreationTimeStamp:  {\n\t\tVersion: 1000\n" +
				string.Format("\t\tYear: {0:yyyy}\n\t\tMonth: {0:MM}\n\t\tDay: {0:dd}\n\t\tHour: {0:HH}\n\t\tMinute: {0:mm}\n\t\tSecond: {0:ss}\n\t\tMillisecond: {0:fff}\n", date) +
				"\t}\n\tCreator: \"EJM Software FBX Exporter\"\n\tOtherFlags:  {\n\t\tFlagPLE: 0\n\t}\n}\n" +
				string.Format ("CreationTime: \"{0:yyyy}-{0:MM}-{0:dd} {0:HH}:{0:mm}:{0:ss}:{0:fff}\"\n", date) +
				"Creator: \"EJM Software FBX Exporter\"";
		}
		static FbxNode DefinitionsNode (ref Mesh mesh, ref Renderer renderer) {
			// calculate the number of subnodes needed
			int subnodecount = 6;
			if (renderer.GetType() == typeof(SkinnedMeshRenderer)){
				subnodecount += 2;
			}
			// calculate the counts
			int ModelCount = 1;
			int MatCount = MaterialNodeCount (ref renderer);
			int TexCount = TextureNodeCount (ref renderer);
			// create the node
			FbxContainerNode node = new FbxContainerNode(0, "Definitions", "", new FbxNode[subnodecount]);
			node.subnodes[0] = new FbxNode(1, "Version", "100");
			node.subnodes[1] = new FbxNode(1, "Count", (ModelCount + MatCount + TexCount + 1).ToString());
			node.subnodes[2] = new FbxContainerNode(2, "ObjectType", "\"Model\"", new FbxNode[1]{new FbxNode(3, "Count", ModelCount.ToString())});
			node.subnodes[3] = new FbxContainerNode(2, "ObjectType", "\"Material\"", new FbxNode[1]{new FbxNode(3, "Count", MatCount.ToString())});
			node.subnodes[4] = new FbxContainerNode(2, "ObjectType", "\"Texture\"", new FbxNode[1]{new FbxNode(3, "Count", TexCount.ToString())});
			if (renderer.GetType() == typeof(SkinnedMeshRenderer)){
				SkinnedMeshRenderer skinnedrenderer = renderer as SkinnedMeshRenderer;
				node.subnodes[5] = new FbxContainerNode(2, "ObjectType", "\"Deformer\"", new FbxNode[1]{new FbxNode(3, "Count", skinnedrenderer.bones.Length.ToString())});
				node.subnodes[6] = new FbxContainerNode(2, "ObjectType", "\"Pose\"", new FbxNode[1]{new FbxNode(3, "Count", "1")});
			}
			node.subnodes[subnodecount - 1] = new FbxContainerNode(2, "ObjectType", "\"GlobalSettings\"", new FbxNode[1]{new FbxNode(3, "Count", "1")});// There is always 1 global settings node
			return node;
		}
		static string ObjectsNode (params string[] nodes) {
			return "Objects:  {\n" +
			  string.Join("\n", nodes) +
				"\n}\n";
		}
		static string PoseNode (ref Mesh mesh) {
			string nodetext = "\tPose: \"Pose::BIND_POSES\", \"BindPose\" {\n\t\tType: \"BindPose\"\n\t\tVersion: 100\n";
			nodetext += "\t\tNbPoseNodes: " + mesh.bindposes.Length.ToString() + "\n";
			for (int i=0;i<mesh.bindposes.Length;i++){
				nodetext += "\t\tPoseNode:  {\n";
				nodetext += "\t\t\tNode: \"Model::Bone_" + i.ToString() + "\"\n";
				nodetext += "\t\t\tMatrix: ";
				for (int j=0;j<16;j++){
					if (j>0){nodetext += ",";}
					nodetext += mesh.bindposes[i][j%4, j/4].ToString("n8");
				}
				nodetext += "\n";
				nodetext += "\t\t}\n";
			}
			nodetext += "\t}\n";
			return nodetext;
		}
		static FbxNode ConnectionsNode (ref Mesh mesh, ref Renderer renderer) {
			List<FbxNode> subnodes = new List<FbxNode>();
			//string nodetext = "Connections:  {\n";
			//nodetext += "\tConnect: \"OO\", \"Model::" + mesh.name + "\", \"Model::Scene\"\n";
			subnodes.Add(new FbxNode(1, "Connect", "\"OO\", \"Model::" + mesh.name + "\", \"Model::Scene\""));
			foreach(Material mat in renderer.sharedMaterials){
				// Skip if the material is not defined
				if (mat==null) continue;
				// Add the material connection
				subnodes.Add(new FbxNode(1, "Connect", "\"OO\", \"Material::" + mat.name + "\", \"Model::" + mesh.name + "\""));
				//nodetext += "\tConnect: \"OO\", \"Material::" + mat.name + "\", \"Model::" + mesh.name + "\"\n";
				// Add textures
				if (mat.HasProperty("_MainTex")){
					//nodetext += "\tConnect: \"OO\", \"Texture::" + mat.name + "_diff" + "\", \"Model::" + mesh.name + "\"\n";
					subnodes.Add(new FbxNode(1, "Connect", "\"OO\", \"Texture::" + mat.name + "_diff" + "\", \"Model::" + mesh.name + "\""));
				}
				if (mat.HasProperty("_BumpMap")){
					//nodetext += "\tConnect: \"OO\", \"Texture::" + mat.name + "_bump" + "\", \"Model::" + mesh.name + "\"\n";
					subnodes.Add(new FbxNode(1, "Connect", "\"OO\", \"Texture::" + mat.name + "_bump" + "\", \"Model::" + mesh.name + "\""));
				}
			}
			if (renderer.GetType() == typeof(SkinnedMeshRenderer)){
				SkinnedMeshRenderer skinnedrenderer = renderer as SkinnedMeshRenderer;
				//nodetext += new FbxNode(1, "Connect", "\"OO\", \"Deformer::Skin " + mesh.name + "\", \"Model::" + mesh.name + "\"").ToString();
				subnodes.Add(new FbxNode(1, "Connect", "\"OO\", \"Deformer::Skin " + mesh.name + "\", \"Model::" + mesh.name + "\""));
				foreach (Transform bone in skinnedrenderer.bones){
					//nodetext += new FbxNode(1, "Connect", "\"OO\", \"SubDeformer::Cluster " + mesh.name + " " + bone.name + "\", \"Deformer::Skin " + mesh.name + "\"").ToString();
					subnodes.Add(new FbxNode(1, "Connect", "\"OO\", \"SubDeformer::Cluster " + mesh.name + " " + bone.name + "\", \"Deformer::Skin " + mesh.name + "\""));
				}
				foreach (Transform bone in skinnedrenderer.bones){
					//nodetext += new FbxNode(1, "Connect", "\"OO\", \"Model::" + bone.name + "\", \"SubDeformer::Cluster " + mesh.name + " " + bone.name + "\"").ToString();
					subnodes.Add(new FbxNode(1, "Connect", "\"OO\", \"Model::" + bone.name + "\", \"SubDeformer::Cluster " + mesh.name + " " + bone.name + "\""));
				}
				// Add the heiarchy of models
				List<Transform> boneList = skinnedrenderer.bones.ToList();
				while (boneList.Count > 0){
					for (int i=0;i<boneList.Count;i++){
						Transform parent = boneList[i].parent;
						if (!boneList.Contains(parent)){
							if (System.Array.Exists(skinnedrenderer.bones, p=>p==parent)){
								//nodetext += new FbxNode(1, "Connect", "\"OO\", \"Model::" + boneList[i].name + "\", \"Model::" + parent.name + "\"").ToString();
								subnodes.Add(new FbxNode(1, "Connect", "\"OO\", \"Model::" + boneList[i].name + "\", \"Model::" + parent.name + "\""));
							} else {
								//nodetext += new FbxNode(1, "Connect", "\"OO\", \"Model::" + boneList[i].name + "\", \"Model::Scene\"").ToString();
								subnodes.Add(new FbxNode(1, "Connect", "\"OO\", \"Model::" + boneList[i].name + "\", \"Model::Scene\""));
							}
							boneList.RemoveAt(i);
						}
					}
				}
			}
			//nodetext += "}";
			//return nodetext;
			return new FbxContainerNode(0, "Connections", "", subnodes.ToArray());
		}
		static FbxNode Version5Node () {
			return new FbxNode(0, "Version5", "", "\tAmbientRenderSettings:  {\n\t\tVersion: 101\n\t\tAmbientLightColor: 0.0,0.0,0.0,0\n\t}\n" +
				"\tFogOptions:  {\n\t\tFogEnable: 0\n\t\tFogMode: 0\n\t\tFogDensity: 0.000\n\t\tFogStart: 5.000\n\t\tFogEnd: 25.000\n\t\tFogColor: 0.1,0.1,0.1,1\n\t}\n" +
				"\tSettings:  {\n\t\tFrameRate: \"24\"\n\t\tTimeFormat: 1\n\t\tSnapOnFrames: 0\n\t\tReferenceTimeIndex: -1\n\t\tTimeLineStartTime: 0\n\t\tTimeLineStopTime: 479181389250\n\t}\n" +
				"\tRendererSetting:  {\n\t\tDefaultCamera: \"Producer Perspective\"\n\t\tDefaultViewingMode: 0\n\t}\n");
		}
		// =================================================== SECOND LEVEL NODES ================================================
		static string ModelNodes (ref Mesh mesh, bool uselhcoords = true) {
			// DETERMINE WHAT DATA WILL BE COPIED
			//bool hasuv = (mesh.uv.Length > 0);
			bool hasuv2 = (mesh.uv2.Length > 0);
#if UNITY_5
			bool hasuv3 = (mesh.uv3.Length > 0);
			bool hasuv4 = (mesh.uv4.Length > 0);
#endif
			bool hascolors = (mesh.colors.Length > 0);
			// (((1))) LOAD VERTEX DATA INTO STRINGS
			string verts = "";
			string normals = "";
			string colors = "";
			string uv1 = "";
			string uv2 = "";
			string uv3 = "";
			string uv4 = "";
			for (int i=0; i<mesh.vertexCount; i++) {
				if (i!=0){verts+=",";normals+=",";colors+=",";uv1+=",";uv2+=",";uv3+=",";uv4+=",";}
				verts += string.Format("{0:n6},{1:n6},{2:n6}", mesh.vertices[i].x * (uselhcoords ? -1 : 1), mesh.vertices[i].y, mesh.vertices[i].z);
				normals += string.Format("{0:n6},{1:n6},{2:n6}", mesh.normals[i].x * (uselhcoords ? -1 : 1), mesh.normals[i].y, mesh.normals[i].z);
				if (hascolors){colors += string.Format ("{0:n6},{1:n6},{2:n6},{3:n6}", mesh.colors[i].r, mesh.colors[i].g, mesh.colors[i].b, mesh.colors[i].a);}
				uv1 += mesh.uv[i].x.ToString("n6") + "," + mesh.uv[i].y.ToString("n6");
				if (hasuv2){uv2 += mesh.uv2[i].x.ToString("n6") + "," + mesh.uv2[i].y.ToString("n6");}
#if UNITY_5
				if (hasuv3){uv3 += mesh.uv3[i].x.ToString("n6") + "," + mesh.uv3[i].y.ToString("n6");}
				if (hasuv4){uv4 += mesh.uv4[i].x.ToString("n6") + "," + mesh.uv4[i].y.ToString("n6");}
#endif
			}
			// (((2))) CREATE THE NODES
			// Create the PolygonVertexIndex node
			string PolygonVertexIndex = "\n\t\tPolygonVertexIndex: ";
			for (int i=0; i<mesh.triangles.Length; i+=3) {
				if (i!=0){PolygonVertexIndex+=",";}
				PolygonVertexIndex += string.Format((uselhcoords ? "{1},{0},{2}" : "{0},{1},{2}"), mesh.triangles[i], mesh.triangles[i+1], -1-mesh.triangles[i+2]);
			}
			// Create the LayerElementNormal node
			string LayerElementNormal = "\n\t\tLayerElementNormal: 0 {\n\t\t\tVersion: 101\n\t\t\tName: \"\"\n\t\t\tMappingInformationType: \"ByVertice\"\n\t\t\tReferenceInformationType: \"Direct\"\n\t\t\tNormals: " + normals + "\n\t\t}";
			// Create the LayerElementColor node
			string LayerElementColor = "";
			if (hascolors){
				LayerElementColor = "\n\t\tLayerElementColor: 0 {\n\t\t\tVersion: 101\n\t\t\tName: \"Col\"\n\t\t\tMappingInformationType: \"ByVertice\"\n\t\t\tReferenceInformationType: \"Direct\"\n\t\t\tColors: " + colors + "\n\t\t}";
			}
			// Create the LayerElementUV node
			string LayerElementUV = "\n\t\tLayerElementUV: 0 {\n\t\t\tVersion: 101\n\t\t\tName: \"UVMap\"\n\t\t\tMappingInformationType: \"ByVertice\"\n\t\t\tReferenceInformationType: \"Direct\"\n\t\t\tUV: ";
			LayerElementUV += uv1 + "\n\t\t}";
			// Create the LayerElementUV2 node
			string LayerElementUV2 = "";
			if (hasuv2){
				LayerElementUV2 = "\n\t\tLayerElementUV: 1 {\n\t\t\tVersion: 101\n\t\t\tName: \"UVMap_2\"\n\t\t\tMappingInformationType: \"ByVertice\"\n\t\t\tReferenceInformationType: \"Direct\"\n\t\t\tUV: " + uv2 + "\n\t\t}";
			}
#if UNITY_5
			// Create the LayerElementUV3 node
			string LayerElementUV3 = "";
			if (hasuv3) {
				LayerElementUV3 = "\n\t\tLayerElementUV: 2 {\n\t\t\tVersion: 101\n\t\t\tName: \"UVMap_3\"\n\t\t\tMappingInformationType: \"ByVertice\"\n\t\t\tReferenceInformationType: \"Direct\"\n\t\t\tUV: " + uv3 + "\n\t\t}";
			}
			// Create the LayerElementUV4 node
			string LayerElementUV4 = "";
			if (hasuv4) {
				LayerElementUV4 = "\n\t\tLayerElementUV: 3 {\n\t\t\tVersion: 101\n\t\t\tName: \"UVMap_4\"\n\t\t\tMappingInformationType: \"ByVertice\"\n\t\t\tReferenceInformationType: \"Direct\"\n\t\t\tUV: " + uv4 + "\n\t\t}";
			}
#endif
			// Create the LayerElementTexture node
			string LayerElementTexture = "\n\t\tLayerElementTexture: 0 {\n\t\t\tVersion: 101\n\t\t\tName: \"\"\n\t\t\tMappingInformationType: \"NoMappingInformation\"\n\t\t\tReferenceInformationType: \"IndexToDirect\"\n\t\t\tBlendMode: \"Translucent\"\n\t\t\tTextureAlpha: 1\n\t\t\tTextureId: \n\t\t}";
			// Create the LayerElementMaterial node
			string LayerElementMaterial = "\n\t\tLayerElementMaterial: 0 {\n\t\t\tVersion: 101\n\t\t\tName: \"\"\n\t\t\tMappingInformationType: \"ByPolygon\"\n\t\t\tReferenceInformationType: \"IndexToDirect\"\n\t\t\tMaterials: ";
			for (int i=0; i<mesh.subMeshCount; i++) {
				if (i!=0){LayerElementMaterial+=",";}
				LayerElementMaterial += string.Join(",", Enumerable.Repeat(i.ToString(), mesh.GetTriangles(i).Length/3).ToArray());
			}
			LayerElementMaterial += "\n\t\t}";
			// Create the Layer0 node
			string Layer0 = "\n\t\tLayer: 0 {" +
				"\n\t\t\tVersion: 100" +
				"\n\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementNormal\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}" +
				"\n\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementSmoothing\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}" +
				"\n\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementUV\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}" +
				(hascolors?"\n\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementColor\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}":"") +
				"\n\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementTexture\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}" +
				"\n\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementMaterial\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}" +
				"\n\t\t}";
			// Create the Layer1 node
			string Layer1 = "";
			if (hasuv2){
				Layer1 = "\n\t\tLayer: 1 {\n\t\t\tVersion: 100\n\t\t\tLayerElement:  {\n\t\t\t\ttype: \"LayerElementUV\"\n\t\t\t\tTypedIndex: 1\n\t\t\t}\n\t\t}";
			}
#if UNITY_5
			// Create the Layer2 node
			string Layer2 = "";
			if (hasuv3){
				Layer2 = "\n\t\tLayer: 2 {\n\t\t\tVersion: 100\n\t\t\tLayerElement:  {\n\t\t\t\ttype: \"LayerElementUV\"\n\t\t\t\tTypedIndex: 2\n\t\t\t}\n\t\t}";
			}
			// Create the Layer3 node
			string Layer3 = "";
			if (hasuv4){
				Layer3 = "\n\t\tLayer: 3 {\n\t\t\tVersion: 100\n\t\t\tLayerElement:  {\n\t\t\t\ttype: \"LayerElementUV\"\n\t\t\t\tTypedIndex: 3\n\t\t\t}\n\t\t}";
			}
#endif
			// create and return the node text
			return "\tModel: \"Model::" + mesh.name + "\", \"Mesh\" {" +
				"\n\t\tVersion: 232" +
				"\n\t\tProperties60:  {" +
				"\n\t\t\tProperty: \"GeometricScaling\", \"Vector3D\", \"\",1,1,1" +
				"\n\t\t\tProperty:\"Show\",\"bool\",\"\",1" +
				"\n\t\t\tProperty:\"NegativePercentShapeSupport\",\"bool\",\"\",1" +
				"\n\t\t\tProperty:\"DefaultAttributeIndex\",\"int\",\"\",0" +
				"\n\t\t\tProperty:\"Visibility\",\"Visibility\",\"A+\",1" +
				"\n\t\t\tProperty:\"Color\",\"ColorRGB\",\"N\",0.23921568627451,0.52156862745098,0.0235294117647059" +
				"\n\t\t\tProperty: \"BBoxMin\",\"Vector3D\",\"N\",0,0,0" +
				"\n\t\t\tProperty: \"BBoxMax\",\"Vector3D\",\"N\",0,0,0" +
				"\n\t\t}" +
				"\n\t\tMultiLayer: 1" +
				"\n\t\tMultiTake: 1" +
				"\n\t\tShading: T" +
				"\n\t\tCulling: \"CullingOff\"" +
				"\n\t\tVertices: " + verts +
				PolygonVertexIndex +
				"\n\t\tGeometryVersion: 124" +
				LayerElementNormal +
				LayerElementColor +
				"\n\t\tLayerElementSmoothing: 0 {" +
				"\n\t\t\tVersion: 102" +
				"\n\t\t\tName: \"\"" +
				"\n\t\t\tMappingInformationType: \"ByPolygon\"" +
				"\n\t\t\tReferenceInformationType: \"Direct\"" +
				"\n\t\t\tSmoothing: 1" +
				"\n\t\t}" +
				LayerElementUV +
				LayerElementUV2 +
#if UNITY_5
				LayerElementUV3 +
				LayerElementUV4 +
#endif
				LayerElementTexture +
				LayerElementMaterial +
				Layer0 +
				Layer1 +
#if UNITY_5
				Layer2 +
				Layer3 +
#endif
				"\n\t}\n";
		}
		static Vector3 UnityToFbx(Vector3 rotation){
			// flip the rotation over the YZ plane
			Vector3 rot = new Vector3(rotation.x, -rotation.y, -rotation.z);
			// create the quaternion rotation using Unity's rotation order
			Quaternion q = Quaternion.Euler(rot.x, rot.y, rot.z);
			// calculate the heading pitch and roll from the quaternion
			float heading, pitch, roll;
			if (q.x*q.y + q.z*q.w > 0.4999f) { // singularity at north pole
				heading = 2f * Mathf.Atan2(q.x,q.w) * Mathf.Rad2Deg;
				pitch = 90f;
				roll = 0f;
			} else if (q.x*q.y + q.z*q.w < -0.4999f) { // singularity at south pole
				heading = -2f * Mathf.Atan2(q.x,q.w) * Mathf.Rad2Deg;
				pitch = -90f;
				roll = 0f;
			} else { // standard calculation
				heading = Mathf.Atan2(2f*q.y*q.w-2f*q.x*q.z , 1f - 2f*q.y*q.y - 2f*q.z*q.z) * Mathf.Rad2Deg;
				pitch = Mathf.Asin(2f*q.x*q.y + 2f*q.z*q.w) * Mathf.Rad2Deg;
				roll = Mathf.Atan2(2f*q.x*q.w-2f*q.y*q.z , 1f - 2f*q.x*q.x - 2f*q.z*q.z) * Mathf.Rad2Deg;
			}
			// pack the heading, pitch, and roll into the FBX Euler angles format
			return new Vector3(roll, heading, pitch);
		}
		static string SkinnedModelNodes (ref SkinnedMeshRenderer renderer){
			string nodetext = "";
			foreach (Transform bone in renderer.bones){
				Vector3 angles = UnityToFbx(bone.localEulerAngles);
				nodetext += "\tModel: \"Model::" + bone.name + "\", \"Limb\" {\n" +
					"\t\tVersion: 232\n" +
					"\t\tProperties60:  {\n" +
					"\t\t\tProperty: \"Visibility\", \"Visibility\", \"A+\",1\n" +
					"\t\t\tProperty: \"Lcl Translation\", \"Lcl Translation\", \"A+\"," + (-bone.localPosition.x).ToString("n15") + "," + bone.localPosition.y.ToString("n15") + "," + bone.localPosition.z.ToString("n15") + "\n" +
					"\t\t\tProperty: \"Lcl Rotation\", \"Lcl Rotation\", \"A+\"," + angles.x.ToString("n15") + "," + angles.y.ToString("n15") + "," + angles.z.ToString("n15") + "\n" +
					"\t\t\tProperty: \"Lcl Scaling\", \"Lcl Scaling\", \"A+\"," + bone.localScale.x.ToString("n15") + "," + bone.localScale.y.ToString("n15") + "," + bone.localScale.z.ToString("n15") + "\n" +
					"\t\t}\n" +
					"\t\tMultiLayer: 0\n" +
					"\t\tCulling: \"CullingOff\"\n" +
					"\t\tTypeFlags: \"Skeleton\"\n" +
					"\t}\n";
			}
			return nodetext;
		}
		static string MaterialNodes (ref Renderer renderer) {
			string node = "";
			foreach (Material mat in renderer.sharedMaterials){
				// Skip if the material is not defined
				if (mat==null) continue;
				// Check if a main color is defined
				Color col = Color.white;
				if (mat.HasProperty("_Color")){
					col = mat.color;
				}
				// Add the node text
				node += "\tMaterial: \"Material::" + mat.name + "\", \"\" {\n" +
					"\t\tVersion: 102\n" +
					"\t\tShadingModel: \"lambert\"\n" +
					"\t\tMultiLayer: 0\n" +
					"\t\tProperties60:  {\n" +
					"\t\t\tProperty: \"ShadingModel\", \"KString\", \"\", \"Lambert\"\n" +
					"\t\t\tProperty: \"MultiLayer\", \"bool\", \"\",0\n" +
					"\t\t\tProperty: \"EmissiveColor\", \"ColorRGB\", \"\",0.8000,0.8000,0.8000\n" +
					"\t\t\tProperty: \"EmissiveFactor\", \"double\", \"\",0.0000\n" +
					"\t\t\tProperty: \"AmbientColor\", \"ColorRGB\", \"\"," + col.r.ToString("n4") + "," + col.g.ToString("n4") + "," + col.b.ToString("n4") + "\n" +
					"\t\t\tProperty: \"AmbientFactor\", \"double\", \"\",1.0000\n" +
					"\t\t\tProperty: \"DiffuseColor\", \"ColorRGB\", \"\"," + col.r.ToString("n4") + "," + col.g.ToString("n4") + "," + col.b.ToString("n4") + "\n" +
					"\t\t\tProperty: \"DiffuseFactor\", \"double\", \"\",0.8000\n" +
					"\t\t\tProperty: \"Bump\", \"Vector3D\", \"\",0,0,0\n" +
					"\t\t\tProperty: \"TransparentColor\", \"ColorRGB\", \"\",1,1,1\n" +
					"\t\t\tProperty: \"TransparencyFactor\", \"double\", \"\",0.0000\n" +
					"\t\t\tProperty: \"SpecularColor\", \"ColorRGB\", \"\",1.0000,1.0000,1.0000\n" +
					"\t\t\tProperty: \"SpecularFactor\", \"double\", \"\",0.5000\n" +
					"\t\t\tProperty: \"ShininessExponent\", \"double\", \"\",12.3\n" +
					"\t\t\tProperty: \"ReflectionColor\", \"ColorRGB\", \"\",0,0,0\n" +
					"\t\t\tProperty: \"ReflectionFactor\", \"double\", \"\",1\n" +
					"\t\t\tProperty: \"Emissive\", \"ColorRGB\", \"\",0,0,0\n" +
					"\t\t\tProperty: \"Ambient\", \"ColorRGB\", \"\",1.0,1.0,1.0" + col.r.ToString("n1") + "," + col.g.ToString("n1") + "," + col.b.ToString("n1") + "\n" +
					"\t\t\tProperty: \"Diffuse\", \"ColorRGB\", \"\",0.8,0.8,0.8" + col.r.ToString("n1") + "," + col.g.ToString("n1") + "," + col.b.ToString("n1") + "\n" +
					"\t\t\tProperty: \"Specular\", \"ColorRGB\", \"\",1.0,1.0,1.0\n" +
					"\t\t\tProperty: \"Shininess\", \"double\", \"\",12.3\n" +
					"\t\t\tProperty: \"Opacity\", \"double\", \"\",1.0\n" +
					"\t\t\tProperty: \"Reflectivity\", \"double\", \"\",0\n" +
					"\t\t}\n" +
					"\t}\n";
			}
			return node;
		}
		static int MaterialNodeCount(ref Renderer renderer){
			int counter = 0;
			foreach (Material mat in renderer.sharedMaterials){
				if (mat != null) counter ++;
			}
			return counter;
		}
		static string TextureNodes (ref Renderer renderer) {
			string nodetext = "";
			foreach (Material mat in renderer.sharedMaterials){
				// Skip if the material is not defined
				if (mat==null) continue;
				// Add the main texture
				//Texture bumpMap = mat.GetTexture ("_BumpMap");
				if (mat.HasProperty("_MainTex")){
					nodetext += "\tTexture: \"Texture::" + mat.name + "_diff" + "\", \"\" {\n" +
						"\t\tType: \"TextureVideoClip\"\n" +
						"\t\tVersion: 202\n" +
						"\t\tTextureName: \"Texture::" + mat.name + "_diff" + "\"\n" +
						"\t\tProperties60:  {\n" +
						"\t\t\tProperty: \"TextureTypeUse\", \"enum\", \"\",0\n" +
						"\t\t\tProperty: \"Texture alpha\", \"Number\", \"A+\",1\n" +
						"\t\t\tProperty: \"CurrentMappingType\", \"enum\", \"\",0\n" +
						"\t\t\tProperty: \"WrapModeU\", \"enum\", \"\",0\n" +
						"\t\t\tProperty: \"WrapModeV\", \"enum\", \"\",0\n" +
						"\t\t\tProperty: \"UVSwap\", \"bool\", \"\",0\n" +
						"\t\t\tProperty: \"Translation\", \"Vector\", \"A+\"," + mat.mainTextureOffset.x.ToString() + "," + mat.mainTextureOffset.y.ToString() + ",0\n" +
						"\t\t\tProperty: \"Rotation\", \"Vector\", \"A+\",0,0,0\n" +
						"\t\t\tProperty: \"Scaling\", \"Vector\", \"A+\"," + mat.mainTextureScale.x.ToString() + "," + mat.mainTextureScale.y.ToString() + ",1\n" +
						"\t\t\tProperty: \"TextureRotationPivot\", \"Vector3D\", \"\",0,0,0\n" +
						"\t\t\tProperty: \"TextureScalingPivot\", \"Vector3D\", \"\",0,0,0\n" +
						"\t\t\tProperty: \"UseMaterial\", \"bool\", \"\",1\n" +
						"\t\t\tProperty: \"UseMipMap\", \"bool\", \"\",0\n" +
						"\t\t\tProperty: \"CurrentTextureBlendMode\", \"enum\", \"\",1\n" +
						"\t\t\tProperty: \"UVSet\", \"KString\", \"\", \"UVChannel_1\"\n" +
						"\t\t}\n" +
#if UNITY_EDITOR
						"\t\tFileName: \"" + (Application.dataPath + AssetDatabase.GetAssetPath(mat.GetTexture("_MainTex"))) + "\"\n" +
#endif
						"\t\tModelUVTranslation: 0,0\n" +
						"\t\tModelUVScaling: 1,1\n" +
						"\t\tTexture_Alpha_Source: \"None\"\n" +
						"\t\tCropping: 0,0,0,0\n" +
						"\t}\n";
				}
				// Add the bump map
				if (mat.HasProperty("_BumpMap")){
					nodetext += "\tTexture: \"Texture::" + mat.name + "_bump" + "\", \"\" {\n" +
					"\t\tType: \"TextureVideoClip\"\n" +
					"\t\tVersion: 202\n" +
					"\t\tTextureName: \"Texture::" + mat.name + "_bump" + "\"\n" +
					"\t\tProperties60:  {\n" +
					"\t\t\tProperty: \"TextureTypeUse\", \"enum\", \"\",0\n" +
					"\t\t\tProperty: \"Texture alpha\", \"Number\", \"A+\",1\n" +
					"\t\t\tProperty: \"CurrentMappingType\", \"enum\", \"\",0\n" +
					"\t\t\tProperty: \"WrapModeU\", \"enum\", \"\",0\n" +
					"\t\t\tProperty: \"WrapModeV\", \"enum\", \"\",0\n" +
					"\t\t\tProperty: \"UVSwap\", \"bool\", \"\",0\n" +
					"\t\t\tProperty: \"Translation\", \"Vector\", \"A+\"," + mat.GetTextureOffset("_BumpMap").x.ToString() + "," + mat.GetTextureOffset("_BumpMap").y.ToString() + ",0\n" +
					"\t\t\tProperty: \"Rotation\", \"Vector\", \"A+\",0,0,0\n" +
					"\t\t\tProperty: \"Scaling\", \"Vector\", \"A+\"," + mat.GetTextureScale("_BumpMap").x.ToString() + "," + mat.GetTextureScale("_BumpMap").y.ToString() + ",1\n" +
					"\t\t\tProperty: \"TextureRotationPivot\", \"Vector3D\", \"\",0,0,0\n" +
					"\t\t\tProperty: \"TextureScalingPivot\", \"Vector3D\", \"\",0,0,0\n" +
					"\t\t\tProperty: \"UseMaterial\", \"bool\", \"\",1\n" +
					"\t\t\tProperty: \"UseMipMap\", \"bool\", \"\",0\n" +
					"\t\t\tProperty: \"CurrentTextureBlendMode\", \"enum\", \"\",1\n" +
					"\t\t\tProperty: \"UVSet\", \"KString\", \"\", \"UVChannel_1\"\n" +
					"\t\t}\n" +
#if UNITY_EDITOR
					"\t\tFileName: \"" + (Application.dataPath + AssetDatabase.GetAssetPath(mat.GetTexture ("_BumpMap"))) + "\"\n" +
#endif
					"\t\tModelUVTranslation: 0,0\n" +
					"\t\tModelUVScaling: 1,1\n" +
					"\t\tTexture_Alpha_Source: \"None\"\n" +
					"\t\tCropping: 0,0,0,0\n" +
					"\t}\n";
				}
			}
			return nodetext;
		}
		static int TextureNodeCount(ref Renderer renderer){
			int counter = 0;
			foreach (Material mat in renderer.sharedMaterials){
				if (mat.HasProperty("_MainTex")) counter++;
				if (mat.HasProperty("_BumpMap")) counter++;
			}
			return counter;
		}
		static string DeformationNodes(ref Mesh mesh, ref SkinnedMeshRenderer renderer) {
			string text = "";
			FbxContainerNode skinnode = new FbxContainerNode(1, "Deformer", "\"Deformer::Skin " + mesh.name + "\", \"Skin\"", new FbxNode[4]);
			skinnode.subnodes[0] = new FbxNode(2, "Version", "100");
			skinnode.subnodes[1] = new FbxNode(2, "Multilayer", "0");
			skinnode.subnodes[2] = new FbxNode(2, "Type", "\"Skin\"");
			skinnode.subnodes[3] = new FbxNode(2, "Link_DeformAcuracy", "50");
			text += skinnode.ToString();
			for (int i=0;i<renderer.bones.Length; i++){
				FbxContainerNode node = new FbxContainerNode(1, "Deformer", "\"SubDeformer::Cluster " + mesh.name + " " + renderer.bones[i].name + "\", \"Cluster\"", new FbxNode[6]);
				node.subnodes[0] = new FbxNode(2, "Version", "100");
				node.subnodes[1] = new FbxNode(2, "MultiLayer", "0");
				node.subnodes[2] = new FbxNode(2, "Type", "Cluster");
				node.subnodes[3] = new FbxNode(2, "Indexes", "");
				node.subnodes[4] = new FbxNode(2, "Weights", "");
				for (int j=0;j<mesh.vertexCount;j++){
					BoneWeight weights = mesh.boneWeights[j];
					if (weights.boneIndex0 == i){
						node.subnodes[3].val += j.ToString() + ",";
						node.subnodes[4].val += weights.weight0 + ",";
					} else if (weights.boneIndex1 == i){
						node.subnodes[3].val += j.ToString() + ",";
						node.subnodes[4].val += weights.weight1.ToString("n6") + ",";
					} else if (weights.boneIndex2 == i){
						node.subnodes[3].val += j.ToString() + ",";
						node.subnodes[4].val += weights.weight2.ToString("n6") + ",";
					} else if (weights.boneIndex3 == i){
						node.subnodes[3].val += j.ToString() + ",";
						node.subnodes[4].val += weights.weight3.ToString("n6") + ",";
					}
				}
				node.subnodes[5] = new FbxNode(2, "Transform", "");
				for (int j=0;j<16;j++){
					int x = System.Array.Exists(new int[]{1,2,3,4,8,12}, p=>p==j)?-1:1;
					node.subnodes[5].val += (x*mesh.bindposes[i][j]).ToString("n15") + ",";
				}
				node.subnodes[3].val = node.subnodes[3].val.TrimEnd(',');
				node.subnodes[4].val = node.subnodes[4].val.TrimEnd(',');
				node.subnodes[5].val = node.subnodes[5].val.TrimEnd(',');
				text += node.ToString();
			}
			return text;
		}
		static FbxNode GlobalSettingsNode () {
			return new FbxNode(0, "GlobalSettings", "", "\t\tVersion: 1000\n" +
				"\t\tProperties60:  {\n" +
				"\t\t\tProperty: \"UpAxis\", \"int\", \"\",1\n" +
				"\t\t\tProperty: \"UpAxisSign\", \"int\", \"\",1\n" +
				"\t\t\tProperty: \"FrontAxis\", \"int\", \"\",2\n" +
				"\t\t\tProperty: \"FrontAxisSign\", \"int\", \"\",1\n" +
				"\t\t\tProperty: \"CoordAxis\", \"int\", \"\",0\n" +
				"\t\t\tProperty: \"CoordAxisSign\", \"int\", \"\",1\n" +
				"\t\t\tProperty: \"UnitScaleFactor\", \"double\", \"\",100\n" + // Unity unit is one meter
				"\t\t}\n");
		}
	}
}
