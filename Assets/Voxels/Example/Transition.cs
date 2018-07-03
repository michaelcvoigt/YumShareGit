using UnityEngine;
using System.Collections;

public class Transition : MonoBehaviour
{
    protected struct Transformation
    {
        public Quaternion sourceRotation;
        public Vector3 sourcePosition;
        public Vector3 sourceScaling;
        public Quaternion targetRotation;
        public Vector3 targetPosition;
        public Vector3 targetScaling;
    }


    public Camera targetCamera;

    public GameObject[] sources;
    protected GameObject[] targets;
    protected Transformation[] transformations;

    public int selectedTarget = 0;
    protected int currentTarget = 0;
    protected float targetFactor = 0;
    public float blendTime = 1;


    // Initialization
    void Start()
    {
        // Create empty array to store target objects, which will have been built from source ones
        targets = new GameObject[sources.Length];

        transformations = new Transformation[sources.Length];

        // Process given source objects
        for (int index = 0; index < sources.Length; ++index)
        {
            // Store rotation, position and scaling of the source
            transformations[index].sourceRotation = sources[index].transform.localRotation;
            transformations[index].sourcePosition = sources[index].transform.localPosition;
            transformations[index].sourceScaling = sources[index].transform.localScale;

            // Try to get voxel rasterizer component for current source object
            Voxels.Rasterizer rasterizer = sources[index].GetComponent<Voxels.Rasterizer>();
            if (rasterizer != null)
            {
                // Start processing
                rasterizer.Process(ActOnCreation, sources[index]);
            }
            else
            {
                // Use source as target object
                targets[index] = sources[index];

                transformations[index].targetRotation = sources[index].transform.localRotation;
                transformations[index].targetPosition = sources[index].transform.localPosition;
                transformations[index].targetScaling = sources[index].transform.localScale;
            }

            // Deactive unselected source object
            if (selectedTarget != index)
            {
                sources[index].SetActive(false);
            }
        }

        currentTarget = selectedTarget;
    }

    // Update is called once per frame
    void Update()
    {
        // Limit selected target to array range
        if (selectedTarget < 0)
        {
            selectedTarget = 0;
        }
        else if (selectedTarget >= targets.Length)
        {
            selectedTarget = targets.Length - 1;
        }

        // Check for selection change
        if (currentTarget != selectedTarget)
        {
            // Increase interpolation factor by elapsed time
            if (blendTime > 0)
            {
                targetFactor += Time.deltaTime / blendTime;
            }
            else
            {
                targetFactor = 1;
            }

            // Check if target has reached
            if (targetFactor >= 1)
            {
                // Deactive former object and activate selected object
                if (targets[currentTarget] != null)
                {
                    targets[currentTarget].SetActive(false);
                }
                if (targets[selectedTarget] != null)
                {
                    targets[selectedTarget].transform.localRotation = transformations[selectedTarget].targetRotation;
                    targets[selectedTarget].transform.localPosition = transformations[selectedTarget].targetPosition;
                    targets[selectedTarget].transform.localScale = transformations[selectedTarget].targetScaling;

                    targets[selectedTarget].SetActive(true);
                }

                // Store index of the new target
                currentTarget = selectedTarget;

                // Reset interpolation factor
                targetFactor = 0;
            }
            else
            {
                // Calculate soft interpolation factor
                float transformationFactor = Mathf.Sin((targetFactor - 0.5f) * Mathf.PI) * 0.5f + 0.5f;

                if (targets[currentTarget] != null)
                {
                    // Interpolate transformation between previously and newly selected target
                    targets[currentTarget].transform.localRotation = Quaternion.Slerp(Quaternion.identity, transformations[selectedTarget].sourceRotation * Quaternion.Inverse(transformations[currentTarget].sourceRotation), transformationFactor) * transformations[currentTarget].targetRotation;

                    targets[currentTarget].transform.localPosition = Vector3.Slerp(Vector3.zero, transformations[selectedTarget].sourcePosition - transformations[currentTarget].sourcePosition, transformationFactor) + transformations[currentTarget].targetPosition;

                    targets[currentTarget].transform.localScale = Vector3.Slerp(transformations[currentTarget].targetScaling, new Vector3(
                        transformations[selectedTarget].sourceScaling.x / transformations[currentTarget].sourceScaling.x * transformations[currentTarget].targetScaling.x,
                        transformations[selectedTarget].sourceScaling.y / transformations[currentTarget].sourceScaling.y * transformations[currentTarget].targetScaling.y,
                        transformations[selectedTarget].sourceScaling.z / transformations[currentTarget].sourceScaling.z * transformations[currentTarget].targetScaling.z),
                        transformationFactor);
                }

                if (targets[selectedTarget] != null)
                {
                    targets[selectedTarget].SetActive(true);

                    // Interpolate transformation between previously and newly selected target
                    targets[selectedTarget].transform.localRotation = Quaternion.Slerp(Quaternion.identity, transformations[currentTarget].sourceRotation * Quaternion.Inverse(transformations[selectedTarget].sourceRotation), 1 - transformationFactor) * transformations[selectedTarget].targetRotation;

                    targets[selectedTarget].transform.localPosition = Vector3.Slerp(Vector3.zero, transformations[currentTarget].sourcePosition - transformations[selectedTarget].sourcePosition, 1 - transformationFactor) + transformations[selectedTarget].targetPosition;

                    targets[selectedTarget].transform.localScale = Vector3.Slerp(transformations[selectedTarget].targetScaling, new Vector3(
                        transformations[currentTarget].sourceScaling.x / transformations[selectedTarget].sourceScaling.x * transformations[selectedTarget].targetScaling.x,
                        transformations[currentTarget].sourceScaling.y / transformations[selectedTarget].sourceScaling.y * transformations[selectedTarget].targetScaling.y,
                        transformations[currentTarget].sourceScaling.z / transformations[selectedTarget].sourceScaling.z * transformations[selectedTarget].targetScaling.z),
                        1 - transformationFactor);
                }
            }
        }
    }

