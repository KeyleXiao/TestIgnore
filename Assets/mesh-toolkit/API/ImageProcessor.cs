using UnityEngine;
using System.Collections;

namespace MeshTK
{
	/// <summary>
	/// Image Processor - Copyright EJM Software 2015
	/// Process images to detect edges which can be used to create mesh objects
	/// </summary>
	class ImageProcessor
	{	
		ArrayList edges;
		ArrayList points;
		ArrayList polygons;
		
		public ImageProcessor(Color[] pixels, int imageWidth, int imageHeight, float threshold)
		{	
			edges = new ArrayList();
			points = new ArrayList();
			polygons = new ArrayList();
			
			// -- Iterate through the pixels
			for (int x = 0; x < imageWidth; x++) {
				float uvX = x + 0.5f;
				for (int y = 0; y < imageHeight; y++) {
					float uvY = y+0.5f;
					
					// Exit early if the current pixel is not opaque
					if (pixels[x + (imageWidth * y)].a < threshold) continue;
					
					// Get the surrounding pixels' alpha values using pixel space (origin is bottom left)
					float UpAlpha = 0.0f;
					float DownAlpha = 0.0f;
					float RightAlpha = 0.0f;
					float LeftAlpha = 0.0f;
					float UpRightAlpha = 0.0f;
					float UpLeftAlpha = 0.0f;
					float DownRightAlpha = 0.0f;
					
					if (x>0){
						LeftAlpha = pixels[x - 1 + (imageWidth * y)].a;
						if (y<imageHeight-1){
							UpLeftAlpha = pixels[x - 1 + (imageWidth * (y + 1))].a;
						}
					}
					if (x<imageWidth-1){
						if (y>0){
							DownRightAlpha = pixels[x + 1 + (imageWidth * (y - 1))].a;
						}
						RightAlpha = pixels[x + 1 + (imageWidth * y)].a;
						if (y<imageHeight-1){
							UpRightAlpha = pixels[x + 1 + (imageWidth * (y + 1))].a;
						}
					}
					if (y>0){
						DownAlpha = pixels[x + (imageWidth * (y-1))].a;
					}
					if (y<imageHeight-1){
						UpAlpha = pixels[x + (imageWidth * (y+1))].a;
					}
					//=========================================
					
					// Try to add an edge between the current pixel and the one directly above it
					// Above
					if (UpAlpha >= threshold) {
						if (UpRightAlpha < threshold && RightAlpha < threshold) {
							if (UpLeftAlpha >= threshold || LeftAlpha >= threshold) {
								ImageEdge e = new ImageEdge(GetPoint(x,y,uvX/imageWidth,uvY/imageHeight), GetPoint(x,y+1,uvX/imageWidth,(uvY+1)/imageHeight));
								edges.Add(e);
							}
						}
						else if ( UpLeftAlpha < threshold && LeftAlpha < threshold) {
							if (UpRightAlpha >= threshold || RightAlpha >= threshold) {
								ImageEdge e = new ImageEdge(GetPoint(x,y,uvX/imageWidth,uvY/imageHeight), GetPoint(x,y+1,uvX/imageWidth,(uvY+1)/imageHeight));
								edges.Add(e);
							}
						}
					}
					
					// Try to add an edge between the current pixel and one diagonally above it
					// Above and right
					if (UpRightAlpha >= threshold) {
						if (UpAlpha < threshold && RightAlpha >= threshold) {
							//  .
							// *.
							ImageEdge e = new ImageEdge(GetPoint(x,y,uvX/imageWidth,uvY/imageHeight), GetPoint(x+1,y+1,(uvX+1)/imageWidth,(uvY+1)/imageHeight));
							edges.Add(e);
						}
						else if (UpAlpha >= threshold && RightAlpha < threshold) {
							// ..
							// *
							ImageEdge e = new ImageEdge(GetPoint(x,y,uvX/imageWidth,uvY/imageHeight), GetPoint(x+1,y+1,(uvX+1)/imageWidth,(uvY+1)/imageHeight));
							edges.Add(e);
						}
					}
					
					// Try to add an edge between the current pixel and the one directly right of it
					// Right
					if (RightAlpha >= threshold) {
						if (UpAlpha < threshold && UpRightAlpha < threshold) {
							if (DownAlpha >= threshold || DownRightAlpha >= threshold) {
								// *.  or *.  *.
								// .       .  ..
								ImageEdge e = new ImageEdge(GetPoint(x,y,uvX/imageWidth,uvY/imageHeight), GetPoint(x+1,y,(uvX+1)/imageWidth,uvY/imageHeight));
								edges.Add(e);
							}
						}
						else if ( DownAlpha < threshold && DownRightAlpha < threshold) {
							if (UpAlpha >= threshold || UpRightAlpha >= threshold) {
								// add the horizontal edge
								ImageEdge e = new ImageEdge(GetPoint(x,y,uvX/imageWidth,uvY/imageHeight), GetPoint(x+1,y,(uvX+1)/imageWidth,uvY/imageHeight));
								edges.Add(e);
							}
						}
					}
					
					//Down Right
					if (DownRightAlpha >= threshold) {
						if ( RightAlpha < threshold && DownAlpha >= threshold) {
							// *
							// ..
							ImageEdge e = new ImageEdge(GetPoint(x,y,uvX/imageWidth,uvY/imageHeight), GetPoint(x+1,y-1,(uvX+1)/imageWidth,(uvY-1)/imageHeight));
							edges.Add(e);
						}
						else if (RightAlpha >= threshold && DownAlpha < threshold) {
							// *.
							//  .
							ImageEdge e = new ImageEdge(GetPoint(x,y,uvX/imageWidth,uvY/imageHeight), GetPoint(x+1,y-1,(uvX+1)/imageWidth,(uvY-1)/imageHeight));
							edges.Add(e);
						}
					}
					
				}
			}
			CreateFaces();
		}
		
