using UnityEngine;
using System.Collections;

public class AppController : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		// quit
		if (Input.GetKey(KeyCode.Escape))
	    {
	       Application.Quit();
	    }
	}
}
