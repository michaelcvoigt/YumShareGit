//--------------------------------
//
// Voxels for Unity
//  Version: 1.10.3
//
// © 2014-17 by Ronny Burkersroda
//
//--------------------------------


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;


namespace Voxels
{

    /// <summary>
    /// Class to export incoming voxel data to a .vox file
    /// </summary>
    public class MagicaVoxelExporter : Processor
    {
        /// <summary>
        /// Target file path 
        /// </summary>
        public string filePath;

        /// <summary>
        /// Building co-routine 
        /// </summary>
        private IEnumerator coroutine = null;

        /// <summary>
        /// Current progress 
        /// </summary>
        protected float progress = 0;


        /// <summary>
        /// Build voxel data and store it to file
        /// </summary>
        /// <param name="voxels">Storage instance including collected voxels</param>
        /// <param name="bounds">Bounding box in absolute space</param>
        /// <param name="informer">Callback to report about finishing the creation of objects</param>
        /// <param name="parameter">Application-defined parameter for callback</param>
        /// <returns>Current progress</returns>
        public override float Build(Storage voxels, Bounds bounds, Informer informer, object parameter)
        {
            // Start co-routine at first call
            if (coroutine == null)
            {
                progress = 0;
                coroutine = CoBuild(voxels, bounds);
                if (coroutine != null)
                {
                    StartCoroutine(coroutine);
                }
            }

            // Unset co-routine at finish
            if (progress >= 1)
            {
                coroutine = null;
            }

            return progress;
        }

