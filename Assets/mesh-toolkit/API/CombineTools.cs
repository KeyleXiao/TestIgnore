using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MeshTK
{
	/// <summary>
	/// Combine Tools - Copyright EJM Software 2015
	/// Combine multiple mesh objects into one
	/// </summary>
	public class CombineTools : ScriptableObject
	{
    public static void CombineChildren(GameObject parent)
    {
      // Find all mesh filter submeshes and separate them by their cooresponding materials
      List<Material> materials = new List<Material>();
      ArrayList combineInstanceArrays = new ArrayList();

      MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>();

      foreach(MeshFilter meshFilter in meshFilters){
        MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
        if (meshRenderer!=null) {
          for (int s = 0; s < meshFilter.sharedMesh.subMeshCount; s++){
						//Skip if the submesh is empty
						if (meshFilter.sharedMesh.GetTriangles(s).Length==0){
							continue;
						}
						//Add combine instance
		        int materialArrayIndex = materials.FindIndex(p => p!=null?(p.name==meshRenderer.sharedMaterials[s].name):false);
		        if (materialArrayIndex == -1) {
							if (meshRenderer.sharedMaterials.Length > s) {
		            materials.Add (meshRenderer.sharedMaterials [s]);
							} else {
								materials.Add (null);
							}
		          materialArrayIndex = materials.Count - 1;
		        }
		        combineInstanceArrays.Add (new ArrayList ());

		        CombineInstance combineInstance = new CombineInstance ();
		        combineInstance.transform = meshFilter.transform.localToWorldMatrix * parent.transform.worldToLocalMatrix;
		        combineInstance.subMeshIndex = s;
            combineInstance.mesh = meshFilter.sharedMesh;
		        (combineInstanceArrays [materialArrayIndex] as ArrayList).Add (combineInstance);
		      }
				}
				//Disable child meshes
				meshFilter.gameObject.SetActive(false);
	    }

	    // Get / Create mesh filter
	    MeshFilter meshFilterCombine = parent.GetComponent<MeshFilter>();
	    if(!meshFilterCombine) {
	      meshFilterCombine = parent.AddComponent<MeshFilter>();
      }
	    // Combine by material index into per-material meshes
	    // also, Create CombineInstance array for next step
	    Mesh[] meshes = new Mesh[materials.Count];
	    CombineInstance[] combineInstances = new CombineInstance[materials.Count];

	    for(int m = 0; m < materials.Count; m++) {
	      CombineInstance[] combineInstanceArray = (combineInstanceArrays[m] as ArrayList).ToArray(typeof(CombineInstance)) as CombineInstance[];
	      meshes[m] = new Mesh();
	      meshes[m].CombineMeshes( combineInstanceArray, true, true );

	      combineInstances[m] = new CombineInstance();
	      combineInstances[m].mesh = meshes[m];
	      combineInstances[m].subMeshIndex = 0;
	    }

	    // Combine into one
	    meshFilterCombine.sharedMesh = new Mesh();
	    meshFilterCombine.sharedMesh.CombineMeshes( combineInstances, false, false );

	    // Destroy other meshes
	    foreach(Mesh mesh in meshes){
	      mesh.Clear();
	      DestroyImmediate(mesh);
      }

	    // get or create a mesh renderer
	    MeshRenderer meshRendererCombine = parent.GetComponent<MeshRenderer>();
      if(!meshRendererCombine) {
        meshRendererCombine = parent.AddComponent<MeshRenderer>();
      }

      // Assign materials
      Material[] materialsArray = materials.ToArray() as Material[];
  	  meshRendererCombine.materials = materialsArray;

			//Enable parent object
			parent.SetActive(true);
	  }
	}
}
