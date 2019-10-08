using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace unitycoder_MobilePaint
{
	
	public class ToggleEraserModeUI : MonoBehaviour 
	{
		MobilePaint mobilePaint;

		void Start () 
		{
			mobilePaint = PaintManager.mobilePaint;

			if (mobilePaint==null) Debug.LogError("No MobilePaint assigned",gameObject);

			var toggle=GetComponent<Toggle>();
			if (toggle==null) Debug.LogError("No Toggle component founded",gameObject);
			if (toggle.IsInteractable()) toggle.onValueChanged.AddListener(delegate {this.SetMode();});
		}


		public void SetMode()
		{
			if (GetComponent<Toggle>().isOn)
			{
				mobilePaint.SetDrawModeEraser();
			}

		}

	}
}