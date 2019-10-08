using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace unitycoder_MobilePaint
{
	
	public class ToggleCustomPatternModeUI : MonoBehaviour 
	{
		MobilePaint mobilePaint;
		public GameObject customPanel;

		void Start () 
		{
			mobilePaint = PaintManager.mobilePaint;

			if (mobilePaint==null) Debug.LogError("No MobilePaint assigned at "+transform.name, gameObject);
			if (customPanel==null) Debug.LogError("No customPanel assigned at "+transform.name, gameObject);


			var toggle = GetComponent<Toggle>();
			if (toggle==null) Debug.LogError("No toggle component founded at "+transform.name, gameObject);

			if (mobilePaint.useCustomPatterns) {
				toggle.onValueChanged.AddListener(delegate {this.SetMode();});
			}else{
				Debug.LogWarning("No useCustomPatterns enabled, disabling custom brush button", gameObject);
				toggle.interactable=false;
			}
		}


		public void SetMode()
		{
			if (GetComponent<Toggle>().isOn)
			{
				customPanel.SetActive(true);
				mobilePaint.SetDrawModePattern();
			}else{
				customPanel.SetActive(false);
			}

		}

	}
}