		ImagePoint GetPoint(float x, float y, float u, float v) {
			for (int i = 0; i < points.Count; i++) {
				ImagePoint ver = (ImagePoint) points[i]; 
				if (ver.x == x && ver.y == y) {
					return ver;
				}
			}
			ImagePoint newver = new ImagePoint(x,y,u,v);
			points.Add(newver);
			return newver;
		}

		public void SimplifyEdges(float maxDeviance = 0.448f) {
			foreach (ImagePolygon polygon in polygons) {
				polygon.ReduceEdge(maxDeviance);
			}
		}
		
		void CreateFaces() {
			// Put the edges in order
			// Start the first edge loop with the first outside edge
			ImagePolygon currentpolygons = new ImagePolygon((ImageEdge)edges[0]);
			edges.RemoveAt(0);
			polygons.Add(currentpolygons);
			
			while (edges.Count > 0) {
				// Create new edge loop if the current one closed off
				if (currentpolygons.IsClosed()) {
					ImagePolygon nextpolygons = new ImagePolygon((ImageEdge)edges[0]);
					edges.RemoveAt(0);
					polygons.Add(nextpolygons);
					currentpolygons = nextpolygons;
				}
				// Test each edge to see if it fits into the polygons
				ArrayList deleteEdges = new ArrayList();
				for (int i = 0; i < edges.Count; i++) {
					ImageEdge e = (ImageEdge) edges[i];
					if (currentpolygons.AddEdge(e)) { // try to add the edge
						deleteEdges.Add(e);
					}
				}
				// Delete the added edges
				for (int i = 0; i < deleteEdges.Count; i++) {
					edges.Remove( (ImageEdge)deleteEdges[i] );
				}
			}
			
		}
		
		public ArrayList GetAllPoints() {
			ArrayList allPoints = new ArrayList();
			foreach (ImagePolygon el in polygons) {
				allPoints.Add(el.GetPoints());
			}
			return allPoints;
		}
		
