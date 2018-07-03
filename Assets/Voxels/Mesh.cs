//--------------------------------
//
// Voxels for Unity
//  Version: 1.10.3
//
// © 2014-17 by Ronny Burkersroda
//
//--------------------------------


using UnityEngine;
using System.Collections.Generic;
using UnityEditor;


namespace Voxels
{

    // Class to convert incoming voxel data to a list of meshes
    public class Mesh : Processor
    {
        // Mesh for one voxel and sizing factor
        public UnityEngine.Mesh mesh;
        public float sizeFactor = 1;
        // Object to use as template for containers
        public GameObject containerTemplate = null;
        // Flag to transfer material to vertex colors
        public bool vertexColors = false;
        // Flag to create texture and use it as main one in the materials
        public bool mainTextureTarget = false;
        // Flag to create texture and use it as emission one in the materials
        public bool emissiveTextureTarget = false;
        // Flag to create static game objects
        public bool staticContainers = true;
        // Flag to merge containers with comparable material to one mesh
        public bool mergeMeshes = true;
        // Flag to merge only opaque containers
        public bool opaqueOnly = true;
        // Limit of the amount of voxels per output object
        public int objectVoxelLimit = 0;
        //// Flag to create skinned mesh
        //public bool skinnedMesh = false;
        // Source of voxel texture to use for colors
        public Texture voxelTexture = null;
        // Material to use in combination with voxel texture
        public Material textureMaterialTemplate = null;
        // Name of the target container to create
        public string targetName = "Voxel Mesh";

        // Combination of containers for given materials
        Dictionary<Material, GameObject> groups;
        IEnumerator<KeyValuePair<Material, GameObject>> currentGroup;

        // Array of mesh filters and processing flags
        MeshFilter[] meshFilters;
        bool[] processedMeshes;
        // Container for current material
        GameObject materialContainer;
        // Array of vertex colors
        Color[] colors;
        Color lastColor;

        // Target container
        GameObject mainContainer;

        // Transformation vectors
        Vector3 offset;
        Vector3 scaling;
        Vector3 globalScaling;

        // Current position
        //int currentHeight = 0;
        //int currentDepth = 0;
        int groupNumber = 0;

        // Mesh counters
        int meshCount = 0;
        int currentMesh;

        // List of mesh data interface and current index
        List<IMeshData> meshDataInterfaces;
        int currentMeshDataInterface;

        // Voxel texture input / processor
        public Texture2D.Process voxelTexture2D;
        Vector2[] textureCoordinates;

        // Voxels iterator
        Storage.Iterator iterator;

