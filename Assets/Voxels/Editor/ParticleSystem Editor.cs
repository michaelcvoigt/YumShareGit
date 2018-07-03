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

    // Editor extension for Voxel Particle System component
    [CustomEditor(typeof(ParticleSystem))]
    public class VoxelParticleSystemEditor : Editor
    {

        // Show and process inspector
        public override void OnInspectorGUI()
        {
            ParticleSystem voxelParticleSystem = (ParticleSystem)target;

            // Flag to enable or disable processing
            bool process = EditorGUILayout.Toggle("Enabled", voxelParticleSystem.process);
            if (voxelParticleSystem.process != process)
            {
                Undo.RecordObject(voxelParticleSystem, "Processing Change");
                voxelParticleSystem.process = process;
            }

            // Add title to bar
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x += EditorGUIUtility.currentViewWidth * 0.5f;
            rect.y -= rect.height;
            EditorGUI.LabelField(rect, Information.Title);

            // Object selection for game object to use as template
            GameObject template = (GameObject)EditorGUILayout.ObjectField("Object Template", voxelParticleSystem.template, typeof(GameObject), true);
            if (voxelParticleSystem.template != template)
            {
                Undo.RecordObject(voxelParticleSystem, "Template Object Change");
                voxelParticleSystem.template = template;
            }

            // Sizing factor for the voxel particle
            EditorGUILayout.BeginHorizontal();
            float sizeFactor = EditorGUILayout.FloatField("Size Factor", voxelParticleSystem.sizeFactor);
            if (GUILayout.Button("Reset"))
            {
                sizeFactor = 1;
            }
            if (voxelParticleSystem.sizeFactor != sizeFactor)
            {
                Undo.RecordObject(voxelParticleSystem, "Size Factor Change");
                voxelParticleSystem.sizeFactor = sizeFactor;
            }
            EditorGUILayout.EndHorizontal();

            // Name of the main target container
            string targetName = EditorGUILayout.TextField("Target Name", voxelParticleSystem.targetName);
            if (voxelParticleSystem.targetName != targetName)
            {
                Undo.RecordObject(voxelParticleSystem, "Target Object Name Change");
                voxelParticleSystem.targetName = targetName;
            }
        }

    }

}