		public Vector2 GetUVAtIndex(int loopIndex, int i) {
			ImagePolygon eL = (ImagePolygon) polygons[loopIndex];
			return eL.GetUVAtIndex(i);
		}
	}



	class ImagePoint {
		public float x,y,u,v;
		public ImagePoint(float _x, float _y, float _u, float _v){
			x = _x;
			y = _y;
			u = _u;
			v = _v;
		}
	}

	class ImageEdge {
		public ImagePoint v1, v2;

		public ImageEdge(ImagePoint _v1, ImagePoint _v2) {
			v1 = _v1;
			v2 = _v2;
		}
		
		public void Reverse() {
			ImagePoint hold = v1;
			v1 = v2;
			v2 = hold;
		}
	}
	class ImagePolygon {
		public ArrayList orderedEdges;
		
		public ImagePolygon(ImageEdge e) {
			orderedEdges = new ArrayList();
			orderedEdges.Add(e);
		}
		
		public bool AddEdge(ImageEdge e) {
			// Check if the edge connects the previous edge
			ImagePoint lastPoint = ((ImageEdge)orderedEdges[orderedEdges.Count-1]).v2;
			// Make sure the points are ordered correctly
			if (e.v1 == lastPoint) {
				orderedEdges.Add(e);
				return true;
			} else if (e.v2 == lastPoint) {
				e.Reverse();
				orderedEdges.Add(e);
				return true;
			}
			// Otherwise, Check if it shares with the first edge
			ImagePoint firstPoint = ((ImageEdge)orderedEdges[0]).v1;
			if (e.v2 == firstPoint) {
				orderedEdges.Insert(0,e);
				return true;
			} else if (e.v1 == firstPoint) {
				e.Reverse();
				orderedEdges.Insert(0, e);
				return true;
			}
			return false;
		}
		
		public bool IsClosed() {
			if (orderedEdges.Count < 2) return false; 
			ImagePoint lastPoint = ((ImageEdge) orderedEdges[orderedEdges.Count-1]).v2;
			ImagePoint firstPoint = ((ImageEdge) orderedEdges[0]).v1;
			if (firstPoint.x == lastPoint.x && firstPoint.y == lastPoint.y) return true;
			return false;
		}
		
		public Vector2[] GetPoints() {
			Vector2[] verts = new Vector2[orderedEdges.Count];
			for (int i = 0; i < orderedEdges.Count; i++) {
				ImagePoint v = ((ImageEdge) orderedEdges[i]).v1;
				verts[i]= new Vector2(v.x, v.y);
			}
			return verts;
		}
		
		public Vector2 GetUVAtIndex(int i) {
			if (i >= orderedEdges.Count) {

				return new Vector2();
			}
			ImagePoint v = ((ImageEdge)orderedEdges[i]).v1;
			return new Vector2(v.u, v.v);
		}
		
		public void ReduceEdge (float maxDeviance = 0.448f) {
			ArrayList newOrderedEdges = new ArrayList();
			ImageEdge currentEdge = (ImageEdge)orderedEdges[0];
			ImagePoint v0, v1, v2;
			
			for (int i = 1; i < orderedEdges.Count; i++) {
				ImageEdge testEdge = (ImageEdge) orderedEdges[i];
				v1 = currentEdge.v1;
				v0 = currentEdge.v2; // Middle point
				v2 = testEdge.v2;
				
				float d = Mathf.Abs((v2.x-v1.x)*(v1.y-v0.y) - (v1.x-v0.x)*(v2.y-v1.y)) / Mathf.Sqrt (Mathf.Pow(v2.x-v1.x, 2) + Mathf.Pow(v2.y-v1.y, 2));
				if (d<=maxDeviance){ // Merge the current edge and test edge
					currentEdge.v2 = v2;
				} else { // There isn't a continuation of line, so add current to new ordered and set current to testEdge
					newOrderedEdges.Add(currentEdge);
					currentEdge = testEdge;
				}
			}
			orderedEdges = newOrderedEdges;
		}
	}
}