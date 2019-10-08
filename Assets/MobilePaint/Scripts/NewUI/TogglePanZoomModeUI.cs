using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace unitycoder_MobilePaint
{
	
	public class TogglePanZoomModeUI : MonoBehaviour 
	{
		MobilePaint mobilePaint;

		void Start () 
		{
			mobilePaint = PaintManager.mobilePaint;

			if (mobilePaint==null) Debug.LogError("No MobilePaint assigned at "+transform.name,gameObject);

			GetComponent<Toggle>().onValueChanged.AddListener(delegate {this.ToggleZoomPan();});
		}


		public void ToggleZoomPan()
		{
			// toggle zoompanmode based on toggle
			mobilePaint.SetPanZoomMode(GetComponent<Toggle>().isOn);
		}

	}
}