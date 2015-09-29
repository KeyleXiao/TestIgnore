using UnityEngine;
using System.IO;

namespace MeshTK
{
	/// <summary>
	/// Texture Tools - Copyright EJM Software 2015
	/// Class for manipulating any texture.
	/// </summary>
	public class TextureTools
	{
		public static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
		{
		 	int dy = (int)(y1-y0);
			int dx = (int)(x1-x0);
		 	int stepx, stepy;
		 
			if (dy < 0) {dy = -dy; stepy = -1;}
			else {stepy = 1;}
			if (dx < 0) {dx = -dx; stepx = -1;}
			else {stepx = 1;}
			dy <<= 1;
			dx <<= 1;
		 
			float fraction = 0;
		 
			tex.SetPixel(x0, y0, col);
			if (dx > dy) {
				fraction = dy - (dx >> 1);
				while (Mathf.Abs(x0 - x1) > 1) {
					if (fraction >= 0) {
						y0 += stepy;
						fraction -= dx;
					}
					x0 += stepx;
					fraction += dy;
					tex.SetPixel(x0, y0, col);
				}
			}
			else {
				fraction = dx - (dy >> 1);
				while (Mathf.Abs(y0 - y1) > 1) {
					if (fraction >= 0) {
						x0 += stepx;
						fraction -= dy;
					}
					y0 += stepy;
					fraction += dx;
					tex.SetPixel(x0, y0, col);
				}
			}
		}
		/// <summary>
		/// Saves a texture at the given path
		/// </summary>
		public static void SaveTexture(Texture2D tex, string path)
		{
			if (!string.IsNullOrEmpty(path)){
				byte[] bytes = tex.EncodeToPNG();
				File.WriteAllBytes(path, bytes);
			}
		}
	}
}