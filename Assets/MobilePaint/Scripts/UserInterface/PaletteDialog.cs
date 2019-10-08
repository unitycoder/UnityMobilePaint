// simple script for displaying "modal" palette dialog

using UnityEngine;
using System.Collections;

public class PaletteDialog : MonoBehaviour {

	// fullscreen modal background
	public GameObject modalBackground;
	public GameObject canvas;

	void OnMouseDown()
	{
		OpenPalette();
	}


	void OpenPalette()
	{
		// TODO: if paintcolor was changed from some other script, would have to update the "gui_button_ColorPreview" color to that here, also alpha slider value would need to be updated

		ToggleModalBackground();
	}


	void ClosePalette()
	{
		ToggleModalBackground();
	}


	public void ToggleModalBackground()
	{
		canvas.layer = canvas.layer==0?2:0; // swap between default & ignore raycast
		modalBackground.SetActive(!modalBackground.activeSelf);
	}


}
