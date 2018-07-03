//--------------------------------
//
// Voxels for Unity
//  Version: 1.10.3
//
// © 2014-17 by Ronny Burkersroda
//
//--------------------------------


using UnityEngine;
using UnityEditor;


namespace Voxels
{

    // Editor extension for Voxel Mesh component
    [CustomEditor(typeof(Mesh))]
    public class VoxelMeshEditor : Editor
    {

        // Show and process inspector
        public override void OnInspectorGUI()
        {
            Mesh voxelMesh = (Mesh)target;

            // Flag to enable or disable processing
            bool process = EditorGUILayout.Toggle("Enabled", voxelMesh.process);
            if (voxelMesh.process != process)
            {
                Undo.RecordObject(voxelMesh, "Processing Change");
                voxelMesh.process = process;
            }

            // Add title to bar
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x += EditorGUIUtility.currentViewWidth * 0.5f;
            rect.y -= rect.height;
            EditorGUI.LabelField(rect, Information.Title);

            // Object selection for a mesh to use as a voxel
            UnityEngine.Mesh mesh = (UnityEngine.Mesh)EditorGUILayout.ObjectField("Voxel Mesh", voxelMesh.mesh, typeof(UnityEngine.Mesh), true);
            if (voxelMesh.mesh != mesh)
            {
                Undo.RecordObject(voxelMesh, "Voxel Mesh Change");
                voxelMesh.mesh = mesh;
            }

            // Sizing factor for the voxel mesh
            EditorGUILayout.BeginHorizontal();
            float sizeFactor = EditorGUILayout.FloatField("Size Factor", voxelMesh.sizeFactor);
            if (GUILayout.Button("Reset"))
            {
                sizeFactor = 1;
            }
            if (voxelMesh.sizeFactor != sizeFactor)
            {
                Undo.RecordObject(voxelMesh, "Size Factor Change");
                voxelMesh.sizeFactor = sizeFactor;
            }
            EditorGUILayout.EndHorizontal();

            // Flag to make new containers static
            EditorGUILayout.BeginHorizontal();
            bool staticContainers = EditorGUILayout.Toggle("Static Containers", voxelMesh.staticContainers);
            if (voxelMesh.staticContainers != staticContainers)
            {
                Undo.RecordObject(voxelMesh, "Static Containers Change");
                voxelMesh.staticContainers = staticContainers;
            }

            //bool skinnedMesh = EditorGUILayout.ToggleLeft("Skinned Mesh", voxelMesh.skinnedMesh);
            //if (voxelMesh.skinnedMesh != skinnedMesh)
            //{
            //    Undo.RecordObject(voxelMesh, "Skinned Mesh Flag Change");
            //    voxelMesh.skinnedMesh = skinnedMesh;
            //}
            EditorGUILayout.EndHorizontal();

            // Flag to merge meshes with equal materials
            EditorGUILayout.BeginHorizontal();
            bool mergeMeshes = EditorGUILayout.Toggle("Merge Meshes", voxelMesh.mergeMeshes);
            if (voxelMesh.mergeMeshes != mergeMeshes)
            {
                Undo.RecordObject(voxelMesh, "Meshes Merging Change");
                voxelMesh.mergeMeshes = mergeMeshes;
            }

            // Flag to merge only meshes with opaque materials
            EditorGUI.BeginDisabledGroup(!mergeMeshes);
            bool opaqueOnly = EditorGUILayout.ToggleLeft("Opaque Only", voxelMesh.opaqueOnly);
            if (voxelMesh.opaqueOnly != opaqueOnly)
            {
                Undo.RecordObject(voxelMesh, "Only Opaque Mesh Merging Change");
                voxelMesh.opaqueOnly = opaqueOnly;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Elements to fill a texture
            EditorGUILayout.BeginHorizontal();
            bool mainTextureTarget = EditorGUILayout.ToggleLeft("Main Texture", voxelMesh.mainTextureTarget, GUILayout.MaxWidth(128));
            if (voxelMesh.mainTextureTarget != mainTextureTarget)
            {
                Undo.RecordObject(voxelMesh, "Main Texture Target Flag Change");
                voxelMesh.mainTextureTarget = mainTextureTarget;
            }
            bool emissiveTextureTarget = EditorGUILayout.ToggleLeft("Emission Texture", voxelMesh.emissiveTextureTarget, GUILayout.MaxWidth(128));
            if (voxelMesh.emissiveTextureTarget != emissiveTextureTarget)
            {
                Undo.RecordObject(voxelMesh, "Emissive Texture Target Flag Change");
                voxelMesh.emissiveTextureTarget = emissiveTextureTarget;
            }

            // Flag to transfer material to vertex color
            bool vertexColors = EditorGUILayout.ToggleLeft("Vertex Colors", voxelMesh.vertexColors, GUILayout.MaxWidth(128));
            if (voxelMesh.vertexColors != vertexColors)
            {
                Undo.RecordObject(voxelMesh, "Vertex Color Flag Change");
                voxelMesh.vertexColors = vertexColors;
            }
            EditorGUILayout.EndHorizontal();

            //// Object selection for a voxel texture to use for colors
            //VoxelTexture voxelTexture = (VoxelTexture)EditorGUILayout.ObjectField("Color Texture", voxelMesh.voxelTexture, typeof(VoxelTexture), true);
            //if (voxelMesh.voxelTexture != voxelTexture)
            //{
            //    Undo.RecordObject(voxelMesh, "Color Texture Change");
            //    voxelMesh.voxelTexture = voxelTexture;
            //}

            //if (voxelMesh.voxelTexture != null)
            //{
            //    // Object selection of a material to use for texturing
            //    EditorGUILayout.BeginHorizontal();
            //    EditorGUILayout.LabelField("", GUILayout.MaxWidth(16));
            //    Material templateMaterial = (Material)EditorGUILayout.ObjectField("Material Template", voxelMesh.textureMaterialTemplate, typeof(Material), true);
            //    if (voxelMesh.textureMaterialTemplate != templateMaterial)
            //    {
            //        Undo.RecordObject(voxelMesh, "Texture Template Material Change");
            //        voxelMesh.textureMaterialTemplate = templateMaterial;
            //    }
            //    EditorGUILayout.EndHorizontal();
            //}

            //VoxelTexture[] voxelTextures = voxelMesh.GetComponents<VoxelTexture>();
            //if (voxelTextures.Length >= 1)
            //{
            //    string[] names = new string[voxelTextures.Length + 1];
            //    int[] indices = new int[voxelTextures.Length + 1];
            //    int number;
            //    int index = -1;

            //    names[0] = "(none)";
            //    indices[0] = -1;

            //    for (number = 0; number < voxelTextures.Length; ++number)
            //    {
            //        if (voxelMesh.voxelTexture == voxelTextures[number])
            //        {
            //            index = number;
            //        }

            //        names[number + 1] = voxelTextures[number].name + " (" + number + ")";
            //        indices[number + 1] = index;
            //    }

            //    index = EditorGUILayout.IntPopup("Color Texture", index, names, indices);
            //    //if (voxelConverter.bakingOperationMode != bakingOperationMode)
            //    //{
            //    //    Undo.RecordObject(voxelConverter, "Baking Operation Mode Change");
            //    //    voxelConverter.bakingOperationMode = bakingOperationMode;
            //    //}
            //}

            // Name of the main target container
            string targetName = EditorGUILayout.TextField("Target Name", voxelMesh.targetName);
            if (voxelMesh.targetName != targetName)
            {
                Undo.RecordObject(voxelMesh, "Target Object Name Change");
                voxelMesh.targetName = targetName;
            }

        }

    }

}