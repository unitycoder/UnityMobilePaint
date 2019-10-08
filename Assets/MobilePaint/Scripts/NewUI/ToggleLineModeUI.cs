using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace unitycoder_MobilePaint
{
	
	public class ToggleLineModeUI : MonoBehaviour 
	{
		MobilePaint mobilePaint;

		void Start () 
		{
			mobilePaint = PaintManager.mobilePaint;

			if (mobilePaint==null) Debug.LogError("No MobilePaint assigned at "+transform.name,gameObject);

			var toggle = GetComponent<Toggle>();
			if (toggle==null) Debug.LogError("No toggle component founded at "+transform.name, gameObject);

			toggle.onValueChanged.AddListener(delegate {this.SetMode();});
		}


		public void SetMode()
		{
			if (GetComponent<Toggle>().isOn)
			{
				mobilePaint.SetDrawModeLine();
			}

		}

	}
}