    // Process the construction of the target objects
    void ActOnCreation(Object[] objects, object parameter)
    {
        // Process all constructed objects
        foreach (Object current in objects)
        {
            int target = -1;

            // Check for game object
            if (current.GetType() == typeof(GameObject))
            {
                // Check if application-defined parameter is a game object, too
                if (parameter.GetType() == typeof(GameObject))
                {
                    // Find given in source objects
                    for (int index = 0; index < sources.Length; ++index)
                    {
                        if ((object)sources[index] == parameter)
                        {
                            target = index;
                        }
                    }

                    // Deactivate source object
                    ((GameObject)parameter).SetActive(false);
                }
                // Check if index is given directly
                else if (parameter.GetType() == typeof(int))
                {
                    // Check for valid index
                    if ((int)parameter >= 0 && (int)parameter < targets.Length)
                    {
                        target = (int)parameter;
                    }

                    // Deactivate source object
                    if ((int)parameter >= 0 && (int)parameter < sources.Length)
                    {
                        sources[(int)parameter].SetActive(false);
                    }
                }

                // Attach new object to this one
                ((GameObject)current).transform.parent = transform;

                // Store new obiect and its initial transformation as target
                if (target != -1)
                {
                    targets[target] = (GameObject)current;

                    transformations[target].targetRotation = targets[target].transform.localRotation;
                    transformations[target].targetPosition = targets[target].transform.localPosition;
                    transformations[target].targetScaling = targets[target].transform.localScale;

                    // Deactive unselected object
                    if (selectedTarget != target)
                    {
                        targets[target].SetActive(false);
                    }
                }
            }
        }
    }

    // Return blend factor for given object
    public float GetBlendFactor(GameObject target)
    {
        if (target == null)
        {
            return targetFactor;
        }
        else if (targets[currentTarget] == target)
        {
            return 1 - targetFactor;
        }
        else if (targets[selectedTarget] == target)
        {
            return targetFactor;
        }

        return 0;
    }

    // Return blend factor for given object
    public float GetModifiedBlendFactor(GameObject target, float blendFactor)
    {
        if (targets[currentTarget] == target)
        {
            return 1 - blendFactor;
        }

        return blendFactor;
    }

    public Quaternion GetRotation(GameObject target, float blendFactor)
    {
        if (targets[currentTarget] == target)
        {
            return Quaternion.Slerp(Quaternion.identity, transformations[selectedTarget].sourceRotation * Quaternion.Inverse(transformations[currentTarget].sourceRotation), 1 - blendFactor);
        }
        else if (targets[selectedTarget] == target)
        {
            return Quaternion.Slerp(Quaternion.identity, transformations[currentTarget].sourceRotation * Quaternion.Inverse(transformations[selectedTarget].sourceRotation), 1 - blendFactor);
        }

        return Quaternion.identity;
    }

}
