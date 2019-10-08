//  hides color dialog, with small delay to avoid accidental "click through"

using UnityEngine;
using System.Collections;

namespace unitycoder_MobilePaint
{
	public class CloseCustomBrushDialog : MonoBehaviour {

		public GameObject functionButton;

		void OnMouseDown()
		{
			Invoke("DelayedToggle",0.4f);
			GetComponent<GUITexture>().enabled = false;
		}

		void DelayedToggle()
		{
			functionButton.GetComponent<CustomBrushDialog>().ToggleModalBackground();
			GetComponent<GUITexture>().enabled = true;
		}


	}
}