        /// <summary>
        /// Building function as co-routine 
        /// </summary>
        /// <param name="voxels">Storage instance including collected voxels</param>
        /// <param name="bounds">Bounding box in absolute space</param>
        /// <returns>Enumerator for co-routine processing</returns>
        protected virtual IEnumerator CoBuild(Storage voxels, Bounds bounds)
        {
            // Check for given array
            if (voxels != null)
            {
                // Create target file
                var fileStream = new BinaryWriter(File.Open(filePath, FileMode.OpenOrCreate));
                if (fileStream != null)
                {
                    // Resolution limit for file format
                    int maximumWidth = 256;
                    int maximumHeight = 256;
                    int maximumSlice = 256;

                    // Store file type identifier
                    fileStream.Write(new char[] { 'V', 'O', 'X', ' ' });
                    fileStream.Write((int)150);

                    // Begin main chunk
                    fileStream.Write(new char[] { 'M', 'A', 'I', 'N' });
                    fileStream.Write((int)0);
                    var sizePointer = fileStream.BaseStream.Position;
                    fileStream.Write((int)(sizeof(char) * 4 + sizeof(int) * 3));

                    // Write resolution
                    fileStream.Write(new char[] { 'S', 'I', 'Z', 'E' });
                    fileStream.Write((int)(sizeof(int) * 3));
                    fileStream.Write((int)0);
                    fileStream.Write((int)voxels.Width < maximumWidth ? voxels.Width : maximumWidth);
                    fileStream.Write((int)voxels.Height < maximumHeight ? voxels.Height : maximumHeight);
                    fileStream.Write((int)voxels.Depth < maximumSlice ? voxels.Depth : maximumSlice);

                    Material[] outputMaterials;
                    Dictionary<int, int> assignments;
                    var materialGroups = new Dictionary<Material, int>();
                    var materialsList = new List<Material>();
                    int materialsCount = 0;

                    Voxels.Storage.Iterator iterator = voxels.GetIterator();
                    Material material;
                    int x, y, z;
                    int voxelsNumber = 0;
                    int voxelsCount = 0;

                    // Interrupt co-routine
                    progress = 0;
                    yield return null;

                    // Process materials of all input voxels
                    while ((material = iterator.GetNextMaterial(out x, out y, out z)) != null)
                    {
                        // Convert coordinates to format space
                        var column = voxels.Width - x - 1;
                        var line = voxels.Depth - z - 1;
                        var slice = y;

                        // Check if coordinate is supported by file format
                        if (column < maximumWidth && line < maximumHeight && slice < maximumSlice)
                        {
                            // Add material to list and hash table if it is new
                            if (!materialGroups.ContainsKey(material))
                            {
                                materialGroups.Add(material, materialsList.Count);
                                materialsList.Add(material);
                                ++materialsCount;
                            }

                            // Increase number of voxels to store to file
                            ++voxelsCount;
                        }

                        // Compute current progression
                        if (++voxelsNumber % 1000 == 0)
                        {
                            progress = (float)voxelsNumber / (float)voxels.Count * 0.25f;
                            yield return null;
                        }
                    }

                    // Interrupt co-routine
                    progress = 0.25f;
                    yield return null;

                    // Minimize number of colors to given limit
                    var materialReducer = new MaterialReducer(this, materialsList, 254);
                    if (materialReducer != null)
                    {
                        // Wait until reduction has been finished
                        float progress;
                        while((progress = materialReducer.GetProgress()) < 1)
                        {
                            this.progress = progress * 0.5f + 0.25f;
                            yield return null;
                        }

                        // Get reduced materials and assignment pointers from original ones
                        materialReducer.GetResult(out outputMaterials, out assignments);
                    }
                    else
                    {
                        outputMaterials = null;
                        assignments = null;
                    }

                    // Interrupt co-routine
                    progress = 0.75f;
                    yield return null;

                    // Store voxels with assigned color index to file
                    fileStream.Write(new char[] { 'X', 'Y', 'Z', 'I' });
                    fileStream.Write((int)(sizeof(int) + sizeof(int) * voxelsCount));
                    fileStream.Write((int)0);
                    fileStream.Write((int)voxelsCount);
                    voxelsNumber = 0;
                    while ((material = iterator.GetNextMaterial(out x, out y, out z)) != null)
                    {
                        var column = voxels.Width - x - 1;
                        var line = voxels.Depth - z - 1;
                        var slice = y;

                        if (column < maximumWidth && line < maximumHeight && slice < maximumSlice)
                        {
                            int sourceIndex = materialGroups[material];
                            int targetIndex = assignments[sourceIndex];

                            fileStream.Write((byte)column);
                            fileStream.Write((byte)line);
                            fileStream.Write((byte)slice);
                            fileStream.Write((byte)(targetIndex + 1));
                        }

                        if (++voxelsNumber % 1000 == 0)
                        {
                            progress = (float)voxelsNumber / (float)voxels.Count * 0.25f + 0.75f;
                            yield return null;
                        }
                    }

                    // Store material colors to file
                    fileStream.Write(new char[] { 'R', 'G', 'B', 'A' });
                    fileStream.Write((int)(sizeof(int) * 256));
                    fileStream.Write((int)0);
                    for (int index = 0; index < outputMaterials.Length; ++index)
                    {
                        material = outputMaterials[index];

                        fileStream.Write((byte)(material.color.r * 255 + 0.5f));
                        fileStream.Write((byte)(material.color.g * 255 + 0.5f));
                        fileStream.Write((byte)(material.color.b * 255 + 0.5f));
                        fileStream.Write((byte)(material.color.a * 255 + 0.5f));
                    }

                    // Fill unused colors
                    for (int index = outputMaterials.Length; index < 256; ++index)
                    {
                        fileStream.Write((uint)0xFF000000);
                    }

                    // Store data size into file
                    var endPointer = fileStream.BaseStream.Position;
                    fileStream.Seek((int)sizePointer, SeekOrigin.Begin);
                    fileStream.Write((int)(endPointer - sizePointer - sizeof(int)));

                    // Close file stream
                    fileStream.Close();
                    fileStream = null;
                }
            }

            // Finish function
            progress = 1;
            yield return null;
        }

    }

}
