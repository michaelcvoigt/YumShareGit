// This complete script can be attached to a camera to make it
// continuously point at another object.

// The target variable shows up as a property in the inspector.
// Drag another object onto it to make the camera look at it.
using UnityEngine;
using System.Collections;

public class AlwaysFaceCamera : MonoBehaviour
{
    //public Transform target;

    void Update()
    {
        // Rotate the camera every frame so it keeps looking at the target
        Transform target = Camera.main.transform;

        Vector3 targetPostition = new Vector3(target.position.x,
                                        this.transform.position.y,
                                        target.position.z);

        this.transform.LookAt(targetPostition);
    }
}