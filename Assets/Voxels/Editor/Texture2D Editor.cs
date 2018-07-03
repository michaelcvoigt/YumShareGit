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

    // Editor extension for Voxel Texture2D component
    [CustomEditor(typeof(Texture2D))]
    public class VoxelTexture2DEditor : Editor
    {

        // Show and process inspector
        public override void OnInspectorGUI()
        {
            Texture2D voxelTexture = (Texture2D)target;

            // Flag to enable or disable processing
            bool process = EditorGUILayout.Toggle("Enabled", voxelTexture.process);
            if (voxelTexture.process != process)
            {
                Undo.RecordObject(voxelTexture, "Processing Change");
                voxelTexture.process = process;
            }

            // Add title to bar
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x += EditorGUIUtility.currentViewWidth * 0.5f;
            rect.y -= rect.height;
            EditorGUI.LabelField(rect, Information.Title);

            bool powerOfTwo = EditorGUILayout.Toggle("Power of Two", voxelTexture.powerOfTwo);
            if (voxelTexture.powerOfTwo != powerOfTwo)
            {
                Undo.RecordObject(voxelTexture, "Power-of-Two Flag Change");
                voxelTexture.powerOfTwo = powerOfTwo;
            }

            // Path of the target file
            EditorGUILayout.BeginHorizontal();
            bool fileStoring = EditorGUILayout.Toggle("Target File", voxelTexture.fileStoring);
            if (voxelTexture.fileStoring != fileStoring)
            {
                Undo.RecordObject(voxelTexture, "File Storing Flag Change");
                voxelTexture.fileStoring = fileStoring;
            }
            GUI.enabled = false;
            string filePath = EditorGUILayout.TextField(voxelTexture.filePath == null ? "" : voxelTexture.filePath);
            GUI.enabled = true;
            if (GUILayout.Button("...", GUILayout.MaxWidth(24)))
            {
                string directory;

                // get directory from path name
                if (filePath.Length != 0)
                {
                    directory = System.IO.Path.GetDirectoryName(filePath);
                    if (directory == null)
                    {
                        directory = "";
                    }
                }
                else
                {
                    directory = "";
                }

                // Open "save to" dialog
                string temporaryFilePath = EditorUtility.SaveFilePanel("Save texture to file after buildup...", directory, System.IO.Path.GetFileName(filePath), "png");
                if (temporaryFilePath.Length != 0)
                {
                    filePath = temporaryFilePath;
                }
            }
            if (filePath.Length == 0)
            {
                filePath = null;
            }
            if (GUILayout.Button("X", GUILayout.MaxWidth(24)))
            {
                filePath = null;
            }
            if (voxelTexture.filePath != filePath)
            {
                Undo.RecordObject(voxelTexture, "File Path Change");
                voxelTexture.filePath = filePath;
            }
            EditorGUILayout.EndHorizontal();

            // Voxel Mesh usage flag
            bool voxelMeshUsage = EditorGUILayout.Toggle("Voxel Mesh Usage", voxelTexture.voxelMeshUsage);
            if (voxelTexture.voxelMeshUsage != voxelMeshUsage)
            {
                Undo.RecordObject(voxelTexture, "Voxel Mesh Usage Flag Change");
                voxelTexture.voxelMeshUsage = voxelMeshUsage;
            }
            if (voxelMeshUsage)
            {
                if (voxelTexture.GetComponent<Mesh>() == null)
                {
                    EditorGUILayout.HelpBox("There is no Voxel Mesh component attached!", MessageType.Warning);
                }

                Texture2D[] textures = voxelTexture.GetComponents<Texture2D>();
                foreach(Texture2D texture in textures)
                {
                    if (texture != voxelTexture)
                    {
                        if (texture.voxelMeshUsage)
                        {
                            EditorGUILayout.HelpBox("There are multiple Voxel Textures, which should be used for Voxel Mesh!", MessageType.Error);
                            break;
                        }
                    }
                }
            }
        }

    }

}