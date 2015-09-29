Mesh Toolkit - Unity Editor Extension
Developed By: EJM Software
=====================================
----> Read the manual at http://ejmsoftware.com/meshtk/manual/
=====================================
Learn about Mesh Toolkit at http://ejmsoftware.com/meshtk/
Check out our other software at http://ejmsoftware.com
=====================================
Mesh Toolkit Basics
 - To use Mesh TK, simply select any object with a mesh filter component in the scene view.
 - Once you have selected an object you should see a wrench icon in the corner of the scene view.
 - Click the wrench icon to enter edit mode.
 - There are six sets of tools you can use to edit the mesh (object, vertex, triangle, normal, uv, primitives)
======================================
Notes (see more information on ejmsoftware.com)
 - You can only create primitives on MeshFilters, not SkinnedMeshRenderers
 - You must use a shader that supports Vertex colors (there is one provided) in order to use vertex colored meshes.
 - When you enter edit mode the object's mesh data is saved. You can revert to this save until you
   exit edit mode, at which point all changes you made are irreversible.
 - If the Mesh TK GUI is not visible for the selected object, make sure its
   MeshFilter or SkinnedMeshRenderer is expanded in the Inspector.