        // Build voxel object
        public override float Build(Storage voxels, Bounds bounds, Informer informer, object parameter)
        {
            // Check for given array
            if (voxels != null)
            {
                GameObject subContainer;
                int width = voxels.Width;
                int height = voxels.Height;
                int depth = voxels.Depth;
                int sides = voxels.FacesCount;
                int x, y, z;

                // Check for non-empty array
                if (width * height * depth * sides > 0)
                {
                    if (mainContainer == null)
                    {
                        // Check if texture is required
                        if (mainTextureTarget || emissiveTextureTarget)
                        {
                            // Create voxel texture, if required
                            if (voxelTexture2D == null)
                            {
                                voxelTexture2D = new Texture2D.Process();
                            }

                            // Build texture
                            if (voxelTexture2D != null && voxelTexture2D.CurrentProgress < 1)
                            {
                                return voxelTexture2D.Build(voxels, bounds) * 0.5f;
                            }
                        }
                        else
                        {
                            voxelTexture2D = null;
                        }

                        // Get iterator
                        iterator = voxels.GetIterator();

                        // Create empty game object
                        mainContainer = new GameObject(targetName);
                        if (mainContainer != null)
                        {
                            // Hide new container
                            mainContainer.hideFlags |= HideFlags.HideAndDontSave;

                            // Create empty list to store groups to
                            groups = new Dictionary<Material, GameObject>();

                            // Create empty list to store mesh data interfaces to
                            meshDataInterfaces = new List<IMeshData>();
                            currentMeshDataInterface = 0;

                            // Copy position from source object
                            mainContainer.transform.position = gameObject.transform.position;

                            // Copy static flag
                            mainContainer.isStatic = staticContainers;

                            // Calculate total scaling for one block
                            globalScaling = new Vector3(2.0f * bounds.extents.x / (float)width, 2.0f * bounds.extents.y / (float)height, 2.0f * bounds.extents.z / (float)depth);

                            // Check for given mesh
                            if (mesh != null)
                            {
                                // Calculate offset and scaling for one voxel mesh
                                offset = -mesh.bounds.center;
                                scaling.x = 0.5f / mesh.bounds.extents.x;
                                scaling.y = 0.5f / mesh.bounds.extents.y;
                                scaling.z = 0.5f / mesh.bounds.extents.z;
                                offset.x *= scaling.x;
                                offset.y *= scaling.y;
                                offset.z *= scaling.z;
                                scaling.x *= globalScaling.x;
                                scaling.y *= globalScaling.y;
                                scaling.z *= globalScaling.z;
                            }
                            else
                            {
                                // Unset translation und scaling
                                offset = Vector3.zero;
                                scaling = Vector3.one;
                            }

                            // Add offset for half voxel
                            offset += new Vector3(0.5f * globalScaling.x, 0.5f * globalScaling.y, 0.5f * globalScaling.z);

                            // Move to match position of the original object
                            offset += bounds.center - gameObject.transform.position - bounds.extents;
                        }
                    }

                    // Check for main container and voxel iterator
                    if (mainContainer != null && iterator != null)
                    {
                        // Process voxels in steps
                        for (int number = 0; number < 10; ++number)
                        {
                            // Retrieve material for current coordinate
                            int iteratorIndex = iterator.Number;
                            Color color;
                            Material material = iterator.GetNextMaterial(out color, out x, out y, out z);

                            // Check for valid voxel
                            if (material != null)
                            {
                                //// Replace material, if texture template is set
                                //if (textureMaterialTemplate != null && voxelTexture != null)
                                //{
                                //    material = textureMaterialTemplate;
                                //    material.SetTexture("_VoxelTex", voxelTexture.target);
                                //}

                                // Check for existing material groups
                                if (groups != null)
                                {
                                    // Check for new group
                                    if (!groups.TryGetValue(material, out subContainer) || (subContainer == null))
                                    {
                                        // Create empty game object
                                        subContainer = new GameObject(material.name == null || material.name.Length == 0 ? (groups.Count + 1).ToString() : material.name);
                                        if (subContainer != null)
                                        {
                                            // Attach it to this main object
                                            subContainer.transform.parent = mainContainer.transform;

                                            // Unset local transformation
                                            subContainer.transform.localPosition = Vector3.zero;
                                            subContainer.transform.localScale = Vector3.one;
                                            subContainer.transform.localRotation = Quaternion.identity;

                                            // Copy static flag
                                            subContainer.isStatic = staticContainers;

                                            // Set layer number by transparency property
                                            if (material.HasProperty("_Color"))
                                            {
                                                if (material.color.a < 1)
                                                {
                                                    subContainer.layer = 1;
                                                }
                                                else
                                                {
                                                    subContainer.layer = 0;
                                                }
                                            }

                                            try
                                            {
                                                // Add group to list
                                                groups.Add(material, subContainer);
                                            }
                                            catch (System.Exception exception)
                                            {
                                                Debug.Log(exception.Message);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Unset container for first material
                                    subContainer = null;
                                }

                                // Calculate current voxel position
                                Vector3 currentPosition = new Vector3((float)x * globalScaling.x + offset.x, (float)y * globalScaling.y + offset.y, (float)z * globalScaling.z + offset.z);

                                // material container as parent for the current voxel
                                GameObject parent = subContainer;

                                // Create empty game object
                                if (containerTemplate != null)
                                {
                                    subContainer = Instantiate(containerTemplate);
                                }
                                else
                                {
                                    subContainer = new GameObject();
                                }
                                if (subContainer != null)
                                {
                                    // Change name of the voxel container to current coordinate
                                    subContainer.name = x.ToString() + ", " + y.ToString() + ", " + z.ToString();

                                    if (parent != null)
                                    {
                                        // Attach it to material container
                                        subContainer.transform.parent = parent.transform;
                                    }
                                    else
                                    {
                                        // Attach it to main object
                                        subContainer.transform.parent = mainContainer.transform;
                                    }

                                    // Set transformation to position and scale current cell
                                    subContainer.transform.localPosition = currentPosition;
                                    subContainer.transform.localScale = scaling * sizeFactor;
                                    subContainer.transform.localRotation = Quaternion.identity;

                                    // Copy static flag
                                    if (containerTemplate == null)
                                    {
                                        subContainer.isStatic = staticContainers;
                                    }

                                    // Set layer number by transparency property
                                    if (material.HasProperty("_Color"))
                                    {
                                        if (material.color.a < 1)
                                        {
                                            subContainer.layer = 1;
                                        }
                                        else
                                        {
                                            subContainer.layer = 0;
                                        }
                                    }

                                    // Check for valid mesh
                                    if (mesh != null)
                                    {
                                        // Add mesh filter
                                        MeshFilter meshFilter = subContainer.AddComponent<MeshFilter>();
                                        if (meshFilter != null)
                                        {
                                            // Apply specified mesh as shared one for the sub container
                                            meshFilter.sharedMesh = mesh;

                                            // Check for vertex color utilization
                                            if (vertexColors)
                                            {
                                                // Create new array, if size does not match
                                                if (colors == null || colors.Length != mesh.vertexCount)
                                                {
                                                    colors = new Color[mesh.vertexCount];
                                                    lastColor = colors[0];
                                                }

                                                // Fill and apply new vertex colors array
                                                if (colors != null)
                                                {
                                                    if (lastColor != color)
                                                    {
                                                        for (int index = 0; index < colors.Length; ++index)
                                                        {
                                                            colors[index] = color;
                                                        }
                                                        lastColor = color;
                                                    }

                                                    meshFilter.mesh.colors = colors;
                                                }
                                            }

                                            // Check for existing voxel map
                                            if (voxelTexture2D != null)
                                            {
                                                // Retrieve texture coordinate for current voxel
                                                Vector2 textureCoordinate = voxelTexture2D.GetTextureCoordinate(voxels, iteratorIndex);
                                                if (!float.IsNaN(textureCoordinate.x))
                                                {
                                                    //// Encode iterator index into texture coordinate
                                                    //textureCoordinate.x += (float)iteratorIndex;

                                                    // Create new array, if size does not match
                                                    if (textureCoordinates == null || textureCoordinates.Length != mesh.vertexCount)
                                                    {
                                                        textureCoordinates = new Vector2[mesh.vertexCount];
                                                    }

                                                    // Fill and apply new UV coordinates array
                                                    if (textureCoordinates != null)
                                                    {
                                                        for (int index = 0; index < textureCoordinates.Length; ++index)
                                                        {
                                                            textureCoordinates[index] = textureCoordinate;
                                                        }

                                                        meshFilter.mesh.uv = textureCoordinates;
                                                    }

                                                    //Debug.Log(textureCoordinate);

                                                    // Apply texture to material
                                                    if (mainTextureTarget)
                                                    {
                                                        material.SetTexture("_MainTex", voxelTexture2D.Texture);
                                                    }
                                                    if (emissiveTextureTarget)
                                                    {
                                                        material.SetTexture("_EmissionMap", voxelTexture2D.Texture);
                                                    }
                                                }
                                            }

                                            //// Check for active volume texture
                                            //if (voxelTexture != null && voxelTexture.target != null)
                                            //{
                                            //    // Instantiate mesh
                                            //    Mesh newMesh = meshFilter.mesh;

                                            //    // Compute texture coordinate center for current voxel
                                            //    Vector3 textureCoordinate = new Vector3((x + 0.5f) / (float)voxelTexture.target.width, (y + 0.5f) / (float)voxelTexture.target.height, (z + 0.5f) / (float)voxelTexture.target.depth);

                                            //    // Fill list of UVs for every vertex
                                            //    List<Vector3> textureCoordinates = new List<Vector3>(newMesh.vertexCount);
                                            //    for (int vertex = 0; vertex < mesh.vertexCount; ++vertex)
                                            //    {
                                            //        textureCoordinates.Add(textureCoordinate);
                                            //    }

                                            //    // Apply them to the mesh
                                            //    newMesh.SetUVs(0, textureCoordinates);
                                            //}

                                            // Add mesh renderer
                                            MeshRenderer meshRenderer = subContainer.AddComponent<MeshRenderer>();
                                            if (meshRenderer != null)
                                            {
                                                // Hide object
                                                meshRenderer.enabled = false;

                                                // Set material to renderer
                                                if (material != null)
                                                {
                                                    meshRenderer.material = material;
                                                }
                                            }

                                            // Check for mesh data and add indices
                                            IMeshData[] meshDataComponents = subContainer.GetComponents<IMeshData>();
                                            if (meshDataComponents != null)
                                            {
                                                foreach (IMeshData meshData in meshDataComponents)
                                                {
                                                    meshData.SetVoxelIndices(new int[1] { iteratorIndex });
                                                    meshDataInterfaces.Add(meshData);
                                                }
                                            }
                                        }
                                    }
                                }

                               
                            }
                            else
                            {
                                iterator = null;
                                break;
                            }
                        }

                        // Return current progress when building has not been finished
                        if (iterator != null)
                        {
                            return ((float)iterator.Number / (float)(voxels.Count + 1) * (mergeMeshes ? 0.5f : 1.0f)) * ((voxelTexture2D != null) ? 0.5f : 1.0f) + ((voxelTexture2D != null) ? 0.5f : 0.0f);
                        }
                    }
                }
            }

            // Check for groups of materials
            if (groups != null && groups.Count > 0)
            {
                GameObject meshContainer;

                // Initialize group enumerator
                if (currentGroup == null)
                {
                    currentGroup = groups.GetEnumerator();
                    if (currentGroup != null)
                    {
                        if (!currentGroup.MoveNext())
                        {
                            currentGroup = null;
                        }
                    }
                }

                // Check for mesh baking
                if (mergeMeshes)
                {
                    int count, index;
                    int vertexCount, indexCount;
                    int iteratorNumber;

                    // Process collected material groups
                    if (currentGroup != null)
                    {
                        // Check if semi-transparent meshes should be merged or if current group is opaque
                        if (!opaqueOnly || (textureMaterialTemplate != null && voxelTexture != null) || (!currentGroup.Current.Key.HasProperty("_Color") || currentGroup.Current.Key.color.a <= 0 || currentGroup.Current.Key.color.a >= 1))
                        {
                            if (materialContainer == null)
                            {
                                // Create empty game object for the material
                                materialContainer = new GameObject(currentGroup.Current.Value.name);
                                if (materialContainer != null)
                                {
                                    // Attach it to this main object
                                    materialContainer.transform.parent = mainContainer.transform;

                                    // Unset relative transformation
                                    materialContainer.transform.localPosition = Vector3.zero;
                                    materialContainer.transform.localScale = Vector3.one;
                                    materialContainer.transform.localRotation = Quaternion.identity;

                                    // Copy static flag
                                    materialContainer.isStatic = staticContainers;

                                    // Get meshes of the current group and create array to store flags of processed meshes 
                                    meshFilters = currentGroup.Current.Value.GetComponentsInChildren<MeshFilter>();
                                    processedMeshes = new bool[meshFilters.Length];

                                    // Initialize processing flags and count meshes to merge
                                    for (meshCount = 0, index = 0; index < meshFilters.Length; ++index)
                                    {
                                        if (meshFilters[index].gameObject != currentGroup.Current.Value)
                                        {
                                            processedMeshes[index] = false;

                                            ++meshCount;
                                        }
                                        else
                                        {
                                            processedMeshes[index] = true;
                                        }
                                    }

                                    if (meshCount == 0)
                                    {
                                        materialContainer = null;
                                    }

                                    currentMesh = 0;
                                }
                            }
                        }
                        else
                        {
                            materialContainer = null;
                        }

                        if (materialContainer != null)
                        {
                            // Count number of meshes and total vertices and indices count for current target
                            for (iteratorNumber = 0, vertexCount = 0, indexCount = 0, count = 0, index = 0; index < meshFilters.Length; ++index)
                            {
                                // Check if mesh has not already been processed
                                if (!processedMeshes[index])
                                {
                                    // Check for vertex, index and voxels count limit
                                    if (vertexCount + meshFilters[index].sharedMesh.vertexCount < 65536 && indexCount + meshFilters[index].sharedMesh.triangles.Length < 65536 && (objectVoxelLimit == 0 || count < objectVoxelLimit))
                                    {
                                        // Increase number of vertices and indices
                                        vertexCount += meshFilters[index].sharedMesh.vertexCount;
                                        indexCount += meshFilters[index].sharedMesh.triangles.Length;

                                        // Increase voxels count
                                        ++count;

                                        // Get mesh data interface
                                        IMeshData meshData = meshFilters[index].gameObject.GetComponent<IMeshData>();
                                        if (meshData != null)
                                        {
                                            // Get indices
                                            int[] indices = meshData.GetVoxelIndices();

                                            // Increase number of voxel indices
                                            if (indices != null)
                                            {
                                                iteratorNumber += indices.Length;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }

                            // Create array to store meshes to merge to
                            CombineInstance[] subMeshes = new CombineInstance[count];

                            // Create array to store voxel indices for current mesh to
                            int[] iteratorIndices = new int[iteratorNumber];

                            // Create empty list to store unused mesh data interface to
                            List<IMeshData> unusedMeshDataInterfaces = new List<IMeshData>();

                            // Process meshes of the current group
                            for (iteratorNumber = 0, vertexCount = 0, indexCount = 0, count = 0, index = 0; index < meshFilters.Length; ++index)
                            {
                                // Check if mesh is not already processed
                                if (!processedMeshes[index])
                                {
                                    // Check for vertex and index limit
                                    if (vertexCount + meshFilters[index].sharedMesh.vertexCount < 65536 && indexCount + meshFilters[index].sharedMesh.triangles.Length < 65536 && (objectVoxelLimit == 0 || count < objectVoxelLimit))
                                    {
                                        // Increase vertices and indices counts for current target mesh
                                        vertexCount += meshFilters[index].sharedMesh.vertexCount;
                                        indexCount += meshFilters[index].sharedMesh.triangles.Length;

                                        // Store mesh instance and calculate transformation relative to parent object
                                        subMeshes[count].mesh = meshFilters[index].sharedMesh;
                                        subMeshes[count].transform = currentGroup.Current.Value.transform.worldToLocalMatrix * meshFilters[index].transform.localToWorldMatrix;

                                        // Set flag to skip mesh at next iteration
                                        processedMeshes[index] = true;

                                        // Get mesh data interface
                                        IMeshData[] meshDataComponents = meshFilters[index].gameObject.GetComponents<IMeshData>();
                                        if (meshDataComponents != null && meshDataComponents.Length >= 1)
                                        {
                                            // Get indices
                                            int[] indices = meshDataComponents[0].GetVoxelIndices();

                                            // Check for valid voxel indices
                                            if (indices != null)
                                            {
                                                // Copy indices
                                                System.Array.Copy(indices, 0, iteratorIndices, iteratorNumber, indices.Length);
                                                iteratorNumber += indices.Length;
                                            }

                                            // Add interfaces for mesh data to list of unused ones
                                            unusedMeshDataInterfaces.AddRange(meshDataComponents);
                                        }

                                        // Increase sub meshes count for current merge target
                                        ++count;
                                    }
                                }
                            }

                            // Create object for current mesh to merge
                            if (containerTemplate != null)
                            {
                                meshContainer = Instantiate(containerTemplate);
                            }
                            else
                            {
                                meshContainer = new GameObject("Part");
                            }
                            if (meshContainer != null)
                            {
                                // Attach it to this material object
                                meshContainer.transform.parent = materialContainer.transform;

                                // Unset relative transformation
                                meshContainer.transform.localPosition = Vector3.zero;
                                meshContainer.transform.localScale = Vector3.one;
                                meshContainer.transform.localRotation = Quaternion.identity;

                                // Copy static flag
                                if (containerTemplate == null)
                                {
                                    meshContainer.isStatic = staticContainers;
                                }

                                // Add mesh filter
                                MeshFilter meshFilter = meshContainer.GetComponent<MeshFilter>();
                                if (meshFilter == null)
                                {
                                    meshFilter = meshContainer.AddComponent<MeshFilter>();
                                }

                                if (meshFilter != null)
                                {
                                    // Create empty mesh object
                                    UnityEngine.Mesh mesh = new UnityEngine.Mesh();
                                    if (mesh != null)
                                    {
                                        // Merge all collected meshes into new one
                                        mesh.CombineMeshes(subMeshes, true, true);

                                        // Set mesh to filter
                                        meshFilter.mesh = mesh;

                                        // Add mesh renderer
                                        MeshRenderer meshRenderer = meshContainer.GetComponent<MeshRenderer>();
                                        if (meshRenderer == null)
                                        {
                                            meshRenderer = meshContainer.AddComponent<MeshRenderer>();
                                        }

                                        // Set material
                                        if (meshRenderer != null)
                                        {
                                            meshRenderer.material = currentGroup.Current.Key;
                                            //meshRenderer.material = groups[group].material;

                                            meshRenderer.enabled = false;
                                        }

                                        // Get texture coordinates to manipulate them
                                        textureCoordinates = mesh.uv2;

                                        // Encode iterator indices into texture coordinates
                                        iteratorNumber = 0;
                                        vertexCount = 0;
                                        foreach (CombineInstance subMesh in subMeshes)
                                        {
                                            for (int vertexNumber = 0; vertexNumber < subMesh.mesh.vertexCount; ++vertexNumber, ++vertexCount)
                                            {
                                                textureCoordinates[vertexCount].x += (float)iteratorNumber;
                                            }

                                            ++iteratorNumber;
                                        }

                                        // Store manipulated UVs
                                        mesh.uv2 = textureCoordinates;

                                        // Remove mesh data interface of the merge meshes
                                        if (unusedMeshDataInterfaces != null)
                                        {
                                            foreach (IMeshData meshData in unusedMeshDataInterfaces)
                                            {
                                                meshDataInterfaces.Remove(meshData);
                                            }
                                        }

                                        // Check for mesh data and add indices
                                        IMeshData[] meshDataComponents = meshContainer.GetComponents<IMeshData>();
                                        if (meshDataComponents != null)
                                        {
                                            foreach (IMeshData meshData in meshDataComponents)
                                            {
                                                meshData.SetVoxelIndices(iteratorIndices);
                                                meshDataInterfaces.Add(meshData);
                                            }
                                        }

                                
                                    }
                                }
                            }

                            // Decrease number of remaining objects
                            currentMesh += count;
                            if (currentMesh >= meshCount)
                            {
                                // Unset objects for current group
                                materialContainer = null;

                                // Remove original game object
                                DestroyImmediate(currentGroup.Current.Value);
                                //DestroyImmediate(groups[group].gameObject);
                            }
                        }
                    }

                    

                    // Increase number of the current group, if it has been finished
                    if (materialContainer == null)
                    {
                        if (currentGroup.MoveNext())
                        {
                            ++groupNumber;
                        }
                        else
                        {
                            currentGroup = null;
                        }
                    }

                    // Return current progress when building has not been finished
                    if (currentGroup != null)
                    {
                        return (((float)groupNumber + ((float)currentMesh / (float)(meshCount + 1))) / (float)groups.Count * 0.5f + 0.5f) * ((voxelTexture2D != null) ? 0.5f : 1.0f) + ((voxelTexture2D != null) ? 0.5f : 0.0f);
                    }
                }

                // Clear groups list
                groups.Clear();
                groups = null;
            }

            // Check for mesh data interfaces
            if (currentMeshDataInterface < meshDataInterfaces.Count)
            {
                // Transfer given voxels using the current interface
                float progress = meshDataInterfaces[currentMeshDataInterface].ProcessVoxels(voxels, bounds);
                if (progress >= 1)
                {
                    ++currentMeshDataInterface;
                    progress = 0;
                }

                if (currentMeshDataInterface < meshDataInterfaces.Count)
                {
                    return ((float)currentMeshDataInterface + progress) / (float)meshDataInterfaces.Count;
                }
            }

            // Reset current processing data
            //currentDepth = 0;
            //currentHeight = 0;
            currentGroup = null;
            groupNumber = 0;
            meshFilters = null;
            processedMeshes = null;
            colors = null;
            voxelTexture2D = null;
            meshDataInterfaces = null;

            if (mainContainer != null)
            {
                // Show new main container and enable its renderers
                mainContainer.hideFlags &= ~HideFlags.HideAndDontSave;
                ShowRenderer(mainContainer);

                
                //Debug.Log("mainContainer = " + mainContainer.gameObject );
                //GameObject prefab = PrefabUtility.CreatePrefab("Assets/Prefabs/test.prefab", mainContainer.gameObject, ReplacePrefabOptions.ReplaceNameBased);

                //StaticBatchingUtility.Combine(mainContainer);

#if UNITY_EDITOR

                // Add object creation undo operation
                if (!Application.isPlaying)
                {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(mainContainer, "\"" + targetName + "\" Creation");
                }
                

#endif

                // Execute informer callback
                if (informer != null)
                {
                    informer(new UnityEngine.Object[] { mainContainer }, parameter);
                }

                mainContainer = null;
            }

            return 1;
        }

        // Enable renderer component of the given object and its children
        void ShowRenderer(GameObject container, bool enabled = true)
        {
            int child;

            if (container.GetComponent<Renderer>() != null)
            {
                container.GetComponent<Renderer>().enabled = enabled;
            }

            for (child = 0; child < container.transform.childCount; ++child)
            {
                ShowRenderer(container.transform.GetChild(child).gameObject, enabled);
            }
        }

    }

    // Interface to store voxel data, which can be used for meshes
    public interface IMeshData
    {
        // Process voxel data
        float ProcessVoxels(Storage voxels, Bounds bounds);

        // Store given array of voxel indices
        void SetVoxelIndices(int[] indices);

        // Return array of voxel indices
        int[] GetVoxelIndices();
    }


}