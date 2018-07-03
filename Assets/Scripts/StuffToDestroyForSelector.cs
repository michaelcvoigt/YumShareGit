using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StuffToDestroyForSelector : MonoBehaviour
{

    public GameObject[] StuffToDestroy;

    public void DestroyStuff()
    {

        foreach (GameObject destroyMe in StuffToDestroy)
        {

            Destroy(destroyMe);
        }
    }


}
