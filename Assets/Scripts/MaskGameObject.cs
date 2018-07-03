using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaskGameObject : MonoBehaviour
{

    void Start()
    {
        // get all renderers in this object and its children:
        Renderer[] renders = GetComponents<Renderer>();

        foreach (Renderer rendr in renders)
        {
            rendr.material.renderQueue = 3002; // set their renderQueue
        }
    }



    // Update is called once per frame
    void Update()
    {

    }
}
