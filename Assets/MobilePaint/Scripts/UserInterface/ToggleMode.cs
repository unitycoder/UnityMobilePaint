// simple script for swapping between drawing modes

using UnityEngine;
using System.Collections;

namespace unitycoder_MobilePaint
{
	public class ToggleMode : MonoBehaviour {

		public GameObject canvas;
		public GameObject otherButton1;
		public GameObject otherButton2;

		public GameObject sizeGUI1;

		public DrawMode mode=DrawMode.Default;

		void OnMouseDown()
		{

			canvas.GetComponent<MobilePaint>().drawMode = mode;

			GetComponent<GUITexture>().color = new Color(0.5f,0.5f,0.5f,0.5f);
			otherButton1.GetComponent<GUITexture>().color = new Color(0.25f,0.25f,0.25f,0.5f);
			otherButton2.GetComponent<GUITexture>().color = new Color(0.25f,0.25f,0.25f,0.5f);

			// only pencil mode can have resize tool
			if (mode!=DrawMode.Default)
			{
				sizeGUI1.GetComponent<GUITexture>().color = new Color(0.25f,0.25f,0.25f,0.5f);
			}else{
				sizeGUI1.GetComponent<GUITexture>().color = new Color(0.5f,0.5f,0.5f,0.5f);
			}
			// toggle between default & no raycast
			sizeGUI1.layer = 2-sizeGUI1.layer;


		} // mousedown

	} // class
} // namespace