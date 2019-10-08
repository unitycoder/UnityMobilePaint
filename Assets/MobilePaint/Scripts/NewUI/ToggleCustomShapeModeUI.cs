using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace unitycoder_MobilePaint
{
	
	public class ToggleCustomShapeModeUI : MonoBehaviour 
	{
		MobilePaint mobilePaint;
		public GameObject customBrushPanel;

		void Start () 
		{
			mobilePaint = PaintManager.mobilePaint;

			if (mobilePaint==null) Debug.LogError("No MobilePaint assigned",gameObject);
			if (customBrushPanel==null) Debug.LogError("No customBrushPanel assigned",gameObject);

			var toggle=GetComponent<Toggle>();
			if (toggle==null) Debug.LogError("No Toggle component founded",gameObject);

			// disable button if not using custom brushes
			toggle.interactable = mobilePaint.useCustomBrushes;

			if (toggle.IsInteractable()) toggle.onValueChanged.AddListener(delegate {this.SetMode();});
		}


		public void SetMode()
		{
			if (GetComponent<Toggle>().isOn)
			{
				customBrushPanel.SetActive(true);
				mobilePaint.SetDrawModeShapes();
			}else{
				customBrushPanel.SetActive(false);
			}

		}

	}
}