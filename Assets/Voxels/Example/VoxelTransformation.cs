using UnityEngine;
using System.Collections;


public class VoxelTransformation : MonoBehaviour, Voxels.IMeshData
{
    struct Voxel
    {
        public Vector3 origin;
        public int shaderPropertyID;
    }

    int[] voxelIndices = null;

    Voxel[] voxels = null;

    float currentRotationX = 0;
    float blendFactor = -1;

    public Transition transition = null;



    void Start()
    {
        if (transition == null)
        {
            transition = GetComponentInParent<Transition>();
        }
    }

    void Update()
    {
        if (transition != null)
        {
            Renderer renderer = GetComponent<Renderer>();

            if (renderer != null)
            {
                Material[] materials = renderer.materials;
                Matrix4x4 matrix;
                Quaternion rotation;
                float blendFactor;

                if (materials != null && voxels != null)
                {
                    blendFactor = (Mathf.Max(Mathf.Min(transition.GetBlendFactor(null), 1.0f), 0.5f) - 0.5f) * 2;

                    if (transform.parent != null && transform.parent.parent != null)
                    {
                        blendFactor = transition.GetModifiedBlendFactor(transform.parent.parent.gameObject, blendFactor);
                        rotation = Quaternion.Inverse(transition.GetRotation(transform.parent.parent.gameObject, blendFactor));
                    }
                    else
                    {
                        rotation = Quaternion.identity;
                    }

                    if (blendFactor != this.blendFactor)
                    {
                        foreach (Material material in materials)
                        {
                            for (int index = 0; index < voxels.Length; ++index)
                            {
                                matrix = Matrix4x4.identity;
                                //matrix *= Matrix4x4.TRS(voxels[index].origin * (Mathf.Sin(currentRotationX * 0.01f) - 1.0f), Quaternion.identity, Vector3.one);
                                //matrix *= Matrix4x4.TRS(voxels[index].origin, Quaternion.Euler(blendFactor * 90, 0, 0), Vector3.one);
                                matrix *= Matrix4x4.TRS(voxels[index].origin, rotation, new Vector3(blendFactor, blendFactor, blendFactor));
                                matrix *= Matrix4x4.TRS(-voxels[index].origin, Quaternion.identity, Vector3.one);

                                if (voxels[index].shaderPropertyID < 0)
                                {
                                    voxels[index].shaderPropertyID = Shader.PropertyToID("matrices" + index.ToString());
                                }

                                material.SetMatrix(voxels[index].shaderPropertyID, matrix);
                            }
                        }

                        this.blendFactor = blendFactor;
                    }
                }

                currentRotationX += Time.deltaTime * 15.0f;
            }
        }
    }


    // Store given array of voxel indices
    public void SetVoxelIndices(int[] indices)
    {
        voxelIndices = indices;
    }

    // Return array of voxel indices
    public int[] GetVoxelIndices()
    {
        return voxelIndices;
    }

    // Process voxel data
    public float ProcessVoxels(Voxels.Storage storage, Bounds bounds)
    {
        Material material;
        int x, y, z;
        float widthFactor = bounds.size.x / (float)storage.Width;
        float heightFactor = bounds.size.y / (float)storage.Height;
        float depthFactor = bounds.size.z / (float)storage.Depth;
        int number = 0;

        if (voxels == null || voxels.Length != voxelIndices.Length)
        {
            voxels = new Voxel[voxelIndices.Length];
        }

        for (number = 0; number < voxelIndices.Length; ++number)
        {
            material = storage.GetMaterial(out x, out y, out z, voxelIndices[number]);
            if (material != null)
            {
                voxels[number].origin = new Vector3(
                    (float)x * widthFactor + bounds.min.x,
                    (float)y * heightFactor + bounds.min.y,
                    (float)z * depthFactor + bounds.min.z
                    );
            }
            else
            {
                voxels[number].origin = new Vector3(float.NegativeInfinity, 0, 0);
            }

            voxels[number].shaderPropertyID = -1;
        }

        return 1;
    }

}
