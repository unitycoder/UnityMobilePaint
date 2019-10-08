// simple script for displaying "modal" palette dialog

using UnityEngine;
using System.Collections;

namespace unitycoder_MobilePaint
{
	public class CustomBrushDialog : MonoBehaviour {

		// fullscreen modal background
		public GameObject modalBackground;
		public GameObject canvas;

		void Awake()
		{
			// if custom brushes are not enabled, this button can be destroyed..
			if (canvas.GetComponent<MobilePaint>().useCustomBrushes == false)
			{
				Destroy(gameObject);
			}
		}


		void OnMouseDown()
		{
			OpenBrushDialog();
		}


		void OpenBrushDialog()
		{
			ToggleModalBackground();
		}


		void CloseBrushDialog()
		{
			ToggleModalBackground();
		}


		public void ToggleModalBackground()
		{
			canvas.layer = canvas.layer==0?2:0; // swap between default & ignore raycast
			modalBackground.SetActive(!modalBackground.activeSelf);
		}
	


	}
}