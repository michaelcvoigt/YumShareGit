using UnityEngine;
using System.Collections;

public class Spin : MonoBehaviour {

	public Vector3 SpinRate = new Vector3( 0.0f, 0.0f, 0.0f);
	
	// Update is called once per frame
	void Update () {
		
			transform.Rotate( SpinRate);
	}
}
