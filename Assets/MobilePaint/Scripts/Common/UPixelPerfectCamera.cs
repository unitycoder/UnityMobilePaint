using UnityEngine;

// script from Unity videos: 2D issues and how to solve them : https://www.youtube.com/watch?v=rMCLWt1DuqI

//[ExecuteInEditMode]
public class UPixelPerfectCamera : MonoBehaviour {

	public float pixelsToUnits = 100;

	void Awake () {

		GetComponent<Camera>().orthographicSize = Screen.height / pixelsToUnits / 2;
	}
}
