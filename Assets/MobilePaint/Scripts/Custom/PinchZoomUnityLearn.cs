using UnityEngine;

// Modified from : https://unity3d.com/learn/tutorials/modules/beginner/platform-specific/pinch-zoom

// NOTE: there is no mouse support for pinch & pan

public class PinchZoomUnityLearn : MonoBehaviour
{
	public float perspectiveZoomSpeed = 0.5f;        // The rate of change of the field of view in perspective mode.
	public float orthoZoomSpeed = 0.5f;        // The rate of change of the orthographic size in orthographic mode.

	public float minOrthoZoom = 10; 
	public float maxOrthoZoom = 100;  // usually should be the defauly full screen = 100 (unless want to zoom out of the image)

	public float minPerpectiveZoom = 10; 
	public float maxPerpectiveZoom = 100;  // usually should be the defauly full screen = 100 (unless want to zoom out of the image)

	public bool panEnabled = true;
	public bool zoomEnabled = true;

	public Vector2 panLimitMin;
	public Vector2 panLimitMax;

	Camera cam;

	void Awake()
	{
		cam = Camera.main;
	}

	// Only enabled if Zoom is on
	void Update()
	{
		// panning, 1 touch
		if (panEnabled && Input.touchCount == 1)
		{
			Touch touchZero = Input.GetTouch(0);

			// just move the camera
			Vector3 newPos = cam.transform.position-(Vector3)touchZero.deltaPosition*Time.deltaTime;

			// clamp pos, NOTE: doesnt take account of zooming yet
			newPos.x = Mathf.Clamp(newPos.x, panLimitMin.x, panLimitMax.x); 
			newPos.y = Mathf.Clamp(newPos.y, panLimitMin.y, panLimitMax.y); 

			cam.transform.position = newPos;

		}
		else if (zoomEnabled && Input.touchCount == 2) // zooming, 2 touches
		{
			// Store both touches.
			Touch touchZero = Input.GetTouch(0);
			Touch touchOne = Input.GetTouch(1);
			
			// Find the position in the previous frame of each touch.
			Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
			Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
			
			// Find the magnitude of the vector (the distance) between the touches in each frame.
			float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
			float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
			
			// Find the difference in the distances between each frame.
			float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
			
			// If the camera is orthographic...
			if (cam.orthographic)
			{
				// ... change the orthographic size based on the change in distance between the touches.
				cam.orthographicSize += deltaMagnitudeDiff * orthoZoomSpeed;
				
				// Make sure the orthographic size never drops below zero.
				cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minOrthoZoom, maxOrthoZoom); //Mathf.Max(cam.orthographicSize, 0.1f);
			}
			else
			{
				// Otherwise change the field of view based on the change in distance between the touches.
				cam.fieldOfView += deltaMagnitudeDiff * perspectiveZoomSpeed;
				
				// Clamp the field of view to make sure it's between 0 and 180.
				cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minPerpectiveZoom, maxPerpectiveZoom);
			}
		}
	} // Update()
}