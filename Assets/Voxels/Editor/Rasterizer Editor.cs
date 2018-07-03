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

    // Editor extension for Voxel Rasterizer component
    [CustomEditor(typeof(Rasterizer))]
    public class RasterizerEditor : Editor
    {
        // Current processing progress (valid >= 0)
        float processingProgress = -1;

        string overscanInput;
        string frametimeTargetInput;

        // Scaling steps of the slider
        struct SliderStep
        {
            // Value limit to next range
            public float limit;
            // Difference for one step
            public float step;
        }

        // Array of ranges for the steps of voxel overscan
        static readonly SliderStep[] overscanSliderSteps =
        {
            new SliderStep { limit = 0, step = 1 },
            new SliderStep { limit = 20, step = 1 },
            new SliderStep { limit = 50, step = 5 },
            new SliderStep { limit = 100, step = 10 },
            new SliderStep { limit = 250, step = 25 },
            new SliderStep { limit = 500, step = 50 },
        };

        // Array of ranges for the steps of computation times
        static readonly SliderStep[] timeSliderSteps =
        {
            new SliderStep { limit = 0, step = 1 },
            new SliderStep { limit = 40, step = 1 },
            new SliderStep { limit = 100, step = 5 },
            new SliderStep { limit = 1000, step = 100 },
            new SliderStep { limit = 2000, step = 1000 },
        };


        // Show and process inspector
        public override void OnInspectorGUI()
        {
            string controlText;
            Bounds targetBounds = new Bounds();
            Vector3 vector3, voxelSize;
            Vector volumeResolution;
            int width, height, depth;

            Rasterizer voxelConverter = (Rasterizer)target;

            //Rect rect = GUILayoutUtility.GetLastRect();
            //EditorGUI.DropShadowLabel(rect, "Voxels for Unity");
            //EditorGUILayout.GetControlRect();

            //GUIContent content = EditorGUIUtility.ObjectContent(null, typeof(ConverterEditor));
            //if (content != null)
            //{
            //    content.image = null;
            //}

            Vector3 minimum = voxelConverter.minimumBound;
            Vector3 maximum = voxelConverter.maximumBound;

            // Button to recompute scan area
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Boundaries:");
            if (GUILayout.Button("Adopt from object") || (voxelConverter.minimumBound == Vector3.one && voxelConverter.maximumBound == -Vector3.one))
            {
                Undo.RecordObject(voxelConverter, "Bound Adoption");
                voxelConverter.RecomputeBounds();
                minimum = voxelConverter.minimumBound;
                maximum = voxelConverter.maximumBound;
            }
            EditorGUILayout.EndHorizontal();

            // Add title to bar
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x += EditorGUIUtility.currentViewWidth * 0.5f;
            rect.y -= rect.height;
            EditorGUI.LabelField(rect, Information.Title);

            // Fields to change boundaries
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("    Minimum", GUILayout.MaxWidth(96));
            minimum = EditorGUILayout.Vector3Field("", minimum);
            maximum = Vector3.Max(minimum, maximum);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("    Maximum", GUILayout.MaxWidth(96));
            maximum = EditorGUILayout.Vector3Field("", maximum);
            minimum = Vector3.Min(minimum, maximum);
            EditorGUILayout.EndHorizontal();

            // Sort components
            vector3 = Vector3.Min(minimum, maximum);
            minimum = vector3;

            // Store new boundaries
            if (minimum != voxelConverter.minimumBound)
            {
                Undo.RecordObject(voxelConverter, "Minimum Bound");
                voxelConverter.minimumBound = minimum;
            }
            if (maximum != voxelConverter.maximumBound)
            {
                Undo.RecordObject(voxelConverter, "Maximum Bound");
                voxelConverter.maximumBound = maximum;
            }

            // Compute center and size
            targetBounds.center = (minimum + maximum) * 0.5f;
            targetBounds.extents = maximum - minimum;

            // Get voxel size
            voxelSize = voxelConverter.GetVoxelSize(targetBounds.extents);

            // Field to change size of one voxel
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Voxel Size", GUILayout.MaxWidth(96));
            vector3 = EditorGUILayout.Vector3Field("", voxelSize);
            EditorGUILayout.EndHorizontal();

            // Store voxel size, if it has been changed
            if (vector3 != voxelSize)
            {
                Undo.RecordObject(voxelConverter, "Voxel Size Change");
                if (voxelConverter.SetVoxelSize(vector3.x, vector3.y, vector3.z))
                {
                    voxelSize = vector3;
                }
            }

            // Compute volume resolution, if it is not set
            volumeResolution = voxelConverter.GetVolumeResolution(targetBounds.extents);

            // Edit number of voxels
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Voxel Counts", GUILayout.MaxWidth(96));
            //EditorGUILayout.LabelField("X", GUILayout.MaxWidth(12));
            width = EditorGUILayout.IntField(volumeResolution.x, GUILayout.MinWidth(32));
            //EditorGUILayout.LabelField("Y", GUILayout.MaxWidth(12));
            height = EditorGUILayout.IntField(volumeResolution.y, GUILayout.MinWidth(32));
            //EditorGUILayout.LabelField("Z", GUILayout.MaxWidth(12));
            depth = EditorGUILayout.IntField(volumeResolution.z, GUILayout.MinWidth(32));
            EditorGUILayout.EndHorizontal();

            // Store volume resolution, if it has been changed
            if (width != volumeResolution.x || height != volumeResolution.y || depth != volumeResolution.z)
            {
                Undo.RecordObject(voxelConverter, "Voxel Resolution Change");
                if (voxelConverter.SetVolumeResolution(width, height, depth))
                {
                    volumeResolution = new Vector(width, height, depth);
                }
            }

            // Determine maximum sampling
            int samplingLimit = Rasterizer.Engine.TextureSizeLimit / Mathf.Max(Mathf.Max(volumeResolution.x, volumeResolution.y), volumeResolution.z);

            // Build resolution vector, sampling slider, color baking toggle mode with combo box
            int samplingResolution = EditorGUILayout.IntSlider("Sampling Resolution", voxelConverter.samplingResolution, 1, samplingLimit);
            if (voxelConverter.samplingResolution != samplingResolution)
            {
                Undo.RecordObject(voxelConverter, "Sampling Resolution Change");
                voxelConverter.samplingResolution = samplingResolution;
            }

            float oldVoxelOverscan = voxelConverter.voxelOverscan * 100.0f;
            float newVoxelOverscan = NonLinearHorizontalSlider("Voxel Overscan", oldVoxelOverscan, ref overscanInput, "%", overscanSliderSteps);

            //// Create own combination of edit field and non-linear slider
            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("Voxel Overscan", GUILayout.MinWidth(108), GUILayout.MaxWidth(116));
            //int oldVoxelOverscan = (int)(voxelConverter.voxelOverscan * 100.0f + 0.5f);
            //int newVoxelOverscan = EditorGUILayout.IntField(oldVoxelOverscan, GUILayout.MinWidth(24), GUILayout.MaxWidth(48));
            //EditorGUILayout.LabelField("%", GUILayout.MinWidth(20), GUILayout.MaxWidth(24));
            //newVoxelOverscan = (int)(SliderToValue(GUILayout.HorizontalSlider(ValueToSlider(newVoxelOverscan, overscanSliderSteps), 0, ValueToSlider(1000000, overscanSliderSteps)), overscanSliderSteps));
            //EditorGUILayout.EndHorizontal();

            // Store new value
            if (oldVoxelOverscan != newVoxelOverscan)
            {
                Undo.RecordObject(voxelConverter, "Voxel Overscan Change");
                voxelConverter.voxelOverscan = (float)newVoxelOverscan * 0.01f;
            }

            BakingOperation bakingOperationMode = (BakingOperation)EditorGUILayout.IntPopup("Baking Mode", (int)voxelConverter.bakingOperationMode, Rasterizer.BakingOperationModeNames, Rasterizer.BakingOperationModes);
            if (voxelConverter.bakingOperationMode != bakingOperationMode)
            {
                Undo.RecordObject(voxelConverter, "Baking Operation Mode Change");
                voxelConverter.bakingOperationMode = bakingOperationMode;
            }
            switch (bakingOperationMode)
            {
                case BakingOperation.OriginalMaterial:
                    break;

                default:
                    // Object selection of materials to use for opaque and transparent voxels
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.MaxWidth(16));
                    Material templateMaterial = (Material)EditorGUILayout.ObjectField("Opaque Template", voxelConverter.opaqueTemplate, typeof(Material), true);
                    if (voxelConverter.opaqueTemplate != templateMaterial)
                    {
                        Undo.RecordObject(voxelConverter, "Opaque Template Material Change");
                        voxelConverter.opaqueTemplate = templateMaterial;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.MaxWidth(16));
                    templateMaterial = (Material)EditorGUILayout.ObjectField("Transparent Template", voxelConverter.transparentTemplate, typeof(Material), true);
                    if (voxelConverter.transparentTemplate != templateMaterial)
                    {
                        Undo.RecordObject(voxelConverter, "Transparent Template Material Change");
                        voxelConverter.transparentTemplate = templateMaterial;
                    }
                    EditorGUILayout.EndHorizontal();

                    // Flags to modulate material components
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.MaxWidth(16));
                    bool mainColorModulation = EditorGUILayout.ToggleLeft("Main Color", voxelConverter.mainColorModulation, GUILayout.MaxWidth(128));
                    if (voxelConverter.mainColorModulation != mainColorModulation)
                    {
                        Undo.RecordObject(voxelConverter, "Main Color Flag Change");
                        voxelConverter.mainColorModulation = mainColorModulation;
                    }
                    bool specularColorModulation = EditorGUILayout.ToggleLeft("Specular Color", voxelConverter.specularColorModulation, GUILayout.MaxWidth(128));
                    if (voxelConverter.specularColorModulation != specularColorModulation)
                    {
                        Undo.RecordObject(voxelConverter, "Specular Color Flag Change");
                        voxelConverter.specularColorModulation = specularColorModulation;
                    }
                    bool emissiveColorModulation = EditorGUILayout.ToggleLeft("Emissive Color", voxelConverter.emissiveColorModulation, GUILayout.MaxWidth(128));
                    if (voxelConverter.emissiveColorModulation != emissiveColorModulation)
                    {
                        Undo.RecordObject(voxelConverter, "Emissive Color Flag Change");
                        voxelConverter.emissiveColorModulation = emissiveColorModulation;
                    }
                    EditorGUILayout.EndHorizontal();

                    //if (mainColorModulation || specularColorModulation || emissiveColorModulation)
                    {
                        // Fields for color modulation
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.MaxWidth(32));
                        float saturationFactor = EditorGUILayout.FloatField("Saturation", voxelConverter.saturationFactor, GUILayout.MaxWidth(320));
                        if (GUILayout.Button("Reset"))
                        {
                            saturationFactor = 1;
                        }
                        if (voxelConverter.saturationFactor != saturationFactor)
                        {
                            Undo.RecordObject(voxelConverter, "Color Saturation Change");
                            voxelConverter.saturationFactor = saturationFactor;
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.MaxWidth(32));
                        float brightnessFactor = EditorGUILayout.FloatField("Brightness", voxelConverter.brightnessFactor, GUILayout.MaxWidth(320));
                        if (GUILayout.Button("Reset"))
                        {
                            brightnessFactor = 1;
                        }
                        if (voxelConverter.brightnessFactor != brightnessFactor)
                        {
                            Undo.RecordObject(voxelConverter, "Color Brightness Change");
                            voxelConverter.brightnessFactor = brightnessFactor;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    break;
            }

            // Get current processing state
            processingProgress = voxelConverter.GetProgress();

            // Check if game is not running
            if (!Application.isPlaying)
            {
                // Processing button with dynamic text depending on current progress
                controlText =
                    processingProgress > 1 ? "Waiting... [" + (int)processingProgress + "]" :
                    processingProgress >= 0 ? "Processing... [" + (int)((1 - processingProgress) * 100) + " %]" :
                    "Process";
                if (GUILayout.Button(controlText))
                {
                    // Check if object is already being processed or will be
                    if (processingProgress >= 0)
                    {
                        // Stop processing
                        if (voxelConverter.Stop())
                        {
                            // Unset progress
                            processingProgress = -1;
                        }
                    }
                    else
                    {
                        // Start processing
                        if (voxelConverter.Process())
                        {
                            // Add update callback
                            EditorApplication.update += OnUpdate;
                        }
                    }
                }
            }
            else
            {
                // Check if object is already being processed or will be
                if (processingProgress >= 0)
                {
                    controlText =
                        processingProgress > 1 ? "Waiting... [" + (int)processingProgress + "]" :
                        "Processing... [" + (int)((1 - processingProgress) * 100) + " %]";

                    // Output progress
                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    Rect rectangle = EditorGUILayout.BeginVertical();
                    EditorGUI.ProgressBar(rectangle, processingProgress == 1 ? 0 : 1 - processingProgress % 1, controlText);
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                    EditorUtility.SetDirty(voxelConverter);
                }
            }

            int oldValue, newValue;

            // Read current frame time target from preferences
            oldValue = (int)(EditorPrefs.GetFloat("VoxelConvertBudgetTime", voxelConverter.budgetTime) * 1000.0f + 0.5f);
            //oldValue = (int)(Converter.budgetTime * 1000.0f + 0.5f);

            //// Convert value to string
            //if (oldValue >= timeSliderSteps[timeSliderSteps.Length - 1].limit)
            //{
            //    controlText = "max";
            //}
            //else
            //{
            //    controlText = oldValue.ToString();
            //}

            //// Create own combination of edit field and non-linear slider
            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("Frame Time Target", GUILayout.MinWidth(108), GUILayout.MaxWidth(116));
            //if (!int.TryParse(controlText = GUILayout.TextField(controlText, GUILayout.MinWidth(24), GUILayout.MaxWidth(48)), out newValue))
            //{
            //    // Convert text to new value
            //    if (controlText.ToLower() == "max")
            //    {
            //        newValue = (int)timeSliderSteps[timeSliderSteps.Length - 1].limit;
            //    }
            //    else if (controlText.Length == 0)
            //    {
            //        newValue = 0;
            //    }
            //    else
            //    {
            //        newValue = oldValue;
            //    }
            //}
            //EditorGUILayout.LabelField("ms", GUILayout.MinWidth(20), GUILayout.MaxWidth(24));
            //int intermediateValue = (int)SliderToValue(ValueToSlider(newValue, timeSliderSteps), timeSliderSteps);
            //int temporaryValue = (int)(SliderToValue(GUILayout.HorizontalSlider(ValueToSlider(newValue, timeSliderSteps), 0, ValueToSlider(1000000, timeSliderSteps)), timeSliderSteps));
            //EditorGUILayout.EndHorizontal();

            //// Store value to instance and preferences, if it has been changed
            //if (intermediateValue != temporaryValue)
            //{
            //    newValue = temporaryValue;
            //}

            newValue = (int)(NonLinearHorizontalSlider("Frame Time Target", oldValue, ref frametimeTargetInput, "ms", timeSliderSteps, false, true, true) + 0.5f);
            if (oldValue != newValue)
            {
                Undo.RecordObject(voxelConverter, "Frame Time Target Change");
                voxelConverter.budgetTime = (float)newValue * 0.001f;
                EditorPrefs.SetFloat("VoxelConvertBudgetTime", voxelConverter.budgetTime);
            }

            bool oldFlag = EditorPrefs.GetBool("VoxelConvertMultiThreading", voxelConverter.multiThreading);
            bool newFlag;

            // Create toggle for multi-threading
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Multi Threading", GUILayout.MinWidth(108), GUILayout.MaxWidth(116));
            newFlag = EditorGUILayout.Toggle(oldFlag);
            EditorGUILayout.EndHorizontal();

            if (newFlag != oldFlag)
            {
                Undo.RecordObject(voxelConverter, "Multi Threading Change");
                voxelConverter.multiThreading = newFlag;
                EditorPrefs.SetBool("VoxelConvertMultiThreading", voxelConverter.multiThreading);
            }

            long sampledWidth, sampledHeight, sampledDepth, size;

            // Compute total size of the volume including sampling
            sampledWidth = volumeResolution.x * voxelConverter.samplingResolution;
            sampledHeight = volumeResolution.y * voxelConverter.samplingResolution;
            sampledDepth = volumeResolution.z * voxelConverter.samplingResolution;

            // Estimate highest amount of memory, which is required to store information to all possible voxels
            size = Storage.ComputeMaximumSize(width, height, depth, voxelConverter.samplingResolution, voxelConverter.bakingOperationMode == BakingOperation.OriginalMaterial);

            // Output information
            controlText = string.Format("Object Extent: {0:F} x {1:F} x {2:F}"
                + "\nRasterization: {3:##,#} x {5:##,#} x {7:##,#}"//   [{4:##,#} x {6:##,#} x {8:##,#}]"
                + "\nVoxels Count: {12}{9:##,#}"//   [{10:##,#}]"
                , targetBounds.extents.x, targetBounds.extents.y, targetBounds.extents.z
                , width, sampledWidth, height, sampledHeight, depth, sampledDepth
                , width * height * depth, sampledWidth * sampledHeight * sampledDepth
                , size
                , "max. ", "max. "
                );
            EditorGUILayout.HelpBox(controlText, MessageType.None);
        }

        const string minimumLabel = "min";
        const string maximumLabel = "max";

        // Convert given slider value to text
        string SliderToText(float value, bool minimize = false, float minimum = 0, bool maximize = false, float maximum = float.MaxValue)
        {
            if (minimize && (value <= minimum))
            {
                return minimumLabel;
            }
            else if (maximize && (value >= maximum))
            {
                return maximumLabel;
            }

            return value.ToString();
        }

        // Show edit field and control for non-linear slider
        float NonLinearHorizontalSlider(string label, float value, ref string text, string unit, SliderStep[] sliderSteps, bool floatingPoint = true, bool minimize = false, bool maximize = false)
        {
            string temporaryText;
            float result;

            // Initialize text string, if it is not
            if (text == null)
            {
                text = SliderToText(value, minimize, sliderSteps[0].limit, maximize, sliderSteps[sliderSteps.Length - 1].limit);
            }

            // Create own combination of edit field and non-linear slider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.MinWidth(108), GUILayout.MaxWidth(116));
            temporaryText = GUILayout.TextField(text, GUILayout.MinWidth(24), GUILayout.MaxWidth(48));
            if (unit != null)
            {
                EditorGUILayout.LabelField(unit, GUILayout.MinWidth(20), GUILayout.MaxWidth(24));
            }
            float intermediateValue = SliderToValue(ValueToSlider(value, sliderSteps), sliderSteps);
            float temporaryValue = SliderToValue(GUILayout.HorizontalSlider(ValueToSlider(value, sliderSteps), 0, ValueToSlider(float.MaxValue, sliderSteps)), sliderSteps);
            EditorGUILayout.EndHorizontal();

            // Check if slider has changed
            if (temporaryValue != intermediateValue)
            {
                // Convert value to string
                text = SliderToText(temporaryValue, minimize, sliderSteps[0].limit, maximize, sliderSteps[sliderSteps.Length - 1].limit);

                // Store new value as result
                result = temporaryValue;
            }
            // Check if text field has changed
            else if (temporaryText != text)
            {
                // Check for minimum
                if (temporaryText.ToLower().StartsWith(minimumLabel))
                {
                    result = sliderSteps[0].limit;
                }
                // Check for maximum
                else if (temporaryText.ToLower().StartsWith(maximumLabel))
                {
                    result = sliderSteps[sliderSteps.Length - 1].limit;
                }
                else
                {
                    // Convert text to number
                    float.TryParse(temporaryText, out result);
                }

                // Store changed text for following comparison
                text = temporaryText;
            }
            else
            {
                // Return unaltered input value
                result = value;
            }

            return result;
        }

        // Convert unscaled to slider value
        float ValueToSlider(float time, SliderStep[] sliderSteps)
        {
            float value = 0;
            float offset = 0;

            if (time < 0)
            {
                time = 0;
            }

            for (int index = 0; index < sliderSteps.Length; ++index)
            {
                if (time < sliderSteps[index].limit)
                {
                    return value + (time - offset) / sliderSteps[index].step;
                }
                else
                {
                    value += (sliderSteps[index].limit - offset) / sliderSteps[index].step;

                    offset = sliderSteps[index].limit;
                }
            }

            return value + 1;
        }

        // Convert slider to unscaled value
        float SliderToValue(float value, SliderStep[] sliderSteps)
        {
            float offset = 0;
            float count;

            if (value < 0)
            {
                value = 0;
            }

            for (int index = 0; index < sliderSteps.Length; ++index)
            {
                count = (sliderSteps[index].limit - offset) / sliderSteps[index].step;

                if (value < count)
                {
                    return (float)(int)((value * sliderSteps[index].step + offset) / sliderSteps[index].step + 0.5f) * sliderSteps[index].step;
                }
                else
                {
                    value -= count;

                    offset = sliderSteps[index].limit;
                }
            }

            return offset;
        }

        // Event method, which is called regularly, if target object is being or will be processed
        void OnUpdate()
        {
            Rasterizer voxelConverter = (Rasterizer)target;

            // Check if converter engine instance is existing
            if (Rasterizer.Engine.IsActive())
            {
                // Force call of the converter engine update method
                voxelConverter.gameObject.name = voxelConverter.gameObject.name;

                // Force update of the inspector
                Repaint();
            }
            else
            {
                // Remove this method from being called
                EditorApplication.update -= OnUpdate;
            }

        }

    }


    // Class for editor initialization
    [UnityEditor.InitializeOnLoad]
    public class EditorStartup
    {
        // Load Voxels component icon
        static EditorStartup()
        {
            foreach (UnityEditor.MonoScript script in UnityEditor.MonoImporter.GetAllRuntimeMonoScripts())
            {
                System.Type type = script.GetClass();
                if (type == typeof(Rasterizer))
                {
                    UnityEngine.Texture texture = null;

                    string[] iconIDs = UnityEditor.AssetDatabase.FindAssets("Voxels for Unity icon");
                    foreach (string iconID in iconIDs)
                    {
                        texture = (UnityEngine.Texture)UnityEditor.AssetDatabase.LoadAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(iconID), typeof(UnityEngine.Texture2D));
                        if (texture != null)
                        {
                            break;
                        }
                    }

                    if (texture != null)
                    {
                        System.Type editorGUIUtility = typeof(UnityEditor.EditorGUIUtility);
                        System.Reflection.MethodInfo method = editorGUIUtility.GetMethod("SetIconForObject", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (method != null)
                        {
                            method.Invoke(null, new object[] { script, texture });
                        }

                        method = editorGUIUtility.GetMethod("TextContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        if (method != null)
                        {
                            GUIContent guiContent = (GUIContent)method.Invoke(null, new object[] { "Voxel Converter (Script)" });
                            if (guiContent != null)
                            {
                                guiContent.text = "Voxel Converter";
                            }
                        }
                    }
                }
            }
        }
    }

}