using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace unitycoder_MobilePaint
{

	public class BrushSizeUI : MonoBehaviour {

		MobilePaint mobilePaint;
		private Slider slider;

		void Start () 
		{
			mobilePaint = PaintManager.mobilePaint; // gets reference to mobilepaint through PaintManager

			if (mobilePaint==null) Debug.LogError("No MobilePaint assigned at "+transform.name, gameObject);

			slider = GetComponent<Slider>();
			if (slider==null) 
			{
				Debug.LogError("No Slider component founded",gameObject);
			}else{
				slider.value = mobilePaint.brushSize;
				slider.onValueChanged.AddListener((value) => { mobilePaint.SetBrushSize((int)value); });
			}
		}
	}
}