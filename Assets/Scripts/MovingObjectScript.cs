using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingObjectScript : MonoBehaviour {

	// Use this for initialization
	void OnBecameInvisible(){

		Debug.Log("Became Invisible");
		if(AREngine.instance){
			AREngine.instance.FinishedEditing();
		}
